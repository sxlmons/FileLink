namespace FileLink.Client.Models
{
    // Represents a user in the system
    public class User
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        
        // Create an instance of the user
        public User(string id, string username, string email = "")
        {
            Id = id;
            Username = username;
            Email = email;
        }
    }
}