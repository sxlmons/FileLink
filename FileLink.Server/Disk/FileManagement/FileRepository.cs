using FileLink.Server.Core.Exceptions;
using FileLink.Server.Services.Logging;
using System.Text.Json;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.FileManagement;

namespace FileLink.Server.Disk.FileManagement;

// Repository for file metadata storage
// Implements the repository pattern with file-based storage
public class FileRepository : IFileRepository
{
    private readonly string _metadataPath;
    private readonly string _storagePath;
    private readonly object _lock = new object();
    private Dictionary<string, FileMetadata> _metadata = new Dictionary<string, FileMetadata>();
    private readonly LogService _logService;
    private readonly IDirectoryRepository _directoryRepository;
    
    // Initializes a new instance of the FileRepository class
    // We need the directory for the files and the file metadata, plus our logging service
    public FileRepository(string metadataPath, string storagePath, IDirectoryRepository directoryRepository, LogService logService)
    {
        _metadataPath = metadataPath ?? throw new ArgumentNullException(nameof(metadataPath));
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
        _directoryRepository = directoryRepository ?? throw new ArgumentNullException(nameof(directoryRepository));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        
        // Ensure that directories exist
        Directory.CreateDirectory(_storagePath);
        Directory.CreateDirectory(_storagePath);
        
        // Load metadata from storage
        LoadMetadata().Wait();
    }
    
    // Gets file metadata by file ID
    public Task<FileMetadata?> GetFileMetadataById(string fileId)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);

        // Use lock to prevent multiple threads accessing metadata
        lock (_lock)
        {
            _metadata.TryGetValue(fileId, out FileMetadata metadata);
            return Task.FromResult(metadata);   
        }
    }

    // Gets all file metadata for a specific user
    public Task<IEnumerable<FileMetadata>> GetFileMetadataByUserId(string userId)   
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        lock (_lock)
        {
            // Retrieve and filter out the meta data properties for selected user ID
            var userFiles = _metadata.Values
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.UpdatedAt)
                .ToList();
            
            return Task.FromResult<IEnumerable<FileMetadata>>(userFiles);
        }
    }

    // Adds new file metadata
    public async Task<bool> AddFileMetadataAsync(FileMetadata fileMetadata)
    {
        ArgumentNullException.ThrowIfNull(fileMetadata);
        ArgumentException.ThrowIfNullOrEmpty(fileMetadata.Id);
        lock (_lock)
        {
            // Check if file ID already exists
            if (!_metadata.TryAdd(fileMetadata.Id, fileMetadata))
            {
                _logService.Warning($"File {fileMetadata.Id} already exists");
                return false;
            }
        }
        // Save changes to storage and log
        await SaveMetadata();
        
        _logService.Info($"File {fileMetadata.FileName} added (ID: {fileMetadata.Id})");
        return true;
    }   

    public async Task<bool> UpdateFileMetadataAsync(FileMetadata fileMetadata)
    {
        ArgumentNullException.ThrowIfNull(fileMetadata);
        ArgumentException.ThrowIfNullOrEmpty(fileMetadata.Id);
        lock (_lock)
        {
            // Check if file ID already exists
            if (!_metadata.TryAdd(fileMetadata.Id, fileMetadata))
            {
                _logService.Warning($"Attempted to update non-existent file metadata: {fileMetadata.Id}");
                return false;
            } 
        }
        // Save changes to storage and log
        await SaveMetadata();
        _logService.Info($"File {fileMetadata.FileName} updated (ID: {fileMetadata.Id})");
        return true;
    }

    // Deletes file metadata
    public async Task<bool> DeleteFileMetadataAsync(string fileId)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileId);
        
        FileMetadata metadata;
        lock (_lock)
        {
            if (_metadata.TryGetValue(fileId, out metadata))
            {
                _logService.Warning($"Attempted to delete non-existent file metadata: {fileId}");
                return false;
            }
            _metadata.Remove(fileId);
        }
        // Save changes to storage
        await SaveMetadata();
        _logService.Info($"File {fileId} deleted (ID: {fileId})");
        return true;
    }
    
    // Loads all metadata to storage
    private async Task LoadMetadata()
    {
        try
        {
            string filePath = Path.Combine(_metadataPath, "files.json");

            if (!File.Exists(filePath))
            {
                _logService.Info($"File {filePath} does not exist. Creating a new one.");
                return;
            }
            
            // Read the file
            string json = await File.ReadAllTextAsync(filePath);
            
            // Deserialize from JSON
            var metadataList = JsonSerializer.Deserialize<List<FileMetadata>>(json);
            
            // Build the dictionary
            Dictionary<string, FileMetadata> metadataDict = new Dictionary<string, FileMetadata>();
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
            _logService.Info($"Loaded {_metadata.Count} file metadata entries from {filePath}");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error loading metadata: {ex.Message}", ex);
            
            // If we cant load the metadata, just start with an empty dictionary
            lock (_lock)
            {
                _metadata = new Dictionary<string, FileMetadata>();
            }
        }
    }
    
    // Saves all metadata to storage
    private async Task SaveMetadata()
    {
        try
        {
            string filePath = Path.Combine(_metadataPath, "files.json");
            
            // Create a copy of the metadata dictionary to avoid holding the lock during file I/O
            Dictionary<string, FileMetadata> metadataCopy;
            lock (_lock)
            {
                metadataCopy = new Dictionary<string, FileMetadata>(_metadata);
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
            _logService.Info($"File saved to {filePath}");
        }
        catch (Exception ex)
        {
            _logService.Error($"Error saving metadata: {ex.Message}", ex);
            throw new FileOperationException("Failed to save metadata", ex);
        }
    }
    
    //-----------------------------------
    // NEW: Directory Feature Extension
    //-----------------------------------
    
    // Gets all metadata for files in a specific directory
    public Task<IEnumerable<FileMetadata>> GetFilesByDirectoryId(string directoryId, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult<IEnumerable<FileMetadata>>(Array.Empty<FileMetadata>());

        lock (_lock)
        {
            var files = _metadata.Values
                .Where(m => m.UserId == userId && 
                            (directoryId == null ? 
                                string.IsNullOrEmpty(m.DirectoryId) : 
                                m.DirectoryId == directoryId))
                .OrderByDescending(m => m.UpdatedAt)
                .ToList();
                
            return Task.FromResult<IEnumerable<FileMetadata>>(files);
        }
    }
    
    // Moves files to a different directory
    public async Task<bool> MoveFilesToDirectory(IEnumerable<string> fileIds, string directoryId, string userId)
    {
        if (string.IsNullOrEmpty(userId) || fileIds == null)
            return false;

        var fileIdList = fileIds.ToList();
        if (fileIdList.Count == 0)
            return true;

        // Validate directory if specified
        if (!string.IsNullOrEmpty(directoryId))
        {
            // This would require dependency injection of the DirectoryRepository
            // For now, assume this check is done at the service layer
        }

        bool allSuccessful = true;
        foreach (var fileId in fileIdList)
        {
            var file = await GetFileMetadataById(fileId);
            if (file == null || file.UserId != userId)
            {
                _logService.Warning($"File not found or access denied when moving file {fileId} to directory {directoryId}");
                allSuccessful = false;
                continue;
            }

            // Update file metadata with new directory
            file.DirectoryId = directoryId;
            file.UpdatedAt = DateTime.Now;

            // Update storage metadata
            bool updated = await UpdateFileMetadataAsync(file);
            if (!updated)
            {
                _logService.Error($"Failed to update metadata for file {fileId} when moving to directory {directoryId}");
                allSuccessful = false;
            }
        }

        return allSuccessful;
    }
    
    // Gets a directory by its ID
    public async Task<DirectoryMetadata> GetDirectoryById(string directoryId)
    {
        try
        {
            // This implementation depends on having access to the DirectoryRepository
            // We'll use the private field _directoryRepository
            if (string.IsNullOrEmpty(directoryId))
                return null;
        
            return await _directoryRepository.GetDirectoryMetadataById(directoryId);
        }
        catch (Exception ex)
        {
            _logService.Error($"Error getting directory by ID {directoryId}: {ex.Message}", ex);
            return null;
        }
    }
}