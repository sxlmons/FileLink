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
    
    
    public Task<DirectoryMetadata> GetDirectoryMetadataById(string directoryId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByUserId(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByParentId(string parentDirectoryId, string userId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DirectoryMetadata>> GetRootDirectories(string userId)
    {
        throw new NotImplementedException();
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

    public Task<bool> DirectoryExistsWithName(string name, string parentDirectoryId, string userId)
    {
        throw new NotImplementedException();
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