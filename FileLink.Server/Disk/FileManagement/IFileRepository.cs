using FileLink.Server.Disk.DirectoryManagement;

namespace FileLink.Server.Disk.FileManagement
{

// Interface for the file repository
// Implements the repository pattern for file metadata storage
    public interface IFileRepository
    {
        // File Management
        Task<FileMetadata?> GetFileMetadataById(string fileId);

        Task<IEnumerable<FileMetadata>> GetFileMetadataByUserId(string userId);

        Task<bool> AddFileMetadataAsync(FileMetadata fileMetadata);

        Task<bool> UpdateFileMetadataAsync(FileMetadata fileMetadata);

        Task<bool> DeleteFileMetadataAsync(string fileId);

        // DIRECTORY FEATURE EXTENSION
        Task<IEnumerable<FileMetadata>> GetFilesByDirectoryId(string directoryId, string userId);
        Task<bool> MoveFilesToDirectory(IEnumerable<string> fileIds, string directoryId, string userId);
        Task<DirectoryMetadata> GetDirectoryById(string directoryId);


    }
}