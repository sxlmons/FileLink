using NUnit.Framework;
using FileLink.Server.Authentication;
using FileLink.Server.Services.Logging;
using FileLink.Server.Server;
using System;
using System.IO;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.ServerTests.IntegrationTests.Authentication
{
    [TestFixture]
    public class AuthenticationServiceIntegrationTests
    {
        private string _testUsersPath;
        private string _testFilesPath;
        private UserRepository _userRepository;
        private AuthenticationService _authService;
        private LogService _logService;
        private ServerConfiguration _config;
        
        [SetUp]
        public void Setup()
        {
            // Create a real logger for integration tests
            var fileLogger = new FileLogger(Path.Combine(Path.GetTempPath(), "FileLink_AuthIntegrationTest.log"));
            _logService = new LogService(fileLogger);
            
            // Create temporary directories for tests
            string testRoot = Path.Combine(Path.GetTempPath(), "FileLink_AuthTest_" + Guid.NewGuid().ToString());
            _testUsersPath = Path.Combine(testRoot, "users");
            _testFilesPath = Path.Combine(testRoot, "files");
            
            Directory.CreateDirectory(_testUsersPath);
            Directory.CreateDirectory(_testFilesPath);
            
            // Initialize server configuration
            _config = new ServerConfiguration
            {
                UsersDataPath = _testUsersPath,
                FileStoragePath = _testFilesPath
            };
            
            // Set up configuration for the EnsureUserDirectoryExists method
            ServerEngine.Configuration = _config;
            
            // Initialize the user repository
            _userRepository = new UserRepository(_testUsersPath, _logService);
            
            // Initialize the authentication service
            _authService = new AuthenticationService(_userRepository, _logService);
        }
        
        [TearDown]
        public void Cleanup()
        {
            // Clean up the test directories
            string testRoot = Path.GetDirectoryName(_testUsersPath);
            if (Directory.Exists(testRoot))
            {
                try
                {
                    Directory.Delete(testRoot, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up test directory: {ex.Message}");
                    // Continue with test cleanup even if directory deletion fails
                }
            }
        }
        
        /// <summary>
        /// INT-AUTH-005: Authentication Service Integration
        /// Tests the login and authentication flow
        /// </summary>
        [Test]
        public async Task AuthenticationService_Authentication_HandlesLoginFlowCorrectly()
        {
            // 1. Register a test user
            string username = "authuser";
            string password = "P@ssw0rd123!";
            string email = "auth@example.com";
            string role = "User";
            
            var createdUser = await _authService.RegisterUser(username, password, role, email);
            Assert.That(createdUser, Is.Not.Null);
            string userId = createdUser.Id;
            
            // 2. Test successful authentication with correct credentials
            var authenticatedUser = await _authService.Authenticate(username, password);
            Assert.That(authenticatedUser, Is.Not.Null);
            Assert.That(authenticatedUser.Id, Is.EqualTo(userId));
            Assert.That(authenticatedUser.Username, Is.EqualTo(username));
            Assert.That(authenticatedUser.LastLoginAt, Is.Not.Null);
            
            // 3. Test failed authentication with incorrect password
            var failedPasswordAuth = await _authService.Authenticate(username, "WrongPassword123!");
            Assert.That(failedPasswordAuth, Is.Null);
            
            // 4. Test failed authentication with non-existent username
            var failedUsernameAuth = await _authService.Authenticate("nonexistentuser", password);
            Assert.That(failedUsernameAuth, Is.Null);
            
            // 5. Test failed authentication with empty credentials
            var failedEmptyAuth = await _authService.Authenticate("", "");
            Assert.That(failedEmptyAuth, Is.Null);
            
            // 6. Test that last login time is updated after successful authentication
            var lastLoginTime = authenticatedUser.LastLoginAt;
            
            // Wait a moment to ensure timestamps will be different
            await Task.Delay(1000);
            
            var authenticatedAgain = await _authService.Authenticate(username, password);
            Assert.That(authenticatedAgain.LastLoginAt, Is.GreaterThan(lastLoginTime));
            
            // 7. Verify user can still be retrieved after authentication
            var retrievedUser = await _authService.GetUserById(userId);
            Assert.That(retrievedUser, Is.Not.Null);
            Assert.That(retrievedUser.Id, Is.EqualTo(userId));
        }
        
        /// <summary>
        /// INT-AUTH-006: User Registration Integration
        /// Tests creation of new user accounts
        /// </summary>
        [Test]
        public async Task AuthenticationService_Registration_CreatesNewUserAccountsCorrectly()
        {
            // 1. Register a user with all fields
            string username1 = "reguser1";
            string password1 = "RegP@ssw0rd123!";
            string email1 = "reg1@example.com";
            string role1 = "User";
            
            var user1 = await _authService.RegisterUser(username1, password1, role1, email1);
            Assert.That(user1, Is.Not.Null);
            Assert.That(user1.Username, Is.EqualTo(username1));
            Assert.That(user1.Email, Is.EqualTo(email1));
            Assert.That(user1.Role, Is.EqualTo(role1));
            
            // 2. Register a user with minimal fields (no email)
            string username2 = "reguser2";
            string password2 = "RegP@ssw0rd456!";
            string role2 = "Admin";
            
            var user2 = await _authService.RegisterUser(username2, password2, role2);
            Assert.That(user2, Is.Not.Null);
            Assert.That(user2.Username, Is.EqualTo(username2));
            Assert.That(user2.Email, Is.EqualTo("")); // Default empty email
            Assert.That(user2.Role, Is.EqualTo(role2));
            
            // 3. Test duplicate username
            var duplicateUser = await _authService.RegisterUser(username1, "DifferentPassword", "User");
            Assert.That(duplicateUser, Is.Null);
            
            // 4. Test invalid credentials
            var emptyUsername = await _authService.RegisterUser("", password1, "User");
            Assert.That(emptyUsername, Is.Null);
            
            var emptyPassword = await _authService.RegisterUser("validname", "", "User");
            Assert.That(emptyPassword, Is.Null);
            
            // 5. Verify users can be retrieved
            var retrievedUser1 = await _authService.GetUserByUsername(username1);
            Assert.That(retrievedUser1, Is.Not.Null);
            Assert.That(retrievedUser1.Id, Is.EqualTo(user1.Id));
            
            var retrievedUser2 = await _authService.GetUserByUsername(username2);
            Assert.That(retrievedUser2, Is.Not.Null);
            Assert.That(retrievedUser2.Id, Is.EqualTo(user2.Id));
            
            // 6. Verify user counts and user persistence
            // Create a new repository instance to verify persistence
            var newRepository = new UserRepository(_testUsersPath, _logService);
            var newAuthService = new AuthenticationService(newRepository, _logService);
            
            var retrievedAgain1 = await newAuthService.GetUserByUsername(username1);
            Assert.That(retrievedAgain1, Is.Not.Null);
            Assert.That(retrievedAgain1.Id, Is.EqualTo(user1.Id));
            
            var retrievedAgain2 = await newAuthService.GetUserByUsername(username2);
            Assert.That(retrievedAgain2, Is.Not.Null);
            Assert.That(retrievedAgain2.Id, Is.EqualTo(user2.Id));
        }
        
        /// <summary>
        /// INT-AUTH-007: User Directory Structure Integration
        /// Tests that proper directory structures are created for users
        /// </summary>
        [Test]
        public async Task AuthenticationService_DirectoryStructure_CreatesUserStorageDirectories()
        {
            // 1. Register a user
            string username = "diruser";
            string password = "DirP@ssw0rd123!";
            
            var user = await _authService.RegisterUser(username, password, "User");
            Assert.That(user, Is.Not.Null);
            string userId = user.Id;
            
            // 2. Verify that the user's directory was created
            string expectedUserDirectory = Path.Combine(_testFilesPath, userId);
            Assert.That(Directory.Exists(expectedUserDirectory), Is.True);
            
            // 3. Register multiple users and verify all directories
            var user2 = await _authService.RegisterUser("diruser2", "password2", "User");
            var user3 = await _authService.RegisterUser("diruser3", "password3", "Admin");
            
            string expectedDirectory2 = Path.Combine(_testFilesPath, user2.Id);
            string expectedDirectory3 = Path.Combine(_testFilesPath, user3.Id);
            
            Assert.That(Directory.Exists(expectedDirectory2), Is.True);
            Assert.That(Directory.Exists(expectedDirectory3), Is.True);
            
            // 4. Verify authentication maintains directories
            var authUser = await _authService.Authenticate(username, password);
            Assert.That(authUser, Is.Not.Null);
            Assert.That(Directory.Exists(expectedUserDirectory), Is.True);
            
            // 5. Test directory creation for existing user
            // First, delete the directory to simulate it missing
            Directory.Delete(expectedUserDirectory);
            Assert.That(Directory.Exists(expectedUserDirectory), Is.False);
            
            // Authenticate again - should recreate the directory
            var reAuthUser = await _authService.Authenticate(username, password);
            Assert.That(reAuthUser, Is.Not.Null);
            Assert.That(Directory.Exists(expectedUserDirectory), Is.True);
            
            // 6. Verify user directory structure is correct
            var rootDirectoryInfo = new DirectoryInfo(_testFilesPath);
            var userDirectories = rootDirectoryInfo.GetDirectories();
            
            // Should have at least the 3 users we explicitly created
            // Note: While UserRepository creates a default admin user in the database,
            // the directory is only created when that user authenticates
            Assert.That(userDirectories.Length, Is.GreaterThanOrEqualTo(3));
            
            // Verify all user directories exist
            Assert.That(userDirectories.Any(d => d.Name == userId), Is.True);
            Assert.That(userDirectories.Any(d => d.Name == user2.Id), Is.True);
            Assert.That(userDirectories.Any(d => d.Name == user3.Id), Is.True);
        }
    }
}