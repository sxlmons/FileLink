using System.Text.Json;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands.Directory
{
    // Command handler for directory creation requests.
    // Implements the Command pattern.
    public class DirectoryCreateCommandHandler : ICommandHandler
    {
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        
        // Initializes a new instance of the DirectoryCreateCommandHandler class
        public DirectoryCreateCommandHandler(DirectoryService directoryService, LogService logService)
        {
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.DIRECTORY_CREATE_REQUEST;
        }

        
        // Handles a directory creation request packet
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received directory creation request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to create directories.", "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in directory creation request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
                }

                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning($"Received directory creation request with no payload from user {session.UserId}");
                    return _packetFactory.CreateDirectoryCreateResponse(false, "", "", "Directory information is required.", session.UserId);
                }

                // Deserialize the payload to extract directory information
                var directoryInfo = JsonSerializer.Deserialize<DirectoryCreateInfo>(packet.Payload);
                
                if (string.IsNullOrEmpty(directoryInfo.DirectoryName))
                {
                    _logService.Warning($"Received directory creation request with missing name from user {session.UserId}");
                    return _packetFactory.CreateDirectoryCreateResponse(false, "", "", "Directory name is required.", session.UserId);
                }

                // Create the directory
                var directoryMetadata = await _directoryService.CreateDirectory(
                    session.UserId, 
                    directoryInfo.DirectoryName, 
                    directoryInfo.ParentDirectoryId);
                
                if (directoryMetadata == null)
                {
                    _logService.Warning($"Failed to create directory '{directoryInfo.DirectoryName}' for user {session.UserId}");
                    return _packetFactory.CreateDirectoryCreateResponse(false, "", directoryInfo.DirectoryName, "Failed to create directory. It may already exist with the same name.", session.UserId);
                }
                
                _logService.Info($"Directory created: {directoryMetadata.Name} (ID: {directoryMetadata.Id}) for user {session.UserId}");
                
                return _packetFactory.CreateDirectoryCreateResponse(true, directoryMetadata.Id, directoryMetadata.Name, "Directory created successfully.", session.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory creation request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred during directory creation.", session.UserId);
            }
        }
        
        // Class for deserializing directory creation information from a directory creation request.
        private class DirectoryCreateInfo
        {
            public string DirectoryName { get; set; }
            public string ParentDirectoryId { get; set; }
        }
    }
}