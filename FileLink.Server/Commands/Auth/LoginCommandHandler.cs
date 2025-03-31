using System.Text.Json;
using FileLink.Server.Authentication;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands.Auth
{
    // Command handler for login requests
    // Implements the command pattern
    public class LoginCommandHandler : ICommandHandler
    {
        private readonly AuthenticationService _authService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        
        // Initializes a new instance of the LoginCommandHandler class
        public LoginCommandHandler(AuthenticationService authService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.LOGIN_REQUEST;
        }
        
        // Handles a login request
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning("Received login request with no payload");
                    return _packetFactory.CreateLoginResponse(false, "Invalid login request. No credentials provided.");
                }

                // Deserialize the payload to extract username and password
                var credentials = JsonSerializer.Deserialize<LoginCredentials>(packet.Payload);
                
                if (string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.Password))
                {
                    _logService.Warning("Received login request with missing credentials");
                    return _packetFactory.CreateLoginResponse(false, "Username and password are required.");
                }

                // Attempt to authenticate
                var user = await _authService.Authenticate(credentials.Username, credentials.Password);
                
                if (user != null)
                {
                    _logService.Info($"User authenticated successfully: {user.Username} (ID: {user.Id})");
                    
                    // Update the client session with the authenticated user
                    session.UserId = user.Id;
                    
                    return _packetFactory.CreateLoginResponse(true, "Authentication successful.", user.Id);
                }
                else
                {
                    _logService.Warning($"Failed login attempt for username: {credentials.Username}");
                    return _packetFactory.CreateLoginResponse(false, "Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing login request: {ex.Message}", ex);
                return _packetFactory.CreateLoginResponse(false, "An error occurred during login.");
            }
        }
        
        // Class for deserializing login credentials from a login request
        private class LoginCredentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}