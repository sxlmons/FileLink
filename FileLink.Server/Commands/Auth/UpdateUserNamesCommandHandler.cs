using System.Text.Json;
using FileLink.Server.Authentication;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands.Auth
{
    // Command handler for updating user names
    // implements the command pattern
    public class UpdateUserNamesCommandHandler : ICommandHandler
    {
        private readonly IAuthenticationService _authService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        
        // Initializes a new instance of the UpdateUserNamesCommandHandler class
        public UpdateUserNamesCommandHandler(IAuthenticationService authService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.UPDATE_USER_NAMES_REQUEST;
        }
        
        // Handles the update user names request
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning("Received update user names request with no payload");
                    return _packetFactory.CreateUpdateUserNamesResponse(false, "Invalid update request. No information provided.");
                }

                // Check if user is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Unauthorized attempt to update user names");
                    return _packetFactory.CreateUpdateUserNamesResponse(false, "User must be authenticated to update names.");
                }
                
                // Deserialize the payload to extract user information
                var userInfo = JsonSerializer.Deserialize<UserNameInfo>(packet.Payload);
                
                // Attempt to update the user names
                bool success = await _authService.UpdateUserNames(
                    session.UserId, 
                    userInfo.FirstName, 
                    userInfo.LastName);
                
                if (success)
                {
                    _logService.Info($"Names updated for user ID: {session.UserId}");
                    return _packetFactory.CreateUpdateUserNamesResponse(true, "User names updated successfully.");
                }
                else
                {
                    _logService.Warning($"Failed to update names for user ID: {session.UserId}");
                    return _packetFactory.CreateUpdateUserNamesResponse(false, "Failed to update user names.");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing update user names request: {ex.Message}", ex);
                return _packetFactory.CreateUpdateUserNamesResponse(false, "An error occurred during name update.");
            }
        }
        
        // Class for deserializing user name information from an update request
        private class UserNameInfo
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }
    }
}