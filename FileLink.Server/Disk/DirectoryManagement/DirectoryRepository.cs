using System.Text.Json;
using FileLink.Server.Core.Exceptions;
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

    public async Task<bool> AddDirectoryMetadata(DirectoryMetadata directoryMetadata)
    {
        if (directoryMetadata == null)
            throw new ArgumentNullException(nameof(directoryMetadata));

        if (string.IsNullOrEmpty(directoryMetadata.Id))
            throw new ArgumentException("Directory ID cannot be empty", nameof(directoryMetadata.Id));

        // Validate parent directory if specified
        if (!string.IsNullOrEmpty(directoryMetadata.ParentDirectoryId))
        {
            var parentDir = await GetDirectoryMetadataById(directoryMetadata.ParentDirectoryId);
            if (parentDir == null)
            {
                _logService.Warning($"Attempted to add directory with non-existent parent: {directoryMetadata.ParentDirectoryId}");
                return false;
            }

            // Ensure the parent directory belongs to the same user
            if (parentDir.UserId != directoryMetadata.UserId)
            {
                _logService.Warning($"Attempted to add directory to a parent owned by a different user");
                return false;
            }
        }

        // Check if directory with the same name already exists in the parent
        bool exists = await DirectoryExistsWithName(directoryMetadata.Name, directoryMetadata.ParentDirectoryId, directoryMetadata.UserId);
        if (exists)
        {
            _logService.Warning($"Directory with name '{directoryMetadata.Name}' already exists in the parent directory");
            return false;
        }

        lock (_lock)
        {
            // Check if the directory ID already exists
            if (_metadata.ContainsKey(directoryMetadata.Id))
            {
                _logService.Warning(
                    $"Attempted to add directory metadata with existing ID: {directoryMetadata.Id}");
                return false;
            }

            _metadata[directoryMetadata.Id] = directoryMetadata;

        }
        // Save changes to storage
        await SaveMetadata();
            
        _logService.Debug($"Directory metadata added: {directoryMetadata.Name} (ID: {directoryMetadata.Id})");
        return true;
    }

    // Updates existing directory metadata
    public async Task<bool> UpdateDirectoryMetadata(DirectoryMetadata directoryMetadata)
    {
        if (directoryMetadata == null)
            throw new ArgumentNullException(nameof(directoryMetadata));
            
        if (string.IsNullOrEmpty(directoryMetadata.Id))
            throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryMetadata));

        lock (_lock)
        {
            if (!_metadata.ContainsKey(directoryMetadata.Id))
            {
                _logService.Warning($"Attempted to update non-existent directory metadata: {directoryMetadata.Id}");
                return false;
            }

            _metadata[directoryMetadata.Id] = directoryMetadata;
        }

        // Save changes to storage
        await SaveMetadata();
            
        _logService.Debug($"Directory metadata updated: {directoryMetadata.Name} (ID: {directoryMetadata.Id})");
        return true;
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
            bool exists = _metadata.Values.Any(
                d => d.UserId == userId 
                     && d.Name.Equals(name, StringComparison.OrdinalIgnoreCase) 
                     && ((parentDirectoryId == null && string.IsNullOrEmpty(d.ParentDirectoryId)) 
                         || (d.ParentDirectoryId == parentDirectoryId)));
            return Task.FromResult(exists);
        }
        
    }

    // Gets all subdirectories for a given directory recursively
    public Task<IEnumerable<DirectoryMetadata>> GetAllSubdirectoriesRecursive(string directoryId)
    {
        if (string.IsNullOrEmpty(directoryId))
            return Task.FromResult<IEnumerable<DirectoryMetadata>>(Array.Empty<DirectoryMetadata>());

        lock (_lock)
        {
            var result = new List<DirectoryMetadata>();
            var directQueue = new Queue<string>();
                
            // Start with immediate children
            var immediateChildren = _metadata.Values.Where(d => d.ParentDirectoryId == directoryId).ToList();
                
            foreach (var child in immediateChildren)
            {
                result.Add(child);
                directQueue.Enqueue(child.Id);
            }
                
            // Process all descendants in breadth-first order
            while (directQueue.Count > 0)
            {
                string currentId = directQueue.Dequeue();
                var children = _metadata.Values.Where(d => d.ParentDirectoryId == currentId);
                    
                foreach (var child in children)
                {
                    result.Add(child);
                    directQueue.Enqueue(child.Id);
                }
            }
                
            return Task.FromResult<IEnumerable<DirectoryMetadata>>(result);
        }
    }
    
    private async Task SaveMetadata()
    {
        try
        {
            string filePath = Path.Combine(_metadataPath, "directories.json");
                
            // Create a copy of the metadata dictionary to avoid holding the lock during file I/O
            Dictionary<string, DirectoryMetadata> metadataCopy;
            lock (_lock)
            {
                metadataCopy = new Dictionary<string, DirectoryMetadata>(_metadata);
            }
                
            // Convert to a list for serialization
            var metadataList = metadataCopy.Values.ToList();
                
            // Serialize to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
                
            string json = JsonSerializer.Serialize(metadataList, options);
                
            // Write to file
            await File.WriteAllTextAsync(filePath, json);
                
            _logService.Debug($"Directory metadata saved to {filePath}");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error saving directory metadata: {ex.Message}", ex);
            throw new FileOperationException("Failed to save directory metadata.", ex);
        }
    }

    private async Task LoadMetadata()
    { 
        try 
        { 
            string filePath = Path.Combine(_metadataPath, "directories.json");
            
            if (!File.Exists(filePath)) 
            { 
                _logService.Info($"Directory metadata file not found at {filePath}. Creating a new one."); 
                // Create an empty dictionary
                lock (_lock) 
                { 
                    _metadata = new Dictionary<string, DirectoryMetadata>();
                } 
                return;
            }
                
            // Read the file
            string json = await File.ReadAllTextAsync(filePath);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                _logService.Warning($"Directory metadata file is empty at {filePath}");
                lock (_lock)
                {
                    _metadata = new Dictionary<string, DirectoryMetadata>();
                }
                return;
            }
            
            try
            {
                // Deserialize from JSON
                var metadataList = JsonSerializer.Deserialize<List<DirectoryMetadata>>(json);
                
                // Build the dictionary
                Dictionary<string, DirectoryMetadata> metadataDict = new Dictionary<string, DirectoryMetadata>();
                foreach (var metadata in metadataList)
                {
                    if (!string.IsNullOrEmpty(metadata.Id))
                    {
                        metadataDict[metadata.Id] = metadata;
                    }
                }
                
                // Update the metadata dictionary
                lock (_lock)
                {
                    _metadata = metadataDict;
                }
                
                _logService.Info($"Loaded {_metadata.Count} directory metadata entries from {filePath}");
            }
            catch (JsonException jsonEx)
            {
                _logService.Warning($"Invalid JSON format in directory metadata file. Creating backup and starting fresh: {jsonEx.Message}");
                
                // Back up the corrupted file
                string backupPath = filePath + $".backup_{DateTime.Now:yyyyMMddHHmmss}";
                File.Copy(filePath, backupPath, true);
                _logService.Info($"Created backup of directory metadata file at {backupPath}");
                
                // Start with empty dictionary
                lock (_lock)
                {
                    _metadata = new Dictionary<string, DirectoryMetadata>();
                }
                
                // Create a new, valid JSON file
                await SaveMetadata();
            }
            
        }
        catch (Exception ex)
        {
            _logService.Error($"Error loading directory metadata: {ex.Message}", ex);
            
            // If we can't load the metadata, just start with an empty dictionary
            lock (_lock)
            {
                _metadata = new Dictionary<string, DirectoryMetadata>();
            }
        }
    }
}