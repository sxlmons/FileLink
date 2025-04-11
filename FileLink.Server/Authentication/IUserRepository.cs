namespace FileLink.Server.Authentication
{
    // Interface for the user repository 
    // Implements the repository pattern for user data storage
    public interface IUserRepository
    {
        // Gets user ID
        Task<User> GetUserById(string userId);
        
        // Gets a user by username 
        Task<User> GetUserByUsername(string username);
        
        // Adds a new user
        Task<bool> AddUser(User user);
        
        // Creates a user (uses add user)
        Task<User> CreateUser(string username, string password, string email, string role);
        
        // Updates an existing user
        Task<bool> UpdateUser(User user);
        
        // Validates a user's credentials
        Task<User> ValidateCredentials(string username, string password);
    }
}