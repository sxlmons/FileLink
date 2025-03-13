namespace FileLink.Server.Data.Models;

// Represents a user in our cloud service
public class User
{
    // Unique ID for the user.
    public string Id { get; set; }
    
    // Username assigned to the user.
    public string Username { get; set; }
    
    // Email address for the user.
    public string Email { get; set; }
    
    // The password hash of the user
    public string PasswordHash { get; set; }
    
    // The salt used to hash the password.
    public string PasswordSalt { get; set; }
    
    // Timestamp of when the file was created.
    public DateTime CreatedAt { get; set; }
        
    // Timestamp of when the file was last updated.
    public DateTime UpdatedAt { get; set; }
        
    // Timestamp of when the file was last accessed.
    public DateTime? LastLoginAt { get; set; }
    
    // Initializes a new instance of the User class
    public User()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Initializes a new instance of the User class with the specified username and email.
    /// </summary>
    /// <param name="username">The username for the user</param>
    /// <param name="email">The email address of the user</param>
    public User(string username, string email) : this()
    {
        Username = username;
        Email = email;
    }
    
}