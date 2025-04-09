using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FileLink.Client.Protocol;
using NUnit.Framework;
using FileLink.Client.Services;

namespace FileLink.TestSuite.SystemTesting
{
    [TestFixture]
    public class ClientSystemTests
    {
        // Current services
        private NetworkService _networkService;
        private AuthenticationService _authService;
        private FileService _fileService;
        private DirectoryService _directoryService;
        
        // Test server configuration (from environment or config)
        private readonly string _serverAddress = "localhost";
        private readonly int _serverPort = 9000;
        
        // Test user credentials
        private readonly string _testUsername = "admin";
        private readonly string _testPassword = "admin";

        private static string ComputeSHA256(byte[] data)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Initialize services
            _networkService = new NetworkService();
            _networkService.SetServer(_serverAddress, _serverPort);
            
            _authService = new AuthenticationService(_networkService);
            _fileService = new FileService(_networkService);
            _directoryService = new DirectoryService(_networkService);
            
            // Connect to server
            bool connected = await _networkService.ConnectAsync();
            Assert.That(connected, Is.True, "Should connect to server");
            
            // Login with test credentials
            var (success, message) = await _authService.LoginAsync(_testUsername, _testPassword);
            Assert.That(success, Is.True, $"Login should succeed. Error: {message}");
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            // Logout and disconnect
            if (_authService.IsLoggedIn)
            {
                await _authService.LogoutAsync();
            }
            
            _networkService.Disconnect();
        }

        [Test]
        public async Task Test_AuthenticationService_LoginLogout()
        {
            // Test login with valid credentials
            
            // Test login with invalid credentials
            
            // Test logout
        }

        [Test]
        public async Task Test_FileService_UploadDownloadDelete()
        {
            // Create test file
            string content = "This is test file content";
            string tempPath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempPath, content);
            
            try
            {
                // Upload
                string userId = _authService.CurrentUser?.Id;
                Assert.That(userId, Is.Not.Null, "User ID should not be null");
                
                var uploadedFile = await _fileService.UploadFileAsync(tempPath, null, userId);
                Assert.That(uploadedFile, Is.Not.Null, "Uploaded file should not be null");
                Assert.That(uploadedFile.Id, Is.Not.Empty, "File ID should not be empty");
                
                // Download
                string downloadPath = Path.Combine(Path.GetTempPath(), "downloaded_" + Path.GetFileName(tempPath));
                var (success, filePath) = await _fileService.DownloadFileAsync(uploadedFile.Id, downloadPath, userId);
                
                Assert.That(success, Is.True, "Download should succeed");
                Assert.That(File.Exists(downloadPath), Is.True, "Downloaded file should exist");
                
                // Verify content
                long originalSize = new FileInfo(tempPath).Length;
                long downloadedSize = new FileInfo(downloadPath).Length;
                Assert.That(downloadedSize, Is.EqualTo(originalSize), "File sizes should match");
                
                // Delete
                bool deleted = await _fileService.DeleteFileAsync(uploadedFile.Id, userId);
                Assert.That(deleted, Is.True, "Delete should succeed");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Test]
        public async Task Test_DirectoryService_CreateListDelete()
        {
            // Test creating a directory
            
            // Test listing directory contents
            
            // Test deleting a directory
        }
    }
}