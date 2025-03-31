using System.Text.Json;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Disk.FileManagement;
using FileLink.Server.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands;

// Command handler for file uploads
// Implements the command pattern
public class FileUploadCommandHandler : ICommandHandler
{
    private readonly FileService _fileService;
    private readonly DirectoryService _directoryService;
    private readonly LogService _logService;
    private readonly PacketFactory _packetFactory = new PacketFactory();

    // Constructor
    public FileUploadCommandHandler(FileService fileService, DirectoryService directoryService, LogService logService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }

    // Determines whether this handler can process the specified command code 
    public bool CanHandle(int commandCode)
    {
        return commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST ||
               commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST ||
               commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST;
    }

    // Handle a file upload packet yo
    public async Task<Packet> Handle(Packet packet, ClientSession session)
    {
        try
        {
            // Check if the session is authenticated 
            if (string.IsNullOrEmpty(session.UserId))
            {
                _logService.Warning("Received file upload request from unauthenticated session.");
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to upload files.", "");
            }

            // Check if the user ID in the packet matches the session user ID
            if (!string.IsNullOrEmpty(session.UserId) && packet.UserId != session.UserId)
            {
                _logService.Warning(
                    $"User ID mismatch in file upload request: {packet.UserId} vs session: {session.UserId}");
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
            }

            // Handle the appropriate upload command
            switch (packet.CommandCode)
            {
                case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST:
                    return await HandleFileUploadInitRequest(packet, session);

                case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST:
                    return await HandleFileUploadChunkRequest(packet, session);

                case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST:
                    return await HandleFileUploadCompleteRequest(packet, session);

                default:
                    _logService.Warning($"Unexpected command code in file upload handler: {packet.CommandCode}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "Unexpected command code for file upload operation.", session.UserId);
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error processing file upload request: {ex.Message}", ex);
            return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred during the file upload operation.", session.UserId);
        }
    }

    // Handles a file upload initialization request
    private async Task<Packet> HandleFileUploadInitRequest(Packet packet, ClientSession session)
    {
        try
        {
            if (packet.Payload == null || packet.Payload.Length == 0)
            {
                _logService.Warning($"Received file upload init request with no payload from user {session.UserId}");
                return _packetFactory.CreateFileUploadInitResponse(false, "", "No file information provided.", session.UserId);
            }

            // Deserialize the payload to extract file information
            var fileInfo = JsonSerializer.Deserialize<FileUploadInitInfo>(packet.Payload);

            if (fileInfo == null || string.IsNullOrEmpty(fileInfo.FileName))
            {
                _logService.Warning($"Received file upload init request with invalid file information from user {session.UserId}");
                return _packetFactory.CreateFileUploadInitResponse(false, "", "File name is required.", session.UserId);
            }

            if (fileInfo.FileSize <= 0)
            {
                _logService.Warning($"Received file upload init request with invalid file size: {fileInfo.FileSize} from user {session.UserId}");
                return _packetFactory.CreateFileUploadInitResponse(false, "", "File size must be greater than zero.", session.UserId);
            }
            
            // Check for directory ID in the metadata
            string directoryId = null;
            if (packet.Metadata.TryGetValue("DirectoryId", out string dirId) && dirId != "root")
            {
                directoryId = dirId;
                    
                // Validate that the directory exists and the user has access to it
                var directory = await _directoryService.GetDirectoryById(directoryId, session.UserId);
                if (directory == null)
                {
                    _logService.Warning($"Directory not found or not owned by user: {directoryId}");
                    return _packetFactory.CreateFileUploadInitResponse(
                        false, 
                        "", 
                        "Directory not found or you do not have permission to upload to it.", 
                        session.UserId);
                }
                    
                _logService.Info($"Validated directory for upload: {directory.Name} (ID: {directoryId})");
            }

            // Initialize the file upload
            var fileMetadata = await _fileService.InitializeFileUpload(
                session.UserId,
                fileInfo.FileName,
                fileInfo.FileSize,
                fileInfo.ContentType ?? "application/octet-stream");

            if (fileMetadata == null)
            {
                _logService.Error($"Failed to initialize file upload for user {session.UserId}");
                return _packetFactory.CreateFileUploadInitResponse(false, "", "Failed to initialize file upload.", session.UserId);
            }
            
            // Set the directory ID if specified
            if (!string.IsNullOrEmpty(directoryId))
            {
                _logService.Debug($"Setting directory ID {directoryId} for file {fileMetadata.Id}");
                fileMetadata.DirectoryId = directoryId;
                    
                // Update the file metadata with the directory ID
                bool updateSuccess = await _fileService.UpdateFileMetadata(fileMetadata);
                if (!updateSuccess)
                {
                    _logService.Warning($"Failed to update file metadata with directory ID: {directoryId}");
                    // Continue anyway as this is not a critical error
                }
            }

            _logService.Info($"File upload initialized: {fileInfo.FileName} (ID: {fileMetadata.Id}) for user {session.UserId}");

            // Create and return the response
            return _packetFactory.CreateFileUploadInitResponse(true, fileMetadata.Id, "File upload initialized successfully.", session.UserId);
        }
        catch (Exception ex)
        {
            _logService.Error($"Error handling file upload init request: {ex.Message}", ex);
            return _packetFactory.CreateFileUploadInitResponse(false, "", $"Error initializing file upload: {ex.Message}", session.UserId);
        }
    }

    // Handles a file upload chunk request
    private async Task<Packet> HandleFileUploadChunkRequest(Packet packet, ClientSession session)
    {
        try
        {
            // Get file ID from metadata
            if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
            {
                _logService.Warning($"Received file upload chunk request with no file ID from user {session.UserId}");
                return _packetFactory.CreateFileUploadChunkResponse(
                    false, "", -1, "File ID is required.", session.UserId);
            }

            // Get chunk index from metadata
            if (!packet.Metadata.TryGetValue("ChunkIndex", out string chunkIndexStr) ||
                !int.TryParse(chunkIndexStr, out int chunkIndex))
            {
                _logService.Warning(
                    $"Received file upload chunk request with invalid chunk index: {chunkIndexStr} from user {session.UserId}");
                return _packetFactory.CreateFileUploadChunkResponse(
                    false, fileId, -1, "Valid chunk index is required.", session.UserId);
            }

            // Get isLastChunk flag from metadata
            bool isLastChunk = false;
            if (packet.Metadata.TryGetValue("IsLastChunk", out string isLastChunkStr))
            {
                bool.TryParse(isLastChunkStr, out isLastChunk);
            }

            // Check if payload contains data
            if (packet.Payload == null || packet.Payload.Length == 0)
            {
                _logService.Warning($"Received file upload chunk request with no data from user {session.UserId}");
                return _packetFactory.CreateFileUploadChunkResponse(
                    false, fileId, chunkIndex, "Chunk data is required.", session.UserId);
            }

            // Process the chunk
            bool success = await _fileService.ProcessFileChunk(fileId, chunkIndex, isLastChunk, packet.Payload);

            if (success)
            {
                _logService.Debug($"Processed chunk {chunkIndex} for file {fileId} from user {session.UserId}");
                return _packetFactory.CreateFileUploadChunkResponse(
                    true, fileId, chunkIndex, "Chunk processed successfully.", session.UserId);
            }
            else
            {
                _logService.Warning(
                    $"Failed to process chunk {chunkIndex} for file {fileId} from user {session.UserId}");
                return _packetFactory.CreateFileUploadChunkResponse(
                    false, fileId, chunkIndex, "Failed to process chunk.", session.UserId);
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error handling file upload chunk request: {ex.Message}", ex);
            string fileId = packet.Metadata.TryGetValue("FileId", out string id) ? id : "";
            int chunkIndex = packet.Metadata.TryGetValue("ChunkIndex", out string idx) &&
                             int.TryParse(idx, out int index)
                ? index
                : -1;

            return _packetFactory.CreateFileUploadChunkResponse(
                false, fileId, chunkIndex, $"Error processing chunk: {ex.Message}", session.UserId);
        }
    }

    // Handles a file upload complete request
    private async Task<Packet> HandleFileUploadCompleteRequest(Packet packet, ClientSession session)
    {
        try
        {
            // Get file ID from metadata
            if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
            {
                _logService.Warning($"Received file upload complete request with no file ID from user {session.UserId}");
                return _packetFactory.CreateFileUploadCompleteResponse(
                    false, "", "File ID is required.", session.UserId);
            }

            // Finalize the upload
            bool success = await _fileService.FinalizeFileUpload(fileId);
                
            if (success)
            {
                _logService.Info($"File upload completed for file {fileId} by user {session.UserId}");
                return _packetFactory.CreateFileUploadCompleteResponse(
                    true, fileId, "File upload completed successfully.", session.UserId);
            }
            else
            {
                _logService.Warning($"Failed to complete file upload for file {fileId} by user {session.UserId}");
                return _packetFactory.CreateFileUploadCompleteResponse(
                    false, fileId, "Failed to complete file upload.", session.UserId);
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error handling file upload complete request: {ex.Message}", ex);
            string fileId = packet.Metadata.TryGetValue("FileId", out string id) ? id : "";
                
            return _packetFactory.CreateFileUploadCompleteResponse(
                false, fileId, $"Error completing file upload: {ex.Message}", session.UserId);
        }
    }

    private class FileUploadInitInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
    }
}
