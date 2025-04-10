// Authentication/UserRepositoryTests.cs
using FileLink.Server.Authentication;
using FileLink.Server.Services.Logging;
using MockFactory = FileLink.TestSuite.ServerTests.TestUtilities.MockFactory;

namespace FileLink.TestSuite.ServerTests.UnitTests.Authentication
{
    [TestClass]
    public class UserRepositoryTests
    {
        private string _testDataPath;
        private LogService _logService;

        [TestInitialize]
        public void Initialize()
        {
            // Create a temporary directory for test data
            _testDataPath = Path.Combine(Path.GetTempPath(), "FileLink_Tests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataPath);
            
            // Create log service with mock logger
            _logService = MockFactory.CreateLogService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Delete the temporary test directory
            if (Directory.Exists(_testDataPath))
                Directory.Delete(_testDataPath, true);
        }

        [TestMethod]
        public async Task CreateUser_WithValidData_ShouldReturnUser()
        {
            // Arrange
            var userRepository = new UserRepository(_testDataPath, _logService);
            string username = "newuser";
            string password = "StrongP@ssw0rd";
            string email = "newuser@example.com";
            string role = "User";

            // Act
            var user = await userRepository.CreateUser(username, password, email, role);

            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual(username, user.Username);
            Assert.AreEqual(email, user.Email);
            Assert.AreEqual(role, user.Role);
            Assert.IsNotNull(user.Id);
            Assert.IsNotNull(user.PasswordHash);
            Assert.IsNotNull(user.PasswordSalt);
        }

        [TestMethod]
        public async Task GetUserById_WithExistingId_ShouldReturnUser()
        {
            // Arrange
            var userRepository = new UserRepository(_testDataPath, _logService);
            var createdUser = await userRepository.CreateUser("testuser", "P@ssw0rd", "test@example.com", "User");
            string userId = createdUser.Id;

            // Act
            var retrievedUser = await userRepository.GetUserById(userId);

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual(userId, retrievedUser.Id);
            Assert.AreEqual("testuser", retrievedUser.Username);
        }

        [TestMethod]
        public async Task GetUserByUsername_WithExistingUsername_ShouldReturnUser()
        {
            // Arrange
            var userRepository = new UserRepository(_testDataPath, _logService);
            await userRepository.CreateUser("uniqueuser", "P@ssw0rd", "unique@example.com", "User");

            // Act
            var user = await userRepository.GetUserByUsername("uniqueuser");

            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("uniqueuser", user.Username);
        }

        [TestMethod]
        public async Task ValidateCredentials_WithCorrectPassword_ShouldReturnUser()
        {
            // Arrange
            var userRepository = new UserRepository(_testDataPath, _logService);
            string username = "validationuser";
            string password = "TestP@ssw0rd";
            await userRepository.CreateUser(username, password, "validation@example.com", "User");

            // Act
            var user = await userRepository.ValidateCredentials(username, password);

            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual(username, user.Username);
            Assert.IsNotNull(user.LastLoginAt, "Last login timestamp should be updated");
        }

        [TestMethod]
        public async Task ValidateCredentials_WithIncorrectPassword_ShouldReturnNull()
        {
            // Arrange
            var userRepository = new UserRepository(_testDataPath, _logService);
            string username = "wrongpassuser";
            await userRepository.CreateUser(username, "CorrectP@ssw0rd", "wrong@example.com", "User");

            // Act
            var user = await userRepository.ValidateCredentials(username, "WrongP@ssw0rd");

            // Assert
            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task AddUser_WithDuplicateUsername_ShouldReturnFalse()
        {
            // Arrange
            var userRepository = new UserRepository(_testDataPath, _logService);
            string username = "duplicateuser";
            await userRepository.CreateUser(username, "P@ssw0rd1", "duplicate1@example.com", "User");
            
            // Create a second user with the same username
            var duplicateUser = new User(username, "duplicate2@example.com", "User");

            // Act
            bool result = await userRepository.AddUser(duplicateUser);

            // Assert
            Assert.IsFalse(result);
        }
    }
}