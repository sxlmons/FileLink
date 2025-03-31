
using System;
using System.Threading.Tasks;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands.Directory
{
    // Command handler for directory delete requests.
    // Implements the Command pattern
    public class DirectoryDeleteCommandHandler : ICommandHandler
    {
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        
        // Initializes a new instance of the DirectoryDeleteCommandHandler class
        public DirectoryDeleteCommandHandler(DirectoryService directoryService, LogService logService)
        {
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.DIRECTORY_DELETE_REQUEST;
        }
        
        // Handles a directory delete request packet
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received directory delete request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to delete directories.", "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in directory delete request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
                }

                // Get directory ID from metadata
                if (!packet.Metadata.TryGetValue("DirectoryId", out string directoryId) || string.IsNullOrEmpty(directoryId))
                {
                    _logService.Warning($"Received directory delete request with missing directory ID from user {session.UserId}");
                    return _packetFactory.CreateDirectoryDeleteResponse(false, "", "Directory ID is required.", session.UserId);
                }

                // Get recursive flag from metadata
                bool recursive = false;
                if (packet.Metadata.TryGetValue("Recursive", out string recursiveStr))
                {
                    bool.TryParse(recursiveStr, out recursive);
                }

                // Validate directory exists and is owned by the user
                var directory = await _directoryService.GetDirectoryById(directoryId, session.UserId);
                if (directory == null)
                {
                    _logService.Warning($"Directory not found or not owned by user: {directoryId}");
                    return _packetFactory.CreateDirectoryDeleteResponse(false, directoryId, "Directory not found or you do not have permission to delete it.", session.UserId);
                }

                // Delete the directory
                bool success = await _directoryService.DeleteDirectory(directoryId, session.UserId, recursive);
                
                if (success)
                {
                    _logService.Info($"Directory deleted: {directory.Name} (ID: {directoryId}) for user {session.UserId}, recursive: {recursive}");
                    return _packetFactory.CreateDirectoryDeleteResponse(true, directoryId, "Directory deleted successfully.", session.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to delete directory {directoryId} for user {session.UserId}");
                    return _packetFactory.CreateDirectoryDeleteResponse(false, directoryId, recursive ? 
                            "Failed to delete directory." : 
                            "Failed to delete directory. It may not be empty. Try using recursive deletion.", 
                        session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory delete request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred during directory deletion.", session.UserId);
            }
        }
    }
}