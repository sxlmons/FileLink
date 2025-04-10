// TestUtilities/TestData.cs
using FileLink.Server.Authentication;
using FileLink.Server.Disk.FileManagement;

namespace FileLink.TestSuite.ServerTests.TestUtilities
{
    public static class TestData
    {
        public static User CreateTestUser()
        {
            return new User
            {
                Id = "test-user-id",
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashed_password",
                PasswordSalt = new byte[] { 1, 2, 3, 4 },
                Role = "User",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                LastLoginAt = DateTime.UtcNow.AddHours(-1)
            };
        }
        
        public static List<FileMetadata> CreateTestFileList()
        {
            return
            [
                new FileMetadata()
                {
                    Id = "file-id-1",
                    UserId = "test-user-id",
                    FileName = "test-file-1.txt",
                    FileSize = 1024,
                    ContentType = "text/plain",
                    FilePath = "/path/to/test-file-1.txt",
                    IsComplete = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },

                new FileMetadata()
                {
                    Id = "file-id-2",
                    UserId = "test-user-id",
                    FileName = "test-file-2.jpg",
                    FileSize = 2048,
                    ContentType = "image/jpeg",
                    FilePath = "/path/to/test-file-2.jpg",
                    IsComplete = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                }
            ];
        }
        
        public static FileMetadata CreateTestFileMetadata()
        {
            return new FileMetadata
            {
                Id = "existing-file-id",
                UserId = "test-user-id",
                FileName = "existing-file.txt",
                FileSize = 4096,
                ContentType = "text/plain",
                FilePath = "/path/to/existing-file.txt",
                IsComplete = true,
                ChunksReceived = 4,
                TotalChunks = 4,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };
        }
    }
}