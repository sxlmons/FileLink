using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Disk.DirectoryManagement;

public class DirectoryRepository : IDirectoryRepository
{
    private readonly string _metadataPath;
    private readonly object _lock = new object();
    private Dictionary<string, DirectoryMetadata> _metadata = new Dictionary<string, DirectoryMetadata>();
    private readonly LogService _logService;
    
    // Initializes a new instance of the DirectoryRepository class
    public DirectoryRepository(string metadataPath, LogService logService)
    {
        _metadataPath = metadataPath ?? throw new ArgumentNullException(nameof(metadataPath));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
        // Ensure directories exist
        Directory.CreateDirectory(_metadataPath);
            
        // Load metadata from storage
        LoadMetadata().Wait();
    }
    
    // Gets directory metadata by directory ID
    public Task<DirectoryMetadata> GetDirectoryMetadataById(string directoryId)
    {
        if (string.IsNullOrEmpty(directoryId))
            return Task.FromResult<DirectoryMetadata>(null);

        lock (_lock)
        {
            _metadata.TryGetValue(directoryId, out DirectoryMetadata metadata);
            return Task.FromResult(metadata);
        }
    }

    // Gets all directory metadata for a specific user
    public Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult<IEnumerable<DirectoryMetadata>>(Array.Empty<DirectoryMetadata>());

        lock (_lock)
        {
            var userDirectories = _metadata.Values
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Name)
                .ToList();
                
            return Task.FromResult<IEnumerable<DirectoryMetadata>>(userDirectories);
        }
    }

    // Gets directory metadata for directories with a specific parent
    public Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByParentId(string parentDirectoryId, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult<IEnumerable<DirectoryMetadata>>(Array.Empty<DirectoryMetadata>());

        lock (_lock)
        {
            var directories = _metadata.Values
                .Where(m => m.UserId == userId && (parentDirectoryId == null ? string.IsNullOrEmpty(m.ParentDirectoryId) : m.ParentDirectoryId == parentDirectoryId))
                .OrderBy(m => m.Name)
                .ToList();
                
            return Task.FromResult<IEnumerable<DirectoryMetadata>>(directories);
        }
    }

    public Task<IEnumerable<DirectoryMetadata>> GetRootDirectories(string userId)
    {
        return GetDirectoriesByParentId(null, userId);
    }

    public Task<bool> AddDirectoryMetadata(DirectoryMetadata directoryMetadata)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateDirectoryMetadata(DirectoryMetadata directoryMetadata)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteDirectoryMetadata(string directoryId)
    {
        throw new NotImplementedException();
    }

    // Checks if a directory exists with the given name and parent
    public Task<bool> DirectoryExistsWithName(string name, string parentDirectoryId, string userId)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(userId))
            return Task.FromResult(false);

        lock (_lock)
        {
            bool exists = _metadata.Values.Any(d => 
                d.UserId == userId && 
                d.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                ((parentDirectoryId == null && string.IsNullOrEmpty(d.ParentDirectoryId)) || 
                 (d.ParentDirectoryId == parentDirectoryId)));
                
            return Task.FromResult(exists);
        }
        
    }

    public Task<IEnumerable<DirectoryMetadata>> GetAllSubdirectoriesRecursive(string directoryId)
    {
        throw new NotImplementedException();
    }

    private async Task LoadMetadata()
    {
        string filePath = Path.Combine(_metadataPath, "directories.json");

        try
        {
            // deserialize our metadata json file
        }
        catch (Exception ex)
        {
            
        }
    }
}