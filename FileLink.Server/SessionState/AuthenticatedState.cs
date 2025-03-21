using System.Text.Json;
using FileLink.Server.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.SessionState
{
    // Represents the authenticated state in the session state machine
    // In this state the client can perform general file operations
    public class AuthenticatedState : ISessionState
    {
        private readonly FileService _fileService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        private DateTime _lastActivityTime;

        // Gets the client session this state is associated with
        public ClientSession ClientSession { get; }

        // Initializes a new instance of the AuthenticatedState class
        public AuthenticatedState(ClientSession clientSession, FileService fileService, LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _lastActivityTime = DateTime.Now;
        }

        // Handles all packets while in authenticated state
        public async Task<Packet> HandlePacket(Packet packet)
        {
            // Update last activity time to prevent session timeout
            _lastActivityTime = DateTime.Now;
            
            // Verify the user ID in the packet matches the session's user ID
            if (!string.IsNullOrEmpty(ClientSession.UserId) && !string.IsNullOrEmpty(packet.UserId) && packet.UserId != ClientSession.UserId)
            {
                _logService.Warning($"User ID mismatch in packet: {packet.UserId} vs session: {ClientSession.UserId}");
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", ClientSession.UserId);
            }

            try
            {
                // All commands available while in authenticated state
                switch (packet.CommandCode)
                {
                    case FileLink.Server.Protocol.Commands.CommandCode.LOGOUT_REQUEST:
                        return await HandleLogoutRequest(packet);

                    case FileLink.Server.Protocol.Commands.CommandCode.FILE_LIST_REQUEST:
                        return await HandleFileListRequest(packet);

                    case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST:
                        return await HandleFileUploadInitRequest(packet);

                    case FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST:
                        return await HandleFileDownloadInitRequest(packet);

                    case FileLink.Server.Protocol.Commands.CommandCode.FILE_DELETE_REQUEST:
                        return await HandleFileDeleteRequest(packet);

                    default:
                        _logService.Warning($"Received unexpected command in AuthenticatedState: {Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)}");
                        return _packetFactory.CreateErrorResponse(packet.CommandCode, $"Command {Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)} not supported in authenticated state.", ClientSession.UserId);
                }
            }
            catch (Exception ex)
            {
                // Log error and send response to the request
                _logService.Error($"Error handling packet in AuthenticatedState: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred while processing your request.", ClientSession.UserId);
            }
        }

        // Handles a logout request
        public async Task<Packet> HandleLogoutRequest(Packet packet)
        {
            _logService.Info($"User {ClientSession.UserId} is logging out");
            
            // Create response before transitioning states
            var response = _packetFactory.CreateLogoutResponse(true, "Logout successful");
            
            // Transition to disconnecting state
            ClientSession.TransitionToState(ClientSession.StateFactory.CreateDisconnectingState(ClientSession));
            
            // Schedule disconnect after sending response
            // Intentionally fire and forget this task using discard '_' 
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // Give time for the response to be sent
                await ClientSession.Disconnect("User logged out");
            });
            return response;
        }

        // Handles a file list request
        public async Task<Packet> HandleFileListRequest(Packet packet)
        {
            _logService.Debug($"Fetching list of files from {ClientSession.UserId}");

            try
            {
                // Get the list of files for the user
                var files = await _fileService.GetUserFiles(ClientSession.UserId);

                // Create and return the response
                return _packetFactory.CreateFileListResponse(files, ClientSession.UserId);
            }
            catch  (Exception ex)
            {
                _logService.Error($"Error handling file list request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "Error retrieving file list: " + ex.Message, ClientSession.UserId);
            }
        }

        // Handles a file upload initialization request
        public async Task<Packet> HandleFileUploadInitRequest(Packet packet)
        {
            try
            {
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning($"Received file upload init request with no payload from user {ClientSession.UserId}");
                    return _packetFactory.CreateFileUploadInitResponse(false, "", "No file information provided.", ClientSession.UserId);
                }
                
                // Extract file information from payload
                var fileInfo = JsonSerializer.Deserialize<FileUploadInitInfo>(packet.Payload);
                
                // Validate file name
                if (string.IsNullOrEmpty(fileInfo.FileName))
                {
                    return _packetFactory.CreateFileUploadInitResponse(false, "",  "No file information provided.", ClientSession.UserId);
                }
                
                // Initialize file upload
                var fileMetadata = await _fileService.InitializeFileUpload(
                    ClientSession.UserId,
                    fileInfo.FileName,
                    fileInfo.FileSize,
                    fileInfo.ContentType);

                // Transition to transfer state for upload
                ClientSession.TransitionToState(ClientSession.StateFactory.CreateTransferState(ClientSession, fileMetadata, true));
                
                _logService.Info($"File upload initiated: {fileInfo.FileName} (ID: {fileMetadata.Id}) for user {ClientSession.UserId}");
                
                // Create and return the response
                return _packetFactory.CreateFileUploadInitResponse(true, fileMetadata.Id, "File upload initialized successfully.", ClientSession.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error initializing file upload: {ex.Message}", ex);
                return _packetFactory.CreateFileUploadInitResponse(false, "", "Error initializing file upload: " + ex.Message, ClientSession.UserId);
            }
        }

        // Handles a file download initialization request   
        public async Task<Packet> HandleFileDownloadInitRequest(Packet packet)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning(
                        $"Received file download init request with no file ID from user {ClientSession.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "File ID is required.",
                        ClientSession.UserId);
                }

                // Initialize the file download
                var fileMetadata = await _fileService.InitializeFileDownload(fileId, ClientSession.UserId);

                if (fileMetadata == null)
                {
                    return _packetFactory.CreateErrorResponse(packet.CommandCode,
                        "File not found or you do not have permission to download it.", ClientSession.UserId);
                }

                // Calculate total chunks 
                int totalChunks = (int)Math.Ceiling((double)fileMetadata.FileSize / _fileService.ChunkSize);

                // Transition to transfer state for download
                ClientSession.TransitionToState(
                    ClientSession.StateFactory.CreateTransferState(ClientSession, fileMetadata, false));

                _logService.Info(
                    $"File download initialized: {fileMetadata.FileName} (ID: {fileMetadata.Id}) for user {ClientSession.UserId}");

                // Create and return the response 
                return _packetFactory.CreateFileDownloadInitResponse(
                    true,
                    fileMetadata.Id,
                    fileMetadata.FileName,
                    fileMetadata.FileSize,
                    fileMetadata.ContentType,
                    totalChunks,
                    "File download initialized successfully.",
                    ClientSession.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error initializing file download: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "Error initializing file download: " + ex.Message, ClientSession.UserId);
            }
        }

        // Handles a file delete request
        public async Task<Packet> HandleFileDeleteRequest(Packet packet)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file delete request with no file ID from user {ClientSession.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(false, "", "File ID is required.", ClientSession.UserId);
                }
                
                // Delete the file
                bool success = await _fileService.DeleteFile(fileId, ClientSession.UserId);

                if (success)
                {
                    _logService.Info($"File deleted: {fileId} by user {ClientSession.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(true, fileId, "File deleted successfully.", ClientSession.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to delete file: {fileId} by user {ClientSession.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(false, fileId, "Failed to delete file. File not found or you do not have permission to delete it.", ClientSession.UserId);
                }
            }
            catch(Exception ex)
            {
                _logService.Error($"Error deleting file: {ex.Message}", ex);
                return _packetFactory.CreateFileDeleteResponse(false, "", "Error deleting file: " + ex.Message, ClientSession.UserId);
            }
        }

        // Called when entering the authenticated state
        public Task OnEnter()
        {
            _logService.Debug($"Session {ClientSession.SessionId} entered AuthenticatedState for user {ClientSession.UserId}");
            _lastActivityTime = DateTime.Now;
            return Task.CompletedTask;        
        }

        // Called when exiting the authenticated state
        public Task OnExit()
        {
            _logService.Debug($"Session {ClientSession.SessionId} exited AuthenticatedState for user {ClientSession.UserId}");
            return Task.CompletedTask;        
        }

        // Class for deserializing file upload initialization information
        private class FileUploadInitInfo
        {
            public string FileName { get; set; }
            public long FileSize { get; set; }
            public string ContentType { get; set; }
        }
    }
}