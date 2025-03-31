using FileLink.Server.Disk.FileManagement;
using FileLink.Server.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands
{
    // Command handler for file deletion requests
    // Implements the Command pattern
    public class FileDeleteCommandHandler : ICommandHandler
    {
        // This class is dependent on FileService, LogService and our PacketFactory
        private readonly FileService _fileService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();


        // Initializes a new instance of the FileDeleteCommandHandler class
        public FileDeleteCommandHandler(FileService fileService, LogService logService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_DELETE_REQUEST;
        }
        
        // Handles a file delete request packet
        // First will check for authentication, then will match the fileId and call the DeleteFile service
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received file delete request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to delete files.", "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in file delete request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
                }

                // Get file ID from metadata
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || string.IsNullOrEmpty(fileId))
                {
                    _logService.Warning($"Received file delete request with no file ID from user {session.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(false, "", "File ID is required.", session.UserId);
                }

                // Delete the file
                bool success = await _fileService.DeleteFile(fileId, session.UserId);
                
                if (success)
                {
                    _logService.Info($"File deleted: {fileId} by user {session.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(true, fileId, "File deleted successfully.", session.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to delete file: {fileId} by user {session.UserId}");
                    return _packetFactory.CreateFileDeleteResponse(false, fileId, "Failed to delete file. File not found or you do not have permission to delete it.", session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file delete request: {ex.Message}", ex);
                string fileId = packet.Metadata.TryGetValue("FileId", out string id) ? id : "";
                
                return _packetFactory.CreateFileDeleteResponse(false, fileId, $"Error deleting file: {ex.Message}", session.UserId);
            }
        }
        
        
        
        
        
        
        
        
        
        
        
    }
}