using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using FileLink.Client.Services;
using FileLink.Client.Models;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.SystemTests
{
    // Base class for all FileLink system tests.
    // Handles common setup and teardown operations.
    [TestFixture]
    public abstract class FileLinkSystemTestBase
    {
        // Services used across all tests
        protected NetworkService NetworkService;
        protected AuthenticationService AuthService;
        protected FileService FileService;
        protected DirectoryService DirectoryService;
        
        // Test configuration
        protected readonly string TestServerAddress = "localhost";
        protected readonly int TestServerPort = 9000;
        protected readonly string TestUsername = "testuser";
        protected readonly string TestPassword = "testpassword";
        protected readonly string TestEmail = "test@example.com";
        
        // Test data tracking for cleanup
        protected List<string> CreatedDirectoryIds = new List<string>();
        protected List<string> UploadedFileIds = new List<string>();

        [OneTimeSetUp]
        public virtual async Task OneTimeSetUp()
        {
            // Initialize services
            NetworkService = new NetworkService();
            NetworkService.SetServer(TestServerAddress, TestServerPort);
            
            AuthService = new AuthenticationService(NetworkService);
            FileService = new FileService(NetworkService);
            DirectoryService = new DirectoryService(NetworkService);
            
            // Ensure server connection
            bool connected = await NetworkService.ConnectAsync();
            Assert.That(connected, Is.True, "Failed to connect to the test server");
            
            // Ensure test user exists (try to register, ok if it fails because user exists)
            try
            {
                var result = await AuthService.CreateAccountAsync(TestUsername, TestPassword, TestEmail);
                // We don't assert on this as it might fail if user already exists
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User creation exception (might be ok if user exists): {ex.Message}");
            }
            
            // Login with test user
            var (success, message) = await AuthService.LoginAsync(TestUsername, TestPassword);
            Assert.That(success, Is.True, $"Failed to login as test user: {message}");
        }

        [OneTimeTearDown]
        public virtual async Task OneTimeTearDown()
        {
            // Clean up test data
            if (AuthService.IsLoggedIn)
            {
                // Delete any files that were uploaded during tests
                foreach (var fileId in UploadedFileIds)
                {
                    try
                    {
                        await FileService.DeleteFileAsync(fileId, AuthService.CurrentUser.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting test file {fileId}: {ex.Message}");
                    }
                }
                
                // Delete any directories that were created during tests (in reverse order to handle nesting)
                for (int i = CreatedDirectoryIds.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        await DirectoryService.DeleteDirectoryAsync(CreatedDirectoryIds[i], true, AuthService.CurrentUser.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting test directory {CreatedDirectoryIds[i]}: {ex.Message}");
                    }
                }
                
                // Logout
                await AuthService.LogoutAsync();
            }
            
            // Disconnect from server
            NetworkService.Disconnect();
        }
        
        // Creates a temporary test file of specified size for upload testing
        protected string CreateTestFile(string fileName, int sizeInKB = 100)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "FileLink_Tests");
            Directory.CreateDirectory(tempDir);
            
            string filePath = Path.Combine(tempDir, fileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                // Create a file of specified size
                byte[] buffer = new byte[1024]; // 1KB chunks
                Random random = new Random();
                
                for (int i = 0; i < sizeInKB; i++)
                {
                    random.NextBytes(buffer);
                    fileStream.Write(buffer, 0, buffer.Length);
                }
            }
            
            return filePath;
        }
        
        /// <summary>
        /// Generates a unique name for test resources
        /// </summary>
        protected string GenerateUniqueName(string prefix)
        {
            return $"{prefix}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }

    // Tests for the authentication functionality
    [TestFixture]
    public class AuthenticationTests : FileLinkSystemTestBase
    {
        [Test]
        public async Task Login_WithValidCredentials_ShouldSucceed()
        {
            // Arrange - Using test user from base setup
            await AuthService.LogoutAsync(); // Ensure logged out state
            
            // Act
            var (success, message) = await AuthService.LoginAsync(TestUsername, TestPassword);
            
            // Assert
            Assert.That(success, Is.True, $"Login failed: {message}");
            Assert.That(AuthService.CurrentUser, Is.Not.Null, "Current user should be set after login");
            Assert.That(AuthService.CurrentUser.Id, Is.Not.Empty, "User ID should not be empty after login");
            Assert.That(AuthService.CurrentUser.Username, Is.EqualTo(TestUsername), "Username should match the login credentials");
        }
        
        [Test]
        public async Task Login_WithInvalidCredentials_ShouldFail()
        {
            // Arrange
            await AuthService.LogoutAsync(); // Ensure logged out state
            string invalidPassword = "wrongpassword";
            
            // Act
            var (success, message) = await AuthService.LoginAsync(TestUsername, invalidPassword);
            
            // Assert
            Assert.That(success, Is.False, "Login should fail with invalid credentials");
            Assert.That(AuthService.CurrentUser, Is.Null, "Current user should be null after failed login");
        }
        
        [Test]
        public async Task Logout_WhenLoggedIn_ShouldSucceed()
        {
            // Arrange - Ensure logged in
            if (!AuthService.IsLoggedIn)
            {
                await AuthService.LoginAsync(TestUsername, TestPassword);
            }
            
            // Act
            var (success, message) = await AuthService.LogoutAsync();
            
            // Assert
            Assert.That(success, Is.True, $"Logout failed: {message}");
            Assert.That(AuthService.IsLoggedIn, Is.False, "Should not be logged in after logout");
            Assert.That(AuthService.CurrentUser, Is.Null, "Current user should be null after logout");
            
            // Cleanup - Login again for other tests
            await AuthService.LoginAsync(TestUsername, TestPassword);
        }
    }

    // Tests for directory management functionality
    [TestFixture]
    public class DirectoryManagementTests : FileLinkSystemTestBase
    {
        [Test]
        public async Task CreateDirectory_InRoot_ShouldSucceed()
        {
            // Arrange
            string directoryName = GenerateUniqueName("TestDir");
            
            // Act
            DirectoryItem directory = await DirectoryService.CreateDirectoryAsync(
                directoryName, 
                null, // parent = root
                AuthService.CurrentUser.Id);
            
            // Assert
            Assert.That(directory, Is.Not.Null, "Created directory should not be null");
            Assert.That(directory.Name, Is.EqualTo(directoryName), "Directory name should match");
            Assert.That(directory.ParentDirectoryId, Is.Null, "Root directory should have null parent ID");
            
            // Add to tracking for cleanup
            CreatedDirectoryIds.Add(directory.Id);
        }
        
        [Test]
        public async Task CreateDirectory_InParentDirectory_ShouldSucceed()
        {
            // Arrange - Create parent directory first
            string parentName = GenerateUniqueName("ParentDir");
            string childName = GenerateUniqueName("ChildDir");
            
            var parentDir = await DirectoryService.CreateDirectoryAsync(
                parentName, 
                null, // parent = root
                AuthService.CurrentUser.Id);
            
            Assert.That(parentDir, Is.Not.Null, "Failed to create parent directory for test");
            CreatedDirectoryIds.Add(parentDir.Id);
            
            // Act
            var childDir = await DirectoryService.CreateDirectoryAsync(
                childName, 
                parentDir.Id,
                AuthService.CurrentUser.Id);
            
            // Assert
            Assert.That(childDir, Is.Not.Null, "Created child directory should not be null");
            Assert.That(childDir.Name, Is.EqualTo(childName), "Child directory name should match");
            Assert.That(childDir.ParentDirectoryId, Is.EqualTo(parentDir.Id), "Parent ID should match");
            
            // Add to tracking for cleanup
            CreatedDirectoryIds.Add(childDir.Id);
        }
        
        [Test]
        public async Task GetDirectoryContents_ShouldReturnFilesAndFolders()
        {
            // Arrange - Create a test directory with content
            string dirName = GenerateUniqueName("ContentTestDir");
            string childDirName = GenerateUniqueName("ChildDir");
            
            var parentDir = await DirectoryService.CreateDirectoryAsync(
                dirName, 
                null, // parent = root
                AuthService.CurrentUser.Id);
            
            Assert.That(parentDir, Is.Not.Null, "Failed to create parent directory for test");
            CreatedDirectoryIds.Add(parentDir.Id);
            
            // Create child directory
            var childDir = await DirectoryService.CreateDirectoryAsync(
                childDirName, 
                parentDir.Id,
                AuthService.CurrentUser.Id);
            
            Assert.That(childDir, Is.Not.Null, "Failed to create child directory for test");
            CreatedDirectoryIds.Add(childDir.Id);
            
            // Upload a test file to the directory
            string testFileName = $"{GenerateUniqueName("TestFile")}.txt";
            string filePath = CreateTestFile(testFileName);
            
            var uploadedFile = await FileService.UploadFileAsync(
                filePath, 
                parentDir.Id,
                AuthService.CurrentUser.Id);
            
            Assert.That(uploadedFile, Is.Not.Null, "Failed to upload test file");
            UploadedFileIds.Add(uploadedFile.Id);
            
            // Act
            var (files, directories) = await DirectoryService.GetDirectoryContentsAsync(
                parentDir.Id,
                AuthService.CurrentUser.Id);
            
            // Assert
            Assert.That(files, Is.Not.Null, "Files list should not be null");
            Assert.That(directories, Is.Not.Null, "Directories list should not be null");
            Assert.That(files.Count, Is.EqualTo(1), "Should have one file in directory");
            Assert.That(directories.Count, Is.EqualTo(1), "Should have one subdirectory");
            
            // Verify the file details
            Assert.That(files[0].FileName, Is.EqualTo(testFileName), "File name should match");
            Assert.That(files[0].Id, Is.Not.Empty, "File ID should not be empty");
            
            // Verify the directory details
            Assert.That(directories[0].Name, Is.EqualTo(childDirName), "Directory name should match");
            Assert.That(directories[0].Id, Is.EqualTo(childDir.Id), "Directory ID should match");
        }
        
        [Test]
        public async Task DeleteDirectory_ShouldSucceed()
        {
            // Arrange - Create a test directory
            string dirName = GenerateUniqueName("DeleteTestDir");
            
            var directory = await DirectoryService.CreateDirectoryAsync(
                dirName, 
                null, // parent = root
                AuthService.CurrentUser.Id);
            
            Assert.That(directory, Is.Not.Null, "Failed to create directory for delete test");
            
            // Act
            bool success = await DirectoryService.DeleteDirectoryAsync(
                directory.Id, 
                true, // recursive
                AuthService.CurrentUser.Id);
            
            // Assert
            Assert.That(success, Is.True, "Directory deletion should succeed");
            
            // Verify directory no longer exists by trying to get its contents
            try
            {
                var (files, directories) = await DirectoryService.GetDirectoryContentsAsync(
                    directory.Id,
                    AuthService.CurrentUser.Id);
                
                // This should not be reached or should return empty collections
                Assert.That(files.Count == 0 && directories.Count == 0, Is.True, 
                    "Directory should not exist or have no contents after deletion");
            }
            catch
            {
                // Exception is expected if the directory no longer exists
                Assert.Pass("Directory no longer exists as expected");
            }
        }
    }

    // Tests for file operations functionality
    [TestFixture]
    public class FileOperationTests : FileLinkSystemTestBase
    {
        private string _testDirectoryId;
        
        [OneTimeSetUp]
        public override async Task OneTimeSetUp()
        {
            await base.OneTimeSetUp();
            
            // Create a test directory for file operations
            string dirName = GenerateUniqueName("FileTestDir");
            
            var directory = await DirectoryService.CreateDirectoryAsync(
                dirName, 
                null, // parent = root
                AuthService.CurrentUser.Id);
            
            Assert.That(directory, Is.Not.Null, "Failed to create directory for file tests");
            _testDirectoryId = directory.Id;
            CreatedDirectoryIds.Add(_testDirectoryId);
        }
        
        [Test]
        public async Task UploadFile_ShouldSucceed()
        {
            // Arrange
            string fileName = $"{GenerateUniqueName("UploadTest")}.txt";
            string filePath = CreateTestFile(fileName, 100); // 100KB file
            
            // Act
            FileItem uploadedFile = await FileService.UploadFileAsync(
                filePath, 
                _testDirectoryId,
                AuthService.CurrentUser.Id);
            
            // Assert
            Assert.That(uploadedFile, Is.Not.Null, "Uploaded file should not be null");
            Assert.That(uploadedFile.FileName, Is.EqualTo(fileName), "File name should match");
            Assert.That(uploadedFile.DirectoryId, Is.EqualTo(_testDirectoryId), "Directory ID should match");
            Assert.That(uploadedFile.FileSize, Is.GreaterThan(0), "File size should be greater than 0");
            
            // Add to tracking for cleanup
            UploadedFileIds.Add(uploadedFile.Id);
        }
        
        [Test]
        public async Task DownloadFile_ShouldSucceed()
        {
            // Arrange - Upload a file first
            string fileName = $"{GenerateUniqueName("DownloadTest")}.txt";
            string sourceFilePath = CreateTestFile(fileName, 50); // 50KB file
            long sourceFileSize = new FileInfo(sourceFilePath).Length;
            
            var uploadedFile = await FileService.UploadFileAsync(
                sourceFilePath, 
                _testDirectoryId,
                AuthService.CurrentUser.Id);
            
            Assert.That(uploadedFile, Is.Not.Null, "Failed to upload file for download test");
            UploadedFileIds.Add(uploadedFile.Id);
            
            // Create destination path
            string tempDir = Path.Combine(Path.GetTempPath(), "FileLink_Downloads");
            Directory.CreateDirectory(tempDir);
            string destinationPath = Path.Combine(tempDir, fileName);
            
            // Delete destination file if it exists
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            
            // Act
            var (success, filePath) = await FileService.DownloadFileAsync(
                uploadedFile.Id,
                destinationPath,
                AuthService.CurrentUser.Id);
            
            // Assert
            Assert.That(success, Is.True, "Download should succeed");
            Assert.That(File.Exists(destinationPath), Is.True, "Downloaded file should exist");
            
            // Verify file size matches
            FileInfo downloadedFileInfo = new FileInfo(destinationPath);
            Assert.That(downloadedFileInfo.Length, Is.EqualTo(sourceFileSize), 
                "Downloaded file size should match source file size");
            
            // Clean up downloaded file
            try
            {
                File.Delete(destinationPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        [Test]
        public async Task DeleteFile_ShouldSucceed()
        {
            // Arrange - Upload a file first
            string fileName = $"{GenerateUniqueName("DeleteTest")}.txt";
            string filePath = CreateTestFile(fileName, 10); // 10KB file
            
            var uploadedFile = await FileService.UploadFileAsync(
                filePath, 
                _testDirectoryId,
                AuthService.CurrentUser.Id);
            
            Assert.That(uploadedFile, Is.Not.Null, "Failed to upload file for delete test");
            
            // Act
            bool success = await FileService.DeleteFileAsync(uploadedFile.Id, AuthService.CurrentUser.Id);
            
            // Assert
            Assert.That(success, Is.True, "File deletion should succeed");
            
            // Verify file no longer exists by listing directory contents
            var (files, _) = await DirectoryService.GetDirectoryContentsAsync(
                _testDirectoryId,
                AuthService.CurrentUser.Id);
            
            // Check if the file was deleted
            bool fileExists = files.Exists(f => f.Id == uploadedFile.Id);
            Assert.That(fileExists, Is.False, "File should no longer exist after deletion");
        }
    }
}