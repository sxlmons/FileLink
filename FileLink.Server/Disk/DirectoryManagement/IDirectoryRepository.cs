using FileLink.Server.Disk.DirectoryManagement;

namespace FileLink.Server.Disk.DirectoryManagement

{
    public interface IDirectoryRepository
    {
        Task<DirectoryMetadata> GetDirectoryMetadataById(string directoryId);

        Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByUserId(string userId);

        Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByParentId(string parentDirectoryId, string userId);
        
        Task<IEnumerable<DirectoryMetadata>> GetRootDirectories(string userId);
        
        Task<bool> AddDirectoryMetadata(DirectoryMetadata directoryMetadata);
        
        Task<bool> UpdateDirectoryMetadata(DirectoryMetadata directoryMetadata);
        
        Task<bool> DeleteDirectoryMetadata(string directoryId);
        
        Task<bool> DirectoryExistsWithName(string name, string parentDirectoryId, string userId);

        Task<IEnumerable<DirectoryMetadata>> GetAllSubdirectoriesRecursive(string directoryId);
    }
}