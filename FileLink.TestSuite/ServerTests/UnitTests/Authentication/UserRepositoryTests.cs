using NUnit.Framework;
using Moq;
using FileLink.Server.Authentication;
using FileLink.Server.Services.Logging;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.ServerTests.UnitTests.Authentication
{
    [TestFixture]
    public class UserRepositoryTests
    {
        private Mock<LogService> _mockLogService;
        private string _testUsersPath;
        private UserRepository _userRepository;
        
        [SetUp]
        public void Setup()
        {
            // Create a temporary directory for test users
            _testUsersPath = Path.Combine(Path.GetTempPath(), "FileLink_TestUsers_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testUsersPath);
            
            // Create mock log service
            var mockLogger = new Mock<ILogger>();
            _mockLogService = new Mock<LogService>(mockLogger.Object);

            // Initialize the user repository with the test path
            _userRepository = new UserRepository(_testUsersPath, _mockLogService.Object);
        }
        
        [TearDown]
        public void Cleanup()
        {
            // Clean up the test directory
            if (Directory.Exists(_testUsersPath))
            {
                Directory.Delete(_testUsersPath, true);
            }
        }
        
        [Test]
        public async Task GetUserById_ExistingUser_ReturnsUser()
        {
            // Arrange
            var user = await _userRepository.CreateUser("testuser", "password123", "test@example.com", "User");
            Assert.That(user, Is.Not.Null, "Failed to create test user");
            
            // Act
            var result = await _userRepository.GetUserById(user.Id);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(user.Id));
            Assert.That(result.Username, Is.EqualTo("testuser"));
        }
        
        [Test]
        public async Task GetUserById_NonExistingUser_ReturnsNull()
        {
            // Arrange - use non-existent user ID
            string nonExistentUserId = Guid.NewGuid().ToString();
            
            // Act
            var result = await _userRepository.GetUserById(nonExistentUserId);
            
            // Assert
            Assert.That(result, Is.Null);
        }
        
        [Test]
        public async Task GetUserByUsername_ExistingUsername_ReturnsUser()
        {
            // Arrange
            var user = await _userRepository.CreateUser("findme", "password123", "find@example.com", "User");
            Assert.That(user, Is.Not.Null, "Failed to create test user");
            
            // Act
            var result = await _userRepository.GetUserByUsername("findme");
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Username, Is.EqualTo("findme"));
            Assert.That(result.Id, Is.EqualTo(user.Id));
        }
        
        [Test]
        public async Task GetUserByUsername_NonExistingUsername_ReturnsNull()
        {
            // Act
            var result = await _userRepository.GetUserByUsername("nonexistentuser");
            
            // Assert
            Assert.That(result, Is.Null);
        }
        
        [Test]
        public async Task CreateUser_ValidData_CreatesUser()
        {
            // Arrange
            string username = "newuser";
            string password = "secure_password";
            string email = "new@example.com";
            string role = "Admin";
            
            // Act
            var user = await _userRepository.CreateUser(username, password, email, role);
            
            // Assert
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Username, Is.EqualTo(username));
            Assert.That(user.Email, Is.EqualTo(email));
            Assert.That(user.Role, Is.EqualTo(role));
            Assert.That(user.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(user.PasswordHash, Is.Not.Null.And.Not.Empty);
            Assert.That(user.PasswordSalt, Is.Not.Null);
            
            // Verify we can retrieve the created user
            var retrievedUser = await _userRepository.GetUserById(user.Id);
            Assert.That(retrievedUser, Is.Not.Null);
            Assert.That(retrievedUser.Username, Is.EqualTo(username));
        }
        
        [Test]
        public async Task CreateUser_DuplicateUsername_ReturnsNull()
        {
            // Arrange
            await _userRepository.CreateUser("duplicatetest", "password", "first@example.com", "User");
            
            // Act
            var duplicateUser = await _userRepository.CreateUser("duplicatetest", "different_password", "second@example.com", "Admin");
            
            // Assert
            Assert.That(duplicateUser, Is.Null);
        }
        
        [Test]
        public async Task AddUser_ValidUser_ReturnsTrue()
        {
            // Arrange
            var user = new User("addeduser", "added@example.com", "User");
            user.PasswordHash = "hashedpassword";
            user.PasswordSalt = new byte[] { 1, 2, 3, 4 };
            
            // Act
            bool result = await _userRepository.AddUser(user);
            
            // Assert
            Assert.That(result, Is.True);
            
            // Verify the user was added
            var retrievedUser = await _userRepository.GetUserById(user.Id);
            Assert.That(retrievedUser, Is.Not.Null);
            Assert.That(retrievedUser.Username, Is.EqualTo("addeduser"));
        }
        
        [Test]
        public void AddUser_NullUser_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _userRepository.AddUser(null));
        }
        
        [Test]
        public async Task UpdateUser_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var user = await _userRepository.CreateUser("updateme", "password", "update@example.com", "User");
            Assert.That(user, Is.Not.Null, "Failed to create test user");
            
            // Modify the user
            user.Email = "updated@example.com";
            user.Role = "Admin";
            
            // Act
            bool result = await _userRepository.UpdateUser(user);
            
            // Assert
            Assert.That(result, Is.True);
            
            // Verify the user was updated
            var updatedUser = await _userRepository.GetUserById(user.Id);
            Assert.That(updatedUser, Is.Not.Null);
            Assert.That(updatedUser.Email, Is.EqualTo("updated@example.com"));
            Assert.That(updatedUser.Role, Is.EqualTo("Admin"));
        }
        
        [Test]
        public async Task UpdateUser_NonExistingUser_ReturnsFalse()
        {
            // Arrange
            var nonExistentUser = new User("nonexistent", "nonexistent@example.com", "User");
            nonExistentUser.Id = Guid.NewGuid().ToString(); // Ensure it's a new ID that doesn't exist
            
            // Act
            bool result = await _userRepository.UpdateUser(nonExistentUser);
            
            // Assert
            Assert.That(result, Is.False);
        }
        
        [Test]
        public async Task ValidateCredentials_ValidCredentials_ReturnsUser()
        {
            // Arrange
            string username = "validuser";
            string password = "correct_password";
            await _userRepository.CreateUser(username, password, "valid@example.com", "User");
            
            // Act
            var user = await _userRepository.ValidateCredentials(username, password);
            
            // Assert
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Username, Is.EqualTo(username));
            Assert.That(user.LastLoginAt, Is.Not.Null); // Should update last login time
        }
        
        [Test]
        public async Task ValidateCredentials_InvalidPassword_ReturnsNull()
        {
            // Arrange
            string username = "passworduser";
            await _userRepository.CreateUser(username, "correct_password", "pwd@example.com", "User");
            
            // Act
            var user = await _userRepository.ValidateCredentials(username, "wrong_password");
            
            // Assert
            Assert.That(user, Is.Null);
        }
        
        [Test]
        public async Task ValidateCredentials_NonExistentUser_ReturnsNull()
        {
            // Act
            var user = await _userRepository.ValidateCredentials("nonexistentuser", "anypassword");
            
            // Assert
            Assert.That(user, Is.Null);
        }
    }
}