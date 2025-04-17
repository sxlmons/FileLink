namespace FileLink.Server.Authentication;

// Represents a user in our cloud service
public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    
    // Initializes a new instance of the User class
    public User()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        Role = "User"; // Default role
    }

 
    // Initializes a new instance of the User class with the specified username and email.
    public User(string username, string email, string role) : this()
    {
        Username = username;
        Email = email;
        Role = role;
    }

    // Updates the user's last login time
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}