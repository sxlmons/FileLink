// TestUtilities/MockFactory.cs
using FileLink.Server.Authentication;
using FileLink.Server.Disk;
using FileLink.Server.Disk.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Services.Logging;
using Moq;

namespace FileLink.TestSuite.ServerTests.TestUtilities
{
    public static class MockFactory
    {
        public static Mock<ILogger> CreateMockLogger()
        {
            var mockLogger = new Mock<ILogger>();
            return mockLogger;
        }

        public static LogService CreateLogService()
        {
            var mockLogger = CreateMockLogger();
            return new LogService(mockLogger.Object);
        }

        public static Mock<IUserRepository> CreateMockUserRepository()
        {
            var mockRepo = new Mock<IUserRepository>();
            // Setup common methods
            mockRepo.Setup(repo => repo.GetUserById(It.IsAny<string>()))
                .Returns((string id) => Task.FromResult(id == "existing-user-id" ? TestData.CreateTestUser() : null)!);
            
            mockRepo.Setup(repo => repo.GetUserByUsername(It.IsAny<string>()))
                .Returns((string username) => Task.FromResult(username == "testuser" ? TestData.CreateTestUser() : null)!);
            
            return mockRepo;
        }

        public static Mock<IFileRepository> CreateMockFileRepository()
        {
            var mockRepo = new Mock<IFileRepository>();
            // Setup common methods
            mockRepo.Setup(repo => repo.GetFileMetadataById(It.IsAny<string>()))
                .Returns((string id) => Task.FromResult(id == "existing-file-id" ? TestData.CreateTestFileMetadata() : null));
            
            mockRepo.Setup(repo => repo.GetFileMetadataByUserId(It.IsAny<string>()))
                .Returns((string userId) => 
                {
                    if (userId == "test-user-id")
                        return Task.FromResult<IEnumerable<FileMetadata>>(TestData.CreateTestFileList());
                    return Task.FromResult<IEnumerable<FileMetadata>>(new List<FileMetadata>());
                });
            
            return mockRepo;
        }

        public static Mock<PhysicalStorageService> CreateMockStorageService()
        {
            var mockStorage = new Mock<PhysicalStorageService>("test-path", CreateLogService());
            // Setup common methods
            mockStorage.Setup(s => s.CreateEmptyFile(It.IsAny<string>())).Returns(true);
            mockStorage.Setup(s => s.DeleteFile(It.IsAny<string>())).Returns(true);
            
            return mockStorage;
        }

        public static Mock<ClientSession> CreateMockClientSession(string? userId = null)
        {
            var mockSession = new Mock<ClientSession>(
                new Mock<System.Net.Sockets.TcpClient>().Object,
                CreateLogService(),
                null!, 
                null!,
                new Server.Server.ServerConfiguration(),
                CancellationToken.None);
            
            // Setup session properties
            mockSession.Setup(s => s.SessionId).Returns(Guid.NewGuid());
            mockSession.Setup(s => s.UserId).Returns(userId ?? "test-user-id");
            
            return mockSession;
        }
    }
}