using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FileLink.Client.Connection;
using FileLink.Client.Protocol;

namespace FileLink.Client.Session
{
    
    // Manages authentication with the cloud file server.
    
    public class AuthenticationManager
    {
        /*
        // =============================== for bypassing auth 
        private bool _bypassAuth = true;
        private string? _userId = "test-user-id"; // Pre-set a user ID for testing
        public bool IsAuthenticated => _bypassAuth || !string.IsNullOrEmpty(_userId);
        public string UserId => _userId ?? "test-user-id"; // Return test ID if null
        */
        
        
        private readonly CloudServerConnection _connection;
        private readonly PacketFactory _packetFactory;
        
        private string? _userId;
        
        // Gets a value indicating whether the client is authenticated.
        public bool IsAuthenticated => !string.IsNullOrEmpty(_userId);
        
        // Gets the user ID of the authenticated user.
        public string UserId => _userId ?? throw new InvalidOperationException("Not authenticated");

        
        // Initializes a new instance of the AuthenticationManager class.
        
        public AuthenticationManager(CloudServerConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _packetFactory = new PacketFactory();
            _connection.ConnectionClosed += OnConnectionClosed;
        }
        
        // Handles the connection closed event.
        private void OnConnectionClosed(object? sender, EventArgs e)
        {
            // Clear authentication state when the connection is closed
            _userId = null;
        }

        
        // Registers a new user account
        public async Task<(bool Success, string Message)> CreateAccountAsync(string username, string password, string email = "", CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure connection
                await _connection.EnsureConnectedAsync(cancellationToken);

                // Create and send the account creation request
                var request = _packetFactory.CreateAccountCreationRequest(username, password, email);
                var response = await _connection.SendAndReceiveAsync(
                    request,
                    Commands.CommandCode.CREATE_ACCOUNT_RESPONSE,
                    cancellationToken: cancellationToken);

                // Check if the account creation was successful
                bool success = response.IsSuccess();
                string message = response.GetMessage();

                // If successful, extract the user ID
                if (success && response.Payload != null)
                {
                    try
                    {
                        var jsonResponse = JsonSerializer.Deserialize<AccountCreationResponse>(response.Payload);
                        if (jsonResponse?.UserId != null)
                        {
                            // Don't set _userId here as the user still needs to log in
                            return (true, $"Account created successfully: {message}");
                        }
                    }
                    catch (JsonException ex)
                    {
                        return (false, $"Error parsing account creation response: {ex.Message}");
                    }
                }

                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating account: {ex.Message}");
            }
        }

        
        // Logs in to the server
        public async Task<(bool Success, string Message)> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                // Clear existing authentication state
                _userId = null;

                // Ensure connection
                await _connection.EnsureConnectedAsync(cancellationToken);

                // Create and send the login request
                var request = _packetFactory.CreateLoginRequest(username, password);
                var response = await _connection.SendAndReceiveAsync(
                    request,
                    Commands.CommandCode.LOGIN_RESPONSE,
                    cancellationToken: cancellationToken);

                // Check if the login was successful
                bool success = response.IsSuccess();
                string message = response.GetMessage();

                // If successful, extract the user ID and save it
                if (success && response.Payload != null)
                {
                    try
                    {
                        var jsonResponse = JsonSerializer.Deserialize<LoginResponse>(response.Payload);
                        if (!string.IsNullOrEmpty(response.UserId))
                        {
                            _userId = response.UserId;
                            return (true, message);
                        }
                    }
                    catch (JsonException ex)
                    {
                        return (false, $"Error parsing login response: {ex.Message}");
                    }
                }

                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error logging in: {ex.Message}");
            }
        }

        
        // Logs out from the server
        public async Task<(bool Success, string Message)> LogoutAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if already logged out
                if (!IsAuthenticated)
                {
                    return (true, "Already logged out");
                }

                // Ensure connection
                await _connection.EnsureConnectedAsync(cancellationToken);

                // Create and send the logout request
                var request = _packetFactory.CreateLogoutRequest(_userId!);
                var response = await _connection.SendAndReceiveAsync(
                    request,
                    Commands.CommandCode.LOGOUT_RESPONSE,
                    cancellationToken: cancellationToken);

                // Clear authentication state regardless of server response
                _userId = null;

                // Check if the logout was successful
                bool success = response.IsSuccess();
                string message = response.GetMessage();

                return (success, message);
            }
            catch (Exception ex)
            {
                // Clear authentication state on error
                _userId = null;
                
                return (false, $"Error logging out: {ex.Message}");
            }
        }

        
        // Ensures the client is authenticated before performing an operation
        public Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated)
            {
                throw new AuthenticationException("Not authenticated. Please log in first.");
            }
            
            return Task.CompletedTask;
        }
    }
    
    // Response from a login request
    internal class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    
    // Response from an account creation request
    internal class AccountCreationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }
    
    // Exception thrown when there's an authentication error
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
        public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }
}