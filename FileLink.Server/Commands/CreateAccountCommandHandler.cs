using System.Text.Json;
using FileLink.Server.Authentication;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands
{
    // Command handler for account creation requests 
    // implements the command pattern
    public class CreateAccountCommandHandler : ICommandHandler
    {
        private readonly AuthenticationService _authService;
        private readonly LogService _logService;
        private readonly PacketFactory  _packetFactory = new PacketFactory(); 
        
        // Initializes a new instance of the CreateAccountHandler class
        public CreateAccountCommandHandler(AuthenticationService authService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.CREATE_ACCOUNT_REQUEST;
        }
        
        // Handles the account creation packet
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning("Received account creation request with no payload");
                    return _packetFactory.CreateAccountCreationResponse(false, "Invalid account creation request. No information provided.");
                }

                // Deserialize the payload to extract user information
                var accountInfo = JsonSerializer.Deserialize<AccountCreationInfo>(packet.Payload);
                
                if (string.IsNullOrEmpty(accountInfo.Username) || string.IsNullOrEmpty(accountInfo.Password))
                {
                    _logService.Warning("Received account creation request with missing required fields");
                    return _packetFactory.CreateAccountCreationResponse(false, "Username and password are required.");
                }

                // Attempt to create the account
                var user = await _authService.RegisterUser(accountInfo.Username, accountInfo.Password, "User", accountInfo.Email);
                
                if (user != null)
                {
                    _logService.Info($"Account created successfully: {user.Username} (ID: {user.Id})");
                    return _packetFactory.CreateAccountCreationResponse(true, "Account created successfully.", user.Id);
                }
                else
                {
                    _logService.Warning($"Failed to create account for username: {accountInfo.Username}");
                    return _packetFactory.CreateAccountCreationResponse(false, "Failed to create account. Username may already be taken.");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing account creation request: {ex.Message}", ex);
                return _packetFactory.CreateAccountCreationResponse(false, "An error occurred during account creation.");
            }
        }
        
        // class for deserializing account creation information from an account creation request
        private class AccountCreationInfo
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
        }
        
    }
}