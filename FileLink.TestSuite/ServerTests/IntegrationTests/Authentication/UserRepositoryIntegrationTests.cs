using NUnit.Framework;
using FileLink.Server.Authentication;
using FileLink.Server.Services.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.ServerTests.IntegrationTests.Authentication
{
    [TestFixture]
    public class UserRepositoryIntegrationTests
    {
        private string _testUsersPath;
        private UserRepository _userRepository;
        private LogService _logService;
        
        [SetUp]
        public void Setup()
        {
            // Create a real logger for integration tests
            var fileLogger = new FileLogger(Path.Combine(Path.GetTempPath(), "FileLink_IntegrationTest.log"));
            _logService = new LogService(fileLogger);
            
            // Create a temporary directory for test users
            _testUsersPath = Path.Combine(Path.GetTempPath(), "FileLink_TestUsers_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testUsersPath);
            
            // Initialize the user repository with the test path
            _userRepository = new UserRepository(_testUsersPath, _logService);
        }
        
        [TearDown]
        public void Cleanup()
        {
            // Clean up the test directory
            if (Directory.Exists(_testUsersPath))
            {
                try
                {
                    Directory.Delete(_testUsersPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up test directory: {ex.Message}");
                    // Continue with test cleanup even if directory deletion fails
                }
            }
        }
        
        // INT-AUTH-001: User Repository Full Lifecycle Integration
        // Tests the full lifecycle of user accounts including creation, retrieval, update, and authentication
        [Test]
        public async Task UserRepository_FullLifecycle_WorksCorrectly()
        {
            // 1. Create a user
            string username = "testuser";
            string password = "P@ssw0rd123!";
            string email = "test@example.com";
            string role = "User";
            
            var createdUser = await _userRepository.CreateUser(username, password, email, role);
            
            // Verify user was created
            Assert.That(createdUser, Is.Not.Null);
            Assert.That(createdUser.Username, Is.EqualTo(username));
            Assert.That(createdUser.Email, Is.EqualTo(email));
            Assert.That(createdUser.Role, Is.EqualTo(role));
            string userId = createdUser.Id;
            
            // 2. Retrieve user by ID
            var retrievedById = await _userRepository.GetUserById(userId);
            Assert.That(retrievedById, Is.Not.Null);
            Assert.That(retrievedById.Id, Is.EqualTo(userId));
            Assert.That(retrievedById.Username, Is.EqualTo(username));
            
            // 3. Retrieve user by username
            var retrievedByUsername = await _userRepository.GetUserByUsername(username);
            Assert.That(retrievedByUsername, Is.Not.Null);
            Assert.That(retrievedByUsername.Id, Is.EqualTo(userId));
            
            // 4. Update user
            retrievedByUsername.Email = "updated@example.com";
            retrievedByUsername.Role = "Admin";
            bool updateResult = await _userRepository.UpdateUser(retrievedByUsername);
            Assert.That(updateResult, Is.True);
            
            // 5. Verify update
            var updatedUser = await _userRepository.GetUserById(userId);
            Assert.That(updatedUser.Email, Is.EqualTo("updated@example.com"));
            Assert.That(updatedUser.Role, Is.EqualTo("Admin"));
            
            // 6. Authenticate with correct password
            var authenticatedUser = await _userRepository.ValidateCredentials(username, password);
            Assert.That(authenticatedUser, Is.Not.Null);
            Assert.That(authenticatedUser.Id, Is.EqualTo(userId));
            Assert.That(authenticatedUser.LastLoginAt, Is.Not.Null);
            
            // 7. Try to authenticate with incorrect password
            var failedAuth = await _userRepository.ValidateCredentials(username, "wrongpassword");
            Assert.That(failedAuth, Is.Null);
        }
        
        // INT-AUTH-002: Password Hashing Integration
        // Verifies that passwords are properly hashed and salted for security
        [Test]
        public async Task UserRepository_PasswordHashing_SecuresUserCredentials()
        {
            // 1. Create a user with a password
            string username = "passworduser";
            string password = "SecurePassword123!";
            string email = "secure@example.com";
            
            var createdUser = await _userRepository.CreateUser(username, password, email, "User");
            Assert.That(createdUser, Is.Not.Null);
            
            // 2. Verify password is stored as a hash, not plaintext
            Assert.That(createdUser.PasswordHash, Is.Not.Null.And.Not.Empty);
            Assert.That(createdUser.PasswordHash, Is.Not.EqualTo(password));
            Assert.That(createdUser.PasswordSalt, Is.Not.Null);
            
            // 3. Validate with correct password
            var validAuth = await _userRepository.ValidateCredentials(username, password);
            Assert.That(validAuth, Is.Not.Null);
            
            // 4. Validate with incorrect password
            var invalidAuth = await _userRepository.ValidateCredentials(username, "WrongPassword123!");
            Assert.That(invalidAuth, Is.Null);
            
            // 5. Validate with similar but slightly different password
            var similarPassword = await _userRepository.ValidateCredentials(username, "SecurePassword123");
            Assert.That(similarPassword, Is.Null);
            
            // 6. Check that different users with the same password have different hashes (due to salt)
            var secondUser = await _userRepository.CreateUser("seconduser", password, "second@example.com", "User");
            Assert.That(secondUser.PasswordHash, Is.Not.EqualTo(createdUser.PasswordHash));
            Assert.That(secondUser.PasswordSalt, Is.Not.EqualTo(createdUser.PasswordSalt));
        }
        
        // INT-AUTH-003: User Data Persistence Loading Integration
        // Verifies that user data is properly loaded from storage
        [Test]
        public async Task UserRepository_DataLoading_LoadsUserDataCorrectly()
        {
            // 1. Create some users
            var user1 = await _userRepository.CreateUser("loaduser1", "password1", "load1@example.com", "User");
            var user2 = await _userRepository.CreateUser("loaduser2", "password2", "load2@example.com", "Admin");
            var user3 = await _userRepository.CreateUser("loaduser3", "password3", "load3@example.com", "User");
            
            Assert.That(user1, Is.Not.Null);
            Assert.That(user2, Is.Not.Null);
            Assert.That(user3, Is.Not.Null);
            
            // 2. Create a new UserRepository instance pointing to the same path
            var newRepository = new UserRepository(_testUsersPath, _logService);
            
            // 3. Verify that the new repository can load and access all users
            var loadedUser1 = await newRepository.GetUserByUsername("loaduser1");
            var loadedUser2 = await newRepository.GetUserByUsername("loaduser2");
            var loadedUser3 = await newRepository.GetUserByUsername("loaduser3");
            
            Assert.That(loadedUser1, Is.Not.Null);
            Assert.That(loadedUser1.Id, Is.EqualTo(user1.Id));
            Assert.That(loadedUser1.Email, Is.EqualTo("load1@example.com"));
            
            Assert.That(loadedUser2, Is.Not.Null);
            Assert.That(loadedUser2.Id, Is.EqualTo(user2.Id));
            Assert.That(loadedUser2.Role, Is.EqualTo("Admin"));
            
            Assert.That(loadedUser3, Is.Not.Null);
            Assert.That(loadedUser3.Id, Is.EqualTo(user3.Id));
            
            // 4. Verify that authentication works with the new repository instance
            var authUser = await newRepository.ValidateCredentials("loaduser2", "password2");
            Assert.That(authUser, Is.Not.Null);
            Assert.That(authUser.Id, Is.EqualTo(user2.Id));
        }
        
        // INT-AUTH-004: User Data Persistence Saving Integration
        // Verifies that user data is properly saved to storage
        [Test]
        public async Task UserRepository_DataSaving_PersistsUserDataCorrectly()
        {
            // 1. Create a user
            var user = await _userRepository.CreateUser("saveuser", "savepassword", "save@example.com", "User");
            Assert.That(user, Is.Not.Null);
            
            // 2. Verify that the user data file was created on disk
            string userJsonPath = Path.Combine(_testUsersPath, "users.json");
            Assert.That(File.Exists(userJsonPath), Is.True);
            
            // 3. Read the file directly and verify it contains the user
            string fileContent = await File.ReadAllTextAsync(userJsonPath);
            Assert.That(fileContent, Is.Not.Null.And.Not.Empty);
            Assert.That(fileContent.Contains(user.Id), Is.True);
            Assert.That(fileContent.Contains("saveuser"), Is.True);
            
            // 4. Update the user
            user.Email = "updated@example.com";
            bool updateResult = await _userRepository.UpdateUser(user);
            Assert.That(updateResult, Is.True);
            
            // 5. Verify the file was updated
            string updatedContent = await File.ReadAllTextAsync(userJsonPath);
            Assert.That(updatedContent.Contains("updated@example.com"), Is.True);
            
            // 6. Create a second user
            var user2 = await _userRepository.CreateUser("saveuser2", "password2", "save2@example.com", "Admin");
            
            // 7. Verify both users are in the file
            string contentWithTwoUsers = await File.ReadAllTextAsync(userJsonPath);
            Assert.That(contentWithTwoUsers.Contains(user.Id), Is.True);
            Assert.That(contentWithTwoUsers.Contains(user2.Id), Is.True);
            
            // 8. Parse the JSON directly to verify structure
            var usersList = JsonSerializer.Deserialize<List<User>>(contentWithTwoUsers);
            Assert.That(usersList, Is.Not.Null);
            
            // Note: UserRepository creates a default admin user during initialization,
            // so we expect at least 3 users (default admin + our 2 created users)
            Assert.That(usersList.Count, Is.GreaterThanOrEqualTo(3));
            
            // Instead of checking the count, we'll verify our specific users exist
            // Verify the first user's data
            var jsonUser1 = usersList.FirstOrDefault(u => u.Id == user.Id);
            Assert.That(jsonUser1, Is.Not.Null);
            Assert.That(jsonUser1.Username, Is.EqualTo("saveuser"));
            Assert.That(jsonUser1.Email, Is.EqualTo("updated@example.com"));
            
            // Verify the second user's data
            var jsonUser2 = usersList.FirstOrDefault(u => u.Id == user2.Id);
            Assert.That(jsonUser2, Is.Not.Null);
            Assert.That(jsonUser2.Username, Is.EqualTo("saveuser2"));
            Assert.That(jsonUser2.Role, Is.EqualTo("Admin"));
            
            // Verify the default admin user exists
            var adminUser = usersList.FirstOrDefault(u => u.Username == "admin");
            Assert.That(adminUser, Is.Not.Null);
        }
    }
}