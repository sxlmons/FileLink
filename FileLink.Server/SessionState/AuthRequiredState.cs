using System.Text.Json;
using FileLink.Server.Authentication;
using FileLink.Server.Core.Exceptions;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.SessionState
{
    // Represents the authentication-required state in the session state machine 
    public class AuthRequiredState : ISessionState
    {
        private readonly AuthenticationService _authService;
        private readonly LogService  _logService;
        private readonly PacketFactory  _packetFactory = new PacketFactory();
        private int _failedLoginAttempts = 0;
        private const int MaxFailedLoginAttempts = 5;
        
        // Gets the client session this state is associated with 
        public ClientSession ClientSession { get; }
        
        // Initializes a new instance of the AuthRequiredState class
        public AuthRequiredState(ClientSession clientSession, AuthenticationService authService, LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task<Packet> HandlePacket(Packet packet)
        {
            try
            {
                switch (packet.CommandCode)
                {
                    case FileLink.Server.Protocol.Commands.CommandCode.LOGIN_REQUEST: return await HandleLoginRequest(packet);
                    
                    case FileLink.Server.Protocol.Commands.CommandCode.CREATE_ACCOUNT_REQUEST: return await HandleCreateAccountRequest(packet);
                    
                    default: 
                        _logService.Warning($"Unknown CommandCode {packet.CommandCode}");
                        return _packetFactory.CreateErrorResponse(packet.CommandCode, "Authentication Required, please login or create an account first", packet.UserId);
                }
            }
            catch (Exception ex) 
            {
                _logService.Error($"Error handling packet in AuthRequiredState: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred while processing your request.", packet.UserId);
            }
        }

        // Handles a login request, will return a task representing the async operation: containing the response packet
        private async Task<Packet> HandleLoginRequest(Packet packet)
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
                    _failedLoginAttempts++;
                    return _packetFactory.CreateLoginResponse(false, "Username and password are required.");
                }

                // Attempt to authenticate
                var user = await _authService.Authenticate(credentials.Username, credentials.Password);
                
                if (user != null)
                {
                    _logService.Info($"User authenticated successfully: {user.Username} (ID: {user.Id})");
                    
                    // Update the client session with the authenticated user
                    ClientSession.UserId = user.Id;
                    
                    // Transition to authenticated state
                    ClientSession.TransitionToState(ClientSession.StateFactory.CreateAuthenticatedState(ClientSession));
                    
                    return _packetFactory.CreateLoginResponse(true, "Authentication successful.", user.Id);
                }
                else
                {
                    _failedLoginAttempts++;
                    _logService.Warning($"Failed login attempt {_failedLoginAttempts} for username: {credentials.Username}");
                    
                    if (_failedLoginAttempts >= MaxFailedLoginAttempts)
                    {
                        _logService.Warning($"Max failed login attempts reached for session {ClientSession.SessionId}. Disconnecting.");
                        await ClientSession.Disconnect("Too many failed login attempts");
                        return _packetFactory.CreateLoginResponse(false, "Too many failed login attempts. Connection closed.");
                    }
                    
                    return _packetFactory.CreateLoginResponse(false, "Invalid username or password.");
                }
            }
            catch (AuthenticationException ex)
            {
                _logService.Error($"Authentication error: {ex.Message}", ex);
                return _packetFactory.CreateLoginResponse(false, "Authentication error: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logService.Error($"Unexpected error during login: {ex.Message}", ex);
                return _packetFactory.CreateLoginResponse(false, "An unexpected error occurred during login.");
            }
        }

        // Handles an account creation request
        private async Task<Packet> HandleCreateAccountRequest(Packet packet)
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
                var user = await _authService.RegisterUser(accountInfo.Username, accountInfo.Password, "User");
                
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
            catch (AuthenticationException ex)
            {
                _logService.Error($"Account creation error: {ex.Message}", ex);
                return _packetFactory.CreateAccountCreationResponse(false, "Account creation error: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logService.Error($"Unexpected error during account creation: {ex.Message}", ex);
                return _packetFactory.CreateAccountCreationResponse(false, "An unexpected error occurred during account creation.");
            }
        }

        public Task OnEnter()
        {
            _logService.Debug($"Session {ClientSession.SessionId} entered AuthRequiredState");
            _failedLoginAttempts = 0;
            return Task.CompletedTask;
        }

        public Task OnExit()
        {
            _logService.Debug($"Session {ClientSession.SessionId} exited AuthRequiredState");
            return Task.CompletedTask;
        }

        private class LoginCredentials
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        private class AccountCreationInfo
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
        }
    }
}