using FileLink.Client.Models;
using FileLink.Client.Protocol;
using System;
using System.Threading.Tasks;
using FileLink.Client.Protocol;

namespace FileLink.Client.Services
{

    // Provides authentication services for the client
    public class AuthenticationService
    {
        private readonly NetworkService _networkService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        private User? _currentUser;


        // Gets the currently logged in user, if any

        public User? CurrentUser => _currentUser;


        // Gets a value indicating whether the user is logged in

        public bool IsLoggedIn => _currentUser != null;


        // Initializes a new instance of the AuthenticationService class
        public AuthenticationService(NetworkService networkService)
        {
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        }


        // Attempts to create a new user account
        public async Task<(bool Success, string Message, string UserId)> CreateAccountAsync(string username, string password, string email = "")
        {
            try
            {
                // Create the account creation request packet
                var packet = _packetFactory.CreateAccountCreationRequest(username, password, email);
                
                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);
                
                if (response == null)
                    return (false, "No response from server", "");
                
                // Extract the response data
                var (success, message, userId) = _packetFactory.ExtractAccountCreationResponse(response);
                
                return (success, message, userId);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating account: {ex.Message}", "");
            }
        }

        // Attempts to log in with the provided credentials
        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            try
            {
                // Create the login request packet
                var packet = _packetFactory.CreateLoginRequest(username, password);
                
                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);
                
                if (response == null)
                    return (false, "No response from server");
                
                // Extract the response data
                var (success, message, userId) = _packetFactory.ExtractLoginResponse(response);
                
                if (success && !string.IsNullOrEmpty(userId))
                {
                    // Store the current user
                    _currentUser = new User(userId, username);
                }
                
                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error during login: {ex.Message}");
            }
        }
        
        // Attempts to log out the current user
        public async Task<(bool Success, string Message)> LogoutAsync()
        {
            try
            {
                if (!IsLoggedIn)
                    return (false, "Not logged in");
                
                // Create the logout request packet
                var packet = _packetFactory.CreateLogoutRequest(_currentUser!.Id);
                
                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);
                
                if (response == null)
                    return (false, "No response from server");
                
                // Extract the response data
                var (success, message) = _packetFactory.ExtractLogoutResponse(response);
                
                if (success)
                {
                    // Clear the current user
                    _currentUser = null;
                }
                
                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error during logout: {ex.Message}");
            }
        }
    }
}