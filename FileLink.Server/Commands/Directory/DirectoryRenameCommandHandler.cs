using System.Text.Json;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands.Directory
{
    // Command handler for directory rename requests.
    // Implements the Command pattern
    public class DirectoryRenameCommandHandler : ICommandHandler
    {
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        
        // Initializes a new instance of the DirectoryRenameCommandHandler class
        public DirectoryRenameCommandHandler(DirectoryService directoryService, LogService logService)
        {
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.DIRECTORY_RENAME_REQUEST;
        }
        
        // Handles a directory rename request packet
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received directory rename request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to rename directories.", "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in directory rename request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
                }

                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning($"Received directory rename request with no payload from user {session.UserId}");
                    return _packetFactory.CreateDirectoryRenameResponse(false, "", "", "Directory rename information is required.", session.UserId);
                }

                // Deserialize the payload to extract rename information
                var renameInfo = JsonSerializer.Deserialize<DirectoryRenameInfo>(packet.Payload);
                
                if (string.IsNullOrEmpty(renameInfo.DirectoryId))
                {
                    _logService.Warning($"Received directory rename request with missing directory ID from user {session.UserId}");
                    return _packetFactory.CreateDirectoryRenameResponse(false, "", "", "Directory ID is required.", session.UserId);
                }

                if (string.IsNullOrEmpty(renameInfo.NewName))
                {
                    _logService.Warning($"Received directory rename request with missing new name from user {session.UserId}");
                    return _packetFactory.CreateDirectoryRenameResponse(false, renameInfo.DirectoryId, "", "New directory name is required.", session.UserId);
                }

                // Validate directory exists and is owned by the user
                var directory = await _directoryService.GetDirectoryById(renameInfo.DirectoryId, session.UserId);
                if (directory == null)
                {
                    _logService.Warning($"Directory not found or not owned by user: {renameInfo.DirectoryId}");
                    return _packetFactory.CreateDirectoryRenameResponse(false, renameInfo.DirectoryId, "", "Directory not found or you do not have permission to rename it.", session.UserId);
                }

                // Rename the directory
                bool success = await _directoryService.RenameDirectory(renameInfo.DirectoryId, renameInfo.NewName, session.UserId);
                
                if (success)
                {
                    _logService.Info($"Directory renamed: {directory.Name} to {renameInfo.NewName} (ID: {renameInfo.DirectoryId}) for user {session.UserId}");
                    return _packetFactory.CreateDirectoryRenameResponse(true, renameInfo.DirectoryId, renameInfo.NewName, "Directory renamed successfully.", session.UserId);
                }
                else
                {
                    _logService.Warning($"Failed to rename directory {renameInfo.DirectoryId} for user {session.UserId}");
                    return _packetFactory.CreateDirectoryRenameResponse(false, renameInfo.DirectoryId, renameInfo.NewName, "Failed to rename directory. Another directory might already exist with the same name.", session.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory rename request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred during directory renaming.", session.UserId);
            }
        }

        // Class for deserializing directory rename information from a directory rename request.
        private class DirectoryRenameInfo
        {
            public string DirectoryId { get; set; }
            public string NewName { get; set; }
        }
    }
}