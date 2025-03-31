using FileLink.Server.Disk.FileManagement;
using FileLink.Server.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands
{
    // Command handler for file download operations
    // Implements the Command pattern
    public class FileDownloadCommandHandler : ICommandHandler
    {
        // This class is dependent on FileService, LogService and our PacketFactory
        private readonly FileService _fileService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        
        // Initializes a new instance of the FileDownloadCommandHandler class
        public FileDownloadCommandHandler(FileService fileService, LogService logService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST ||
                   commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST ||
                   commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_REQUEST;
        }
        
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received file download request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to download files.", "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in file download request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
                }

                // Handle the appropriate download command
                switch (packet.CommandCode)
                {
                    case FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST:
                        return await HandleFileDownloadInitRequest(packet, session);
                        
                    case FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST:
                        return await HandleFileDownloadChunkRequest(packet, session);
                        
                    case FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_REQUEST:
                        return await HandleFileDownloadCompleteRequest(packet, session);
                        
                    default:
                        _logService.Warning($"Unexpected command code in file download handler: {packet.CommandCode}");
                        return _packetFactory.CreateErrorResponse(packet.CommandCode, "Unexpected command code for file download operation.", session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file download request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred during the file download operation.", session.UserId);
            }
        }
        
        // Handles a file download initialization request
        private async Task<Packet> HandleFileDownloadInitRequest(Packet packet, ClientSession session)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file download init request with no file ID from user {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "File ID is required.", session.UserId);
                }

                // Initialize the file download
                var fileMetadata = await _fileService.InitializeFileDownload(fileId, session.UserId);
                
                if (fileMetadata == null)
                {
                    _logService.Warning($"Failed to initialize file download for file {fileId} by user {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "File not found or you do not have permission to download it.", session.UserId);
                }

                // Calculate total chunks
                int totalChunks = (int)Math.Ceiling((double)fileMetadata.FileSize / _fileService.ChunkSize);
                
                _logService.Info($"File download initialized: {fileMetadata.FileName} (ID: {fileId}) for user {session.UserId}");
                
                // Create and return the response
                return _packetFactory.CreateFileDownloadInitResponse(
                    true, 
                    fileMetadata.Id, 
                    fileMetadata.FileName, 
                    fileMetadata.FileSize, 
                    fileMetadata.ContentType, 
                    totalChunks, 
                    "File download initialized successfully.", session.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling file download init request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, $"Error initializing file download: {ex.Message}", session.UserId);
            }
        }

        // Handles a file download chunk request
        private async Task<Packet> HandleFileDownloadChunkRequest(Packet packet, ClientSession session)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file download chunk request with no file ID from user {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "File ID is required.", session.UserId);
                }

                // Get chunk index from metadata
                if (!packet.Metadata.TryGetValue("ChunkIndex", out string chunkIndexStr) || 
                    !int.TryParse(chunkIndexStr, out int chunkIndex))
                {
                    _logService.Warning($"Received file download chunk request with invalid chunk index: {chunkIndexStr} from user {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "Valid chunk index is required.", session.UserId);
                }

                // Get the chunk data
                var (chunkData, isLastChunk) = await _fileService.GetFileChunk(fileId, chunkIndex);
                
                if (chunkData == null)
                {
                    _logService.Warning($"Failed to retrieve chunk {chunkIndex} for file {fileId} for user {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, $"Failed to retrieve chunk {chunkIndex}.", session.UserId);
                }
                
                _logService.Debug($"Retrieved chunk {chunkIndex} for file {fileId} for user {session.UserId} (Size: {chunkData.Length} bytes)");
                
                // Create and return the response
                return _packetFactory.CreateFileDownloadChunkResponse(
                    true, 
                    fileId, 
                    chunkIndex, 
                    isLastChunk, 
                    chunkData, 
                    "Chunk retrieved successfully.", session.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling file download chunk request: {ex.Message}", ex);
                string fileId = packet.Metadata.TryGetValue("FileId", out string id) ? id : "";
                int chunkIndex = packet.Metadata.TryGetValue("ChunkIndex", out string idx) && int.TryParse(idx, out int index) ? index : -1;
                
                return _packetFactory.CreateErrorResponse(packet.CommandCode, $"Error retrieving chunk: {ex.Message}", session.UserId);
            }
        }

        
        // Handles a file download complete request
        private async Task<Packet> HandleFileDownloadCompleteRequest(Packet packet, ClientSession session)
        {
            try
            {
                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file download complete request with no file ID from user {session.UserId}");
                    return _packetFactory.CreateFileDownloadCompleteResponse(false, "", "File ID is required.", session.UserId);
                }

                // There's not much to do here except acknowledge that the download is complete
                _logService.Info($"File download completed for file {fileId} by user {session.UserId}");
                
                // We might update statistics or other metadata here in a real implementation
                
                return _packetFactory.CreateFileDownloadCompleteResponse(true, fileId, "File download completed successfully.", session.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling file download complete request: {ex.Message}", ex);
                string fileId = packet.Metadata.TryGetValue("FileId", out string id) ? id : "";
                
                return _packetFactory.CreateFileDownloadCompleteResponse(false, fileId, $"Error completing file download: {ex.Message}", session.UserId);
            }
        }
    }
}