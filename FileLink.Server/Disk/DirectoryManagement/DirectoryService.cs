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
    }
}
