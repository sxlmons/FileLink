using System.Security.Cryptography;
using System.Text.Json;
using FileLink.Server.Core.Exceptions;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Authentication
{
    // Repository for user data storage 
    // Implements the repository pattern 
    public class UserRepository
    {
        private readonly string _usersPath;
        private readonly object _lock = new object();
        private Dictionary<string, User> _users = new Dictionary<string, User>();
        private readonly LogService _logService;
        
        // Initializes a new instance of the UserRepository class
        public UserRepository(string usersPath, LogService logService)
        {
            _usersPath = usersPath ?? throw new ArgumentNullException(nameof(usersPath));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Ensure the users directory exists
            Directory.CreateDirectory(_usersPath);
            
            // Load users from storage
            LoadUsers().Wait();
        }

        // Gets a user by ID
        public Task<User> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Task.FromResult<User>(null);

            lock (_lock)
            {
                _users.TryGetValue(userId, out User user);
                return Task.FromResult(user);
            }
        }
        
        // Gets a user by username
        public Task<User> GetUserByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return Task.FromResult<User>(null);

            lock (_lock)
            {
                var user = _users.Values.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(user);
            }
        }
        
        // Creates a user with specified username and password
        public async Task<User> CreateUser(string username, string password, string email, string role)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new ArgumentException("Username and password are required");
            
            // Check if the username is already taken 
            var existingUser = await GetUserByUsername(username);
            if (existingUser != null)
            {
                _logService.Warning($"User with username '{username}' already exists");
                return null;
            }
            
            // Create new user 
            var user = new User(username, email, role);
            
            // Generate a salt and hash the password
            user.PasswordSalt = GenerateSalt();
            user.PasswordHash = HashPassword(password, user.PasswordSalt);
            
            // Add the user to the repository
            bool success = await AddUser(user);
            if (success)
            {
                _logService.Info($"User created: {username} (ID: {user.Id})");
                return user;
            }

            return null;
        }
        
        // Adds a new user
        public async Task<bool> AddUser(User user)
        {
            if  (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (string.IsNullOrEmpty(user.Username))
                throw new ArgumentNullException(nameof(user.Username));
            
            // Check if a user with the same username already exists
            var existingUser = await GetUserByUsername(user.Username);
            if (existingUser != null)
            {
                _logService.Warning($"Attempted to add user '{user.Username}' to user '{existingUser.Username}'.");
                return false;
            }

            lock (_lock)
            {
                _users[user.Id] = user;
            }
            
            // Save changes to storage
            await SaveUsers();
            _logService.Info($"User Added: '{user.Username}' (ID: '{user.Id}').");
            return true;
        }   
        
        // Updates an existing user
        public async Task<bool> UpdateUser(User user)
        {
            if  (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (string.IsNullOrEmpty(user.Id))
                throw new ArgumentNullException(nameof(user.Id));

            lock (_lock)
            {
                if (!_users.ContainsKey(user.Id))
                {
                    _logService.Warning($"Attempted to update non-existent user '{user.Username}' ID: '{user.Id}'.");
                    return false;
                }
                _users[user.Id] = user;
            }
            
            // Save changes to storage
            await SaveUsers();
            _logService.Debug($"User Updated: '{user.Username}' (ID: '{user.Id}').");
            return true;
        }
        
        // Validates a user's credentials
        public async Task<User> ValidateCredentials(string username, string password)
        {
            if  (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;
            
            var user = await GetUserByUsername(username);
            if (user == null)
                return null;
            
            // Verify the password
            if (VerifyPassword(password, user.PasswordSalt, user.PasswordHash))
            {
                // Update the last login time
                user.UpdateLastLogin();
                await UpdateUser(user);
                return user;
            }
            return null;
        }
        
        // Save all users to storage
         private async Task SaveUsers()
        {
            try
            {
                string filePath = Path.Combine(_usersPath, "users.json");
                
                // Create a copy of the users dictionary to avoid holding the lock during file I/O
                Dictionary<string, User> usersCopy;
                lock (_lock)
                {
                    usersCopy = new Dictionary<string, User>(_users);
                }
                
                // Convert to a list for serialization
                var usersList = usersCopy.Values.ToList();
                
                // Serialize to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string json = JsonSerializer.Serialize(usersList, options);
                
                // Write to file
                await File.WriteAllTextAsync(filePath, json);
                
                _logService.Debug($"Users saved to {filePath}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error saving users: {ex.Message}", ex);
                throw new AuthenticationException("Failed to save users.", ex);
            }
        }

        // Load all users from storage
        private async Task LoadUsers()
        {
            try
            {
                string filePath = Path.Combine(_usersPath, "users.json");

                if (!File.Exists(filePath))
                {
                    _logService.Info($"Users file not found at '{filePath}'. Creating a new one.");
                    
                    // Create an admin user if no user files exist
                    await CreateDefaultAdminUser();

                    return;
                }
                
                // Read the file
                string json = await File.ReadAllTextAsync(filePath);
                
                // Deserialize from JSON
                var usersList = JsonSerializer.Deserialize<List<User>>(json);
                
                // Build the dictionary
                Dictionary<string, User> usersDict = new Dictionary<string, User>();
                foreach (var user in usersList)
                {
                    if (!string.IsNullOrEmpty(user.Id))
                    {
                        usersDict[user.Id] = user;
                    }
                }
                
                // Update the users dictionary
                lock (_lock)
                {
                    _users = usersDict;
                }
                
                _logService.Info($"Loaded {usersList.Count} users from '{filePath}'.");
            }
            catch (Exception ex)
            {
                _logService.Error($"Something went wrong while loading users: {ex.Message}", ex);
                
                // If we can't load the users, create a default admin user
                if (_users.Count == 0)
                {
                    await CreateDefaultAdminUser();
                }
            }
        }
        
        // Creates a default admin user
        private async Task CreateDefaultAdminUser()
        {
            try
            {
                // Create a default admin user 
                var adminUser = new User("admin", "admin@example.com", "Admin");
                adminUser.PasswordSalt = GenerateSalt();
                adminUser.PasswordHash = HashPassword("admin", adminUser.PasswordSalt);
                
                // Add the admin user
                lock (_lock)
                {
                    _users[adminUser.Id] = adminUser;
                }
                
                // Save changes to storage
                await SaveUsers();
                
                _logService.Info($"Admin Created: '{adminUser.Username}' (ID: '{adminUser.Id}') at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Something went wrong while creating users: {ex.Message}", ex);
            }
        }
        
        
        // Generate a random salt for password hashing
        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        
        // Hashes a password with a salt using PBKDF2
        private string HashPassword(string password, byte[] salt)
        {
            const int iterations = 10000;
            const int hashSize = 32; // 256 bits

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(hashSize);
                return Convert.ToBase64String(hash);
            }
        }
        
        // Verifies a password against a hash
        private bool VerifyPassword(string password, byte[] salt, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || salt == null || string.IsNullOrEmpty(storedHash))
                return false;
            
            string computedHash = HashPassword(password, salt);
            return string.Equals(computedHash, storedHash);
        }
    }
}