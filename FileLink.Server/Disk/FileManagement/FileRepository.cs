using FileLink.Server.Core.Exceptions;
using FileLink.Server.Services.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileLink.Server.Disk.DirectoryManagement;
using System.Linq;

namespace FileLink.Server.Disk.FileManagement
{
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
        private bool _isInitialized = false;

        // Initializes a new instance of the FileRepository class
        // We need the directory for the files and the file metadata, plus our logging service
        public FileRepository(string metadataPath, string storagePath, IDirectoryRepository directoryRepository,
            LogService logService)
        {
            _metadataPath = metadataPath ?? throw new ArgumentNullException(nameof(metadataPath));
            _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
            _directoryRepository = directoryRepository ?? throw new ArgumentNullException(nameof(directoryRepository));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            // Ensure that directories exist
            Directory.CreateDirectory(_storagePath);
            Directory.CreateDirectory(_storagePath);

            // Load metadata from storage
            InitializeRepository().Wait();
        }

        // Initialize the repository if necessary
        // Initialize the repository, migrating data if necessary
        private async Task InitializeRepository()
        {
            if (_isInitialized)
                return;

            try
            {
                string legacyFilePath = Path.Combine(_metadataPath, "files.json");

                // Check if we need to migrate from the legacy format
                if (File.Exists(legacyFilePath))
                {
                    _logService.Info("Legacy metadata file found. Migrating to per-user storage format...");
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
                _logService.Error($"Error initializing file repository: {ex.Message}", ex);
                // If we can't initialize, start with an empty dictionary
                lock (_lock)
                {
                    _metadata = new Dictionary<string, FileMetadata>();
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
                    _logService.Warning("Legacy metadata file is empty.");
                    return;
                }

                // Deserialize from JSON
                var metadataList = JsonSerializer.Deserialize<List<FileMetadata>>(json);

                if (metadataList == null || metadataList.Count == 0)
                {
                    _logService.Warning("No file metadata found in legacy file.");
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

                    string userFilePath = Path.Combine(userMetadataDirectory, "files.json");

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

                    _logService.Info($"Migrated {group.Count()} file metadata records for user {userId}");
                }

                // Create backup of legacy file
                string backupPath = legacyFilePath + $".backup_{DateTime.Now:yyyyMMddHHmmss}";
                File.Copy(legacyFilePath, backupPath, true);

                // Optionally, delete the legacy file
                File.Delete(legacyFilePath);

                _logService.Info($"File metadata migration completed. Created backup at {backupPath}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error migrating legacy metadata: {ex.Message}", ex);
                throw;
            }
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
            ArgumentException.ThrowIfNullOrEmpty(fileMetadata.UserId);

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
            await SaveUserMetadata(fileMetadata.UserId);

            _logService.Info($"File {fileMetadata.FileName} added (ID: {fileMetadata.Id})");
            return true;
        }

        public async Task<bool> UpdateFileMetadataAsync(FileMetadata fileMetadata)
        {
            ArgumentNullException.ThrowIfNull(fileMetadata);
            ArgumentException.ThrowIfNullOrEmpty(fileMetadata.Id);
            ArgumentException.ThrowIfNullOrEmpty(fileMetadata.UserId);

            lock (_lock)
            {
                _metadata[fileMetadata.Id] = fileMetadata;
            }

            // Save changes to storage and log
            await SaveUserMetadata(fileMetadata.UserId);
            _logService.Info($"File {fileMetadata.FileName} updated (ID: {fileMetadata.Id})");
            return true;
        }

        // Deletes file metadata
        public async Task<bool> DeleteFileMetadataAsync(string fileId)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileId);

            string userId = null;
            lock (_lock)
            {
                if (!_metadata.TryGetValue(fileId, out FileMetadata metadata))
                {
                    _logService.Warning($"Attempted to delete non-existent file metadata: {fileId}");
                    return false;
                }

                userId = metadata.UserId;
                _metadata.Remove(fileId);
            }

            if (userId != null)
            {
                // Save changes to storage
                await SaveUserMetadata(userId);
                _logService.Info($"File {fileId} deleted (ID: {fileId})");
                return true;
            }

            return false;
        }

        // Loads metadata for all users
        private async Task LoadAllUserMetadata()
        {
            try
            {
                // Get all user directories in the metadata path
                var userDirectories = Directory.GetDirectories(_metadataPath);

                foreach (var userDir in userDirectories)
                {
                    string userId = Path.GetFileName(userDir);
                    await LoadUserMetadata(userId);
                }

                _logService.Info($"Loaded file metadata for {userDirectories.Length} users");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error loading all user metadata: {ex.Message}", ex);
            }
        }

        // Loads metadata to storage
        private async Task LoadUserMetadata(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logService.Warning("Attempted to load metadata for empty user ID");
                return;
            }

            try
            {
                string userFilePath = GetUserMetadataFilePath(userId);

                if (!File.Exists(userFilePath))
                {
                    // User may not have any files yet, this is normal
                    _logService.Debug($"No file metadata found for user {userId}");
                    return;
                }

                // Read the file
                string json = await File.ReadAllTextAsync(userFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logService.Warning($"File metadata file is empty for user {userId}");
                    return;
                }

                // Deserialize from JSON
                var metadataList = JsonSerializer.Deserialize<List<FileMetadata>>(json);

                if (metadataList == null || metadataList.Count == 0)
                {
                    _logService.Debug($"No file metadata entries found for user {userId}");
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

                _logService.Debug($"Loaded {metadataList.Count} file metadata entries for user {userId}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error loading metadata for user {userId}: {ex.Message}", ex);
            }
        }

        // MODIFIED: Saves all metadata to storage per user
        private async Task SaveUserMetadata(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logService.Warning("Attempted to save metadata with empty user ID");
                return;
            }

            try
            {
                // Get the user-specific metadata directory and file path
                string userMetadataDirectory = GetUserMetadataDirectory(userId);
                Directory.CreateDirectory(userMetadataDirectory);

                string userFilePath = Path.Combine(userMetadataDirectory, "files.json");

                // Filter metadata for this user
                List<FileMetadata> userMetadata;
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

                _logService.Debug($"Saved {userMetadata.Count} file metadata entries for user {userId}");
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
        public async Task<IEnumerable<FileMetadata>> GetFilesByDirectoryId(string directoryId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Array.Empty<FileMetadata>();

            // Ensure user metadata is loaded
            await LoadUserMetadata(userId);

            lock (_lock)
            {
                var files = _metadata.Values
                    .Where(m => m.UserId == userId &&
                                (directoryId == null
                                    ? string.IsNullOrEmpty(m.DirectoryId)
                                    : m.DirectoryId == directoryId))
                    .OrderByDescending(m => m.UpdatedAt)
                    .ToList();

                return files;
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

            // Ensure user metadata is loaded
            await LoadUserMetadata(userId);

            bool needSave = false;
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
                lock (_lock)
                {
                    _metadata[fileId] = file;
                    needSave = true;
                }
            }

            // Save changes if needed
            if (needSave)
            {
                await SaveUserMetadata(userId);
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
        
        // Helper method to get user-specific metadata directory
        private string GetUserMetadataDirectory(string userId)
        {
            return Path.Combine(_metadataPath, userId);
        }
    
        // Helper method to get user-specific file metadata path
        private string GetUserMetadataFilePath(string userId)
        {
            string userDirectory = GetUserMetadataDirectory(userId);
            return Path.Combine(userDirectory, "files.json");
        }
    }
}