using FileLink.Server.FileManagement;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Disk.DirectoryManagement
{
    // Service that provides directory management functionality
    public class DirectoryService
    {
        // Private fields
        private readonly IDirectoryRepository _directoryRepository;
        private readonly IFileRepository _fileRepository;
        private readonly PhysicalStorageService _storageService;
        private readonly LogService _logService;
        
        // Initializes a new instance of the DirectoryService class
        public DirectoryService(IDirectoryRepository directoryRepository, IFileRepository fileRepository, PhysicalStorageService storageService, LogService logService)
        {
            _directoryRepository = directoryRepository ?? throw new ArgumentNullException(nameof(directoryRepository));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task<DirectoryMetadata> CreateDirectory(string userId, string directoryName, string parentDirectoryId = null)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DirectoryMetadata>> GetAllDirectories(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<DirectoryMetadata>> GetDirectoriesInDirectory(string userId, string parentDirectoryId = null)
        {
            throw new NotImplementedException();
        }

        public async Task<(IEnumerable<FileMetadata> Files, IEnumerable<DirectoryMetadata> Directories)> GetDirectoryContents(string userId, string directoryId = null)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RenameDirectory(string directoryId, string newName, string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteDirectory(string directoryId, string userId, bool recursive = false)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> MoveFilesToDirectory(IEnumerable<string> fileIds, string targetDirectoryId, string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> MoveFileToDirectory(string fileId, string targetDirectoryId, string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<DirectoryMetadata> GetDirectoryById(string directoryId, string userId)
        {
            throw new NotImplementedException();
        }

        private string SanitizeDirectoryName(string directoryName)
        {
            return directoryName;
        }
    }
}
