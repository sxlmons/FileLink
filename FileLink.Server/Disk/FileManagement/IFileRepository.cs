namespace FileLink.Server.FileManagement;

// Interface for the file repository
// Implements the repository pattern for file metadata storage
public interface IFileRepository
{
    // Gets the metadata by file ID
    Task<FileMetadata?> GetFileMetadataById(string fileId);
    
    // Gets all file metadata for a specific user 
    Task<IEnumerable<FileMetadata>> GetFileMetadataByUserId(string userId);
    
    // Adds new file metadata
    Task<bool> AddFileMetadataAsync(FileMetadata fileMetadata);
    
    // Updates existing file metadata
    Task<bool> UpdateFileMetadataAsync(FileMetadata fileMetadata);
    
    // Deletes file metadata
    Task<bool> DeleteFileMetadataAsync(string fileId);
}