using System.Text.Json;
using FileLink.Server.Core.Exceptions;
using FileLink.Server.Services.Logging;


namespace FileLink.Server.Disk.DirectoryManagement;

public class DirectoryRepository : IDirectoryRepository
{
    private readonly string _metadataPath;
    private readonly object _lock = new object();
    private Dictionary<string, DirectoryMetadata> _metadata = new Dictionary<string, DirectoryMetadata>();
    private readonly LogService _logService;
    private bool _isInitialized = false;
    
    // Initializes a new instance of the DirectoryRepository class
    public DirectoryRepository(string metadataPath, LogService logService)
    {
        _metadataPath = metadataPath ?? throw new ArgumentNullException(nameof(metadataPath));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
        // Ensure directories exist
        Directory.CreateDirectory(_metadataPath);
            
        // Load metadata from storage
        InitializeRepository().Wait();
    }
    
    // Initialize the repository, migrating data if necessary
    private async Task InitializeRepository()
    {
        if (_isInitialized)
            return;
        
        try
        {
            string legacyFilePath = Path.Combine(_metadataPath, "directories.json");
            
            // Check if we need to migrate from the legacy format
            if (File.Exists(legacyFilePath))
            {
                _logService.Info("Legacy directory metadata file found. Migrating to per-user storage format...");
                await MigrateFromLegacyFormat(legacyFilePath);
            }
            else
            {
                // No legacy data, just load user-specific metadata
                await LoadAllUserMetadata();
            }
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logService.Error($"Error initializing directory repository: {ex.Message}", ex);
            // If we can't initialize, start with an empty dictionary
            lock (_lock)
            {
                _metadata = new Dictionary<string, DirectoryMetadata>();
            }
        }
    }
    
    // Migrate from the legacy single-file format to per-user files
    private async Task MigrateFromLegacyFormat(string legacyFilePath)
    {
        try
        {
            // Read the legacy file
            string json = await File.ReadAllTextAsync(legacyFilePath);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                _logService.Warning("Legacy directory metadata file is empty.");
                return;
            }
            
            // Deserialize from JSON
            var metadataList = JsonSerializer.Deserialize<List<DirectoryMetadata>>(json);
            
            if (metadataList == null || metadataList.Count == 0)
            {
                _logService.Warning("No directory metadata found in legacy file.");
                return;
            }
            
            // Group metadata by user ID
            var userGroups = metadataList.GroupBy(m => m.UserId).ToList();
            
            // Create user-specific files
            foreach (var group in userGroups)
            {
                string userId = group.Key;
                if (string.IsNullOrEmpty(userId))
                    continue;
                
                // Get user-specific path
                string userMetadataDirectory = GetUserMetadataDirectory(userId);
                Directory.CreateDirectory(userMetadataDirectory);
                
                string userFilePath = Path.Combine(userMetadataDirectory, "directories.json");
                
                // Serialize and write user-specific metadata
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string userJson = JsonSerializer.Serialize(group.ToList(), options);
                await File.WriteAllTextAsync(userFilePath, userJson);
                
                // Add to in-memory metadata
                foreach (var metadata in group)
                {
                    if (!string.IsNullOrEmpty(metadata.Id))
                    {
                        _metadata[metadata.Id] = metadata;
                    }
                }
                
                _logService.Info($"Migrated {group.Count()} directory metadata records for user {userId}");
            }
            
            // Create backup of legacy file
            string backupPath = legacyFilePath + $".backup_{DateTime.Now:yyyyMMddHHmmss}";
            File.Copy(legacyFilePath, backupPath, true);
            
            // Optionally, delete the legacy file
            // File.Delete(legacyFilePath);
            
            _logService.Info($"Directory metadata migration completed. Created backup at {backupPath}");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error migrating legacy directory metadata: {ex.Message}", ex);
            throw;
        }
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
    public async Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Array.Empty<DirectoryMetadata>();
        
        // Ensure user metadata is loaded
        await LoadUserMetadata(userId);
        
        lock (_lock)
        {
            var userDirectories = _metadata.Values
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Name)
                .ToList();
                
            return userDirectories;
        }
    }

    // Gets directory metadata for directories with a specific parent
    public async Task<IEnumerable<DirectoryMetadata>> GetDirectoriesByParentId(string parentDirectoryId, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Array.Empty<DirectoryMetadata>();
        
        // Ensure user metadata is loaded
        await LoadUserMetadata(userId);
        
        lock (_lock)
        {
            var directories = _metadata.Values
                .Where(m => m.UserId == userId && (parentDirectoryId == null ? string.IsNullOrEmpty(m.ParentDirectoryId) : m.ParentDirectoryId == parentDirectoryId))
                .OrderBy(m => m.Name)
                .ToList();
                
            return directories;
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
        
        if (string.IsNullOrEmpty(directoryMetadata.UserId))
            throw new ArgumentException("User ID cannot be empty", nameof(directoryMetadata.UserId));

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
        await SaveUserMetadata(directoryMetadata.UserId);
            
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
        
        if (string.IsNullOrEmpty(directoryMetadata.UserId))
            throw new ArgumentException("User ID cannot be empty.", nameof(directoryMetadata.UserId));

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
        await SaveUserMetadata(directoryMetadata.UserId);
            
        _logService.Debug($"Directory metadata updated: {directoryMetadata.Name} (ID: {directoryMetadata.Id})");
        return true;
    }

    public async Task<bool> DeleteDirectoryMetadata(string directoryId)
    {
        if (string.IsNullOrEmpty(directoryId))
            throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));

        DirectoryMetadata metadata;
        string userId;
        
        lock (_lock)
        {
            if (!_metadata.TryGetValue(directoryId, out metadata))
            {
                _logService.Warning($"Attempted to delete non-existent directory metadata: {directoryId}");
                return false;
            }
            
            userId = metadata.UserId;

            // Check if there are subdirectories
            bool hasSubdirectories = _metadata.Values.Any(d => d.ParentDirectoryId == directoryId);
            if (hasSubdirectories)
            {
                _logService.Warning($"Cannot delete directory {directoryId} because it has subdirectories.");
                return false;
            }

            _metadata.Remove(directoryId);
        }

        // Save changes to storage
        if (!string.IsNullOrEmpty(userId))
        {
            await SaveUserMetadata(userId);
            
            _logService.Info($"Directory metadata deleted: {metadata.Name} (ID: {directoryId})");
            return true;
        }
        
        return false;
    }

    // Checks if a directory exists with the given name and parent
    public async Task<bool> DirectoryExistsWithName(string name, string parentDirectoryId, string userId)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(userId))
            return false;
        
        // Ensure user metadata is loaded
        await LoadUserMetadata(userId);
        
        lock (_lock)
        {
            bool exists = _metadata.Values.Any(d => d.UserId 
                == userId 
                && d.Name.Equals(name, StringComparison.OrdinalIgnoreCase) 
                && ((parentDirectoryId == null 
                && string.IsNullOrEmpty(d.ParentDirectoryId)) 
                || (d.ParentDirectoryId == parentDirectoryId)));
            return exists;
        }
    }

    // Gets all subdirectories for a given directory recursively
    public async Task<IEnumerable<DirectoryMetadata>> GetAllSubdirectoriesRecursive(string directoryId)
    {
        if (string.IsNullOrEmpty(directoryId))
            return Array.Empty<DirectoryMetadata>();
        
        // First get the directory to determine user ID
        DirectoryMetadata directory;
        lock (_lock)
        {
            _metadata.TryGetValue(directoryId, out directory);
        }
        
        if (directory == null)
            return Array.Empty<DirectoryMetadata>();
        
        // Ensure user metadata is loaded
        await LoadUserMetadata(directory.UserId);
        
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
                
            return result;
        }
    }
    
    // Helper method to get user-specific metadata directory
    private string GetUserMetadataDirectory(string userId)
    {
        return Path.Combine(_metadataPath, userId);
    }
    
    // Helper method to get user-specific directory metadata path
    private string GetUserMetadataFilePath(string userId)
    {
        string userDirectory = GetUserMetadataDirectory(userId);
        return Path.Combine(userDirectory, "directories.json");
    }
    
    // Loads metadata for all users
    private async Task LoadAllUserMetadata()
    {
        try
        {
            // Get all user directories in the metadata path
            if (!Directory.Exists(_metadataPath))
            {
                _logService.Info($"Metadata path does not exist: {_metadataPath}. Creating it.");
                Directory.CreateDirectory(_metadataPath);
                return;
            }
            
            var userDirectories = Directory.GetDirectories(_metadataPath);
            
            foreach (var userDir in userDirectories)
            {
                string userId = Path.GetFileName(userDir);
                await LoadUserMetadata(userId);
            }
            
            _logService.Info($"Loaded directory metadata for {userDirectories.Length} users");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error loading all user metadata: {ex.Message}", ex);
        }
    }
    
    // Loads metadata for a specific user
    private async Task LoadUserMetadata(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logService.Warning("Attempted to load directory metadata for empty user ID");
            return;
        }
        
        try
        {
            string userFilePath = GetUserMetadataFilePath(userId);
            
            if (!File.Exists(userFilePath))
            {
                // User may not have any directories yet, this is normal
                _logService.Debug($"No directory metadata found for user {userId}");
                return;
            }
            
            // Read the file
            string json = await File.ReadAllTextAsync(userFilePath);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                _logService.Warning($"Directory metadata file is empty for user {userId}");
                return;
            }
            
            // Deserialize from JSON
            var metadataList = JsonSerializer.Deserialize<List<DirectoryMetadata>>(json);
            
            if (metadataList == null || metadataList.Count == 0)
            {
                _logService.Debug($"No directory metadata entries found for user {userId}");
                return;
            }
            
            // Update the metadata dictionary
            lock (_lock)
            {
                foreach (var metadata in metadataList)
                {
                    if (!string.IsNullOrEmpty(metadata.Id))
                    {
                        _metadata[metadata.Id] = metadata;
                    }
                }
            }
            
            _logService.Debug($"Loaded {metadataList.Count} directory metadata entries for user {userId}");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error loading directory metadata for user {userId}: {ex.Message}", ex);
        }
    }
    
    // Saves metadata for a specific user
    private async Task SaveUserMetadata(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logService.Warning("Attempted to save directory metadata with empty user ID");
            return;
        }
        
        try
        {
            // Get the user-specific metadata directory and file path
            string userMetadataDirectory = GetUserMetadataDirectory(userId);
            Directory.CreateDirectory(userMetadataDirectory);
            
            string userFilePath = Path.Combine(userMetadataDirectory, "directories.json");
            
            // Filter metadata for this user
            List<DirectoryMetadata> userMetadata;
            lock (_lock)
            {
                userMetadata = _metadata.Values
                    .Where(m => m.UserId == userId)
                    .ToList();
            }
            
            // Serialize to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            string json = JsonSerializer.Serialize(userMetadata, options);
            
            // Write to file
            await File.WriteAllTextAsync(userFilePath, json);
            
            _logService.Debug($"Saved {userMetadata.Count} directory metadata entries for user {userId}");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error saving directory metadata for user {userId}: {ex.Message}", ex);
            throw new FileOperationException($"Failed to save directory metadata for user {userId}", ex);
        }
    }
}