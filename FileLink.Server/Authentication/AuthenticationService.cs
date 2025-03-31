using FileLink.Server.Core.Exceptions;
using FileLink.Server.Server;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Authentication
{
    // Service that provides authentication and user management functionality
    public class AuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly LogService _logService;
        
        // Initializes a new instance of the AuthenticationService class
        public AuthenticationService(IUserRepository userRepository, LogService logService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Authenticates a user with a username and password
        public async Task<User> Authenticate(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logService.Warning("Authentication attempt with empty username or password");
                    return null;
                }

                _logService.Debug($"Authentication attempt for username: {username}");
                
                // Validate credentials
                var user = await _userRepository.ValidateCredentials(username, password);
                
                if (user != null)
                {
                    _logService.Info($"User authenticated successfully: {username} (ID: {user.Id})");
                    
                    // Create user directory for file storage if it doesn't exist
                    EnsureUserDirectoryExists(user.Id);
                    
                    return user;
                }
                else
                {
                    _logService.Warning($"Authentication failed for username: {username}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during authentication: {ex.Message}", ex);
                throw new AuthenticationException("Authentication failed.", ex);
            }
        }
        
        // Registers a new user
        public async Task<User> RegisterUser(string username, string password, string role, string email = "")
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logService.Warning("Registration attempt with empty username or password");
                    return null;
                }

                // Check if a user with this username already exists
                var existingUser = await _userRepository.GetUserByUsername(username);
                if (existingUser != null)
                {
                    _logService.Warning($"Registration failed. Username already exists: {username}");
                    return null;
                }

                _logService.Info($"Registering new user: {username}");
                
                // Create the user
                var userRepository = _userRepository as UserRepository;
                if (userRepository == null)
                {
                    throw new AuthenticationException("User repository does not support user creation.");
                }
                
                var user = await userRepository.CreateUser(username, password, email, role);
                
                if (user != null)
                {
                    _logService.Info($"User registered successfully: {username} (ID: {user.Id})");
                    
                    // Create user directory for file storage
                    EnsureUserDirectoryExists(user.Id);
                    
                    return user;
                }
                else
                {
                    _logService.Warning($"Registration failed for username: {username}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error during user registration: {ex.Message}", ex);
                throw new AuthenticationException("User registration failed.", ex);
            }
        }
        
        // Ensures that the user's directory for file storage exists, otherwise creates one
        private void EnsureUserDirectoryExists(string userId)
        {
            try
            {
                // Get the configuration
                var config = ServerEngine.Configuration;
                if (config == null)
                {
                    _logService.Warning("Server configuration not available. Cannot create user directory.");
                    return;
                }

                // Create the user's directory
                string userDirectory = Path.Combine(config.FileStoragePath, userId);
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                    _logService.Debug($"Created user directory: {userDirectory}");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error ensuring user directory exists: {ex.Message}", ex);
            }
        }
        
        // Gets a user by ID
        public Task<User> GetUserById(string userId)
        {
            return _userRepository.GetUserById(userId);
        }
        
        // Gets a user by username
        public Task<User> GetUserByUsername(string username)
        {
            return _userRepository.GetUserByUsername(username);
        }
        
        // Updates a user
        public Task<bool> UpdateUser(User user)
        {
            return _userRepository.UpdateUser(user);
        }
    }
}
















