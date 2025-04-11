using System.Threading.Tasks;

namespace FileLink.Server.Authentication
{
    // Interface for authentication-related services
    public interface IAuthenticationService
    {
        Task<User> Authenticate(string username, string password);
        Task<User> RegisterUser(string username, string password, string role, string email = "");
        Task<User> GetUserById(string userId);
        Task<User> GetUserByUsername(string username);
        Task<bool> UpdateUser(User user);
    }
}