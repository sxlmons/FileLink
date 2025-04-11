using NUnit.Framework;
using Moq;
using FileLink.Server.Authentication;
using FileLink.Server.Services.Logging;
using FileLink.Server.Server;
using System.IO;
using System;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.ServerTests.UnitTests.Authentication
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<LogService> _mockLogService;
        private IAuthenticationService _authService; 
        private string _tempPath;
        
        [SetUp]
        public void Setup()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            _mockLogService = new Mock<LogService>(mockLogger.Object);
            
            _authService = new AuthenticationService(_mockUserRepository.Object, _mockLogService.Object);
            
            // Set up a temporary path for the user directory test
            _tempPath = Path.Combine(Path.GetTempPath(), "FileLink_Test_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempPath);
            
            // Set up ServerEngine.Configuration for the EnsureUserDirectoryExists test
            ServerEngine.Configuration = new ServerConfiguration
            {
                FileStoragePath = _tempPath
            };
        }
        
        [TearDown]
        public void Cleanup()
        {
            // Clean up the temporary directory
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, true);
            }
        }
        
        [Test]
        public async Task Authenticate_ValidCredentials_ReturnsUser()
        {
            // Arrange
            string username = "testuser";
            string password = "password123";
            var expectedUser = new User(username, "test@example.com", "User")
            {
                Id = "user123"
            };
            
            _mockUserRepository.Setup(r => r.ValidateCredentials(username, password))
                .ReturnsAsync(expectedUser);
            
            // Act
            var result = await _authService.Authenticate(username, password);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(expectedUser.Id));
            Assert.That(result.Username, Is.EqualTo(username));
            
            // Verify method was called
            _mockUserRepository.Verify(r => r.ValidateCredentials(username, password), Times.Once);
        }
        
        [Test]
        public async Task Authenticate_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            string password = "wrongpassword";
            
            _mockUserRepository.Setup(r => r.ValidateCredentials(username, password))
                .ReturnsAsync((User)null);
            
            // Act
            var result = await _authService.Authenticate(username, password);
            
            // Assert
            Assert.That(result, Is.Null);
            
            // Verify method was called
            _mockUserRepository.Verify(r => r.ValidateCredentials(username, password), Times.Once);
        }
        
        [Test]
        public async Task Authenticate_EmptyCredentials_ReturnsNull()
        {
            // Arrange - empty username
            string emptyUsername = "";
            string password = "password123";
            
            // Act
            var result1 = await _authService.Authenticate(emptyUsername, password);
            
            // Assert
            Assert.That(result1, Is.Null);
            
            // Arrange - empty password
            string username = "testuser";
            string emptyPassword = "";
            
            // Act
            var result2 = await _authService.Authenticate(username, emptyPassword);
            
            // Assert
            Assert.That(result2, Is.Null);
            
            // Verify method was not called
            _mockUserRepository.Verify(r => r.ValidateCredentials(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        
        [Test]
        public async Task RegisterUser_ValidData_CreatesUserAndReturns()
        {
            // This test is challenging because RegisterUser casts _userRepository to UserRepository
            // For this test, we'll use a more integration-style approach by creating a real UserRepository
            
            // Arrange
            string username = "newuser";
            string password = "password123";
            string email = "new@example.com";
            string role = "User";
            
            // Set up a temporary directory for the user repository
            string userRepoPath = Path.Combine(_tempPath, "UserRepo");
            Directory.CreateDirectory(userRepoPath);
            
            try
            {
                // Create a real UserRepository
                var userRepo = new UserRepository(userRepoPath, _mockLogService.Object);
                
                // Create an AuthenticationService with the real UserRepository
                var authService = new AuthenticationService(userRepo, _mockLogService.Object);
                
                // Act
                var result = await authService.RegisterUser(username, password, role, email);
                
                // Assert
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Username, Is.EqualTo(username));
                Assert.That(result.Email, Is.EqualTo(email));
                Assert.That(result.Role, Is.EqualTo(role));
                
                // Verify the user was created
                var retrievedUser = await userRepo.GetUserByUsername(username);
                Assert.That(retrievedUser, Is.Not.Null);
                Assert.That(retrievedUser.Username, Is.EqualTo(username));
            }
            finally
            {
                // Clean up
                if (Directory.Exists(userRepoPath))
                {
                    Directory.Delete(userRepoPath, true);
                }
            }
        }
        
        [Test]
        public async Task RegisterUser_DuplicateUsername_ReturnsNull()
        {
            // Similar to the previous test, we'll use an integration-style approach
            
            // Arrange
            string username = "existinguser";
            string password = "password123";
            string email = "existing@example.com";
            string role = "User";
            
            // Set up a temporary directory for the user repository
            string userRepoPath = Path.Combine(_tempPath, "UserRepo");
            Directory.CreateDirectory(userRepoPath);
            
            try
            {
                // Create a real UserRepository
                var userRepo = new UserRepository(userRepoPath, _mockLogService.Object);
                
                // Create an AuthenticationService with the real UserRepository
                var authService = new AuthenticationService(userRepo, _mockLogService.Object);
                
                // Create a user with the username first
                await userRepo.CreateUser(username, "otherpassword", "other@example.com", "User");
                
                // Act
                var result = await authService.RegisterUser(username, password, role, email);
                
                // Assert
                Assert.That(result, Is.Null);
            }
            finally
            {
                // Clean up
                if (Directory.Exists(userRepoPath))
                {
                    Directory.Delete(userRepoPath, true);
                }
            }
        }
        
        [Test]
        public void EnsureUserDirectoryExists_ValidUserId_CreatesDirectory()
        {
            // Arrange
            string userId = Guid.NewGuid().ToString();
            string expectedPath = Path.Combine(_tempPath, userId);
            
            // Make sure the directory doesn't exist before the test
            if (Directory.Exists(expectedPath))
            {
                Directory.Delete(expectedPath, true);
            }
            
            try
            {
                // Act - call through reflection since the method is private
                var method = typeof(AuthenticationService).GetMethod("EnsureUserDirectoryExists", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                method.Invoke(_authService, new object[] { userId });
                
                // Assert
                Assert.That(Directory.Exists(expectedPath), Is.True);
            }
            finally
            {
                // Clean up
                if (Directory.Exists(expectedPath))
                {
                    Directory.Delete(expectedPath, true);
                }
            }
        }
        
        [Test]
        public async Task GetUserById_ValidId_ReturnsUser()
        {
            // Arrange
            string userId = "user123";
            var expectedUser = new User("testuser", "test@example.com", "User")
            {
                Id = userId
            };
            
            _mockUserRepository.Setup(r => r.GetUserById(userId))
                .ReturnsAsync(expectedUser);
            
            // Act
            var result = await _authService.GetUserById(userId);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(userId));
            
            // Verify method was called
            _mockUserRepository.Verify(r => r.GetUserById(userId), Times.Once);
        }
    }
}