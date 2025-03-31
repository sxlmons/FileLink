using FileLink.Server.Core.Exceptions;
using FileLink.Server.Disk.FileManagement;
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
        
        // Initializes a new instance of the DirectoryService class
        public DirectoryService(IDirectoryRepository directoryRepository, IFileRepository fileRepository, PhysicalStorageService storageService, LogService logService)
        {
            _directoryRepository = directoryRepository ?? throw new ArgumentNullException(nameof(directoryRepository));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task<DirectoryMetadata> CreateDirectory(string userId, string directoryName, string parentDirectoryId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("User ID cannot be empty.", nameof(userId));

                if (string.IsNullOrEmpty(directoryName))
                    throw new ArgumentException("Directory name cannot be empty.", nameof(directoryName));
                
                // Sanitize the directory name
                directoryName = SanitizeDirectoryName(directoryName);

                // Check if a directory with the same name already exists under the parent
                bool exists = await _directoryRepository.DirectoryExistsWithName(directoryName, parentDirectoryId, userId);
                if (exists)
                {
                    _logService.Warning($"Directory '{directoryName}' already exists under parent {parentDirectoryId ?? "root"}");
                    return null;
                }
                
                // Get parent directory path if specified
                string parentPath = null;
                if (!string.IsNullOrEmpty(parentDirectoryId))
                {
                    var parentDir = await _directoryRepository.GetDirectoryMetadataById(parentDirectoryId);
                    if (parentDir == null)
                    {
                        _logService.Warning($"Parent directory not found: {parentDirectoryId}");
                        return null;
                    }
                    
                    // Check if the parent directory belongs to the user
                    if (parentDir.UserId != userId)
                    {
                        _logService.Warning($"User {userId} attempted to create directory in parent owned by {parentDir.UserId}");
                        return null;
                    }
                    
                    parentPath = parentDir.DirectoryPath;
                }
                
                // Generate a unique directory ID
                string directoryId = Guid.NewGuid().ToString();
                
                // Get the physical path for the directory (and make it absolute)
                string physicalPath = _storageService.GetDirectoryPath(userId, directoryName, parentPath);
                physicalPath = Path.GetFullPath(physicalPath);
                
                // Create directory metadata
                var metadata = new DirectoryMetadata(userId, directoryName, parentDirectoryId, physicalPath);
                
                // Create the physical directory
                bool dirCreated = _storageService.CreateDirectory(physicalPath);
                if (!dirCreated)
                {
                    _logService.Warning($"Directory already exists at {physicalPath}");
                    // We'll continue anyway, as this is not a critical error
                }
                
                // Add the metadata to the repository
                bool success = await _directoryRepository.AddDirectoryMetadata(metadata);
                
                if (!success)
                {
                    // Cleanup if metadata addition failed
                    _storageService.DeleteDirectory(physicalPath);
                    throw new FileOperationException("Failed to add directory metadata.");
                }
                
                _logService.Info($"Directory created: {directoryName} (ID: {directoryId}, User: {userId}, Physical path: {physicalPath})");
                
                return metadata;
            }
            catch (Exception ex) when (!(ex is FileOperationException))
            {
                _logService.Error($"Error creating directory: {ex.Message}", ex);
                throw new FileOperationException("Failed to create directory.", ex);
            }
        }

        public async Task<IEnumerable<DirectoryMetadata>> GetAllDirectories(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                var directories = await _directoryRepository.GetDirectoriesByUserId(userId);
                
                _logService.Debug($"Retrieved {directories.Count()} directories for user {userId}");
                
                return directories;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting user directories: {ex.Message}", ex);
                throw new FileOperationException("Failed to get user directories.", ex);
            }
        }

        public async Task<IEnumerable<DirectoryMetadata>> GetDirectoriesInDirectory(string userId, string parentDirectoryId = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Validate parent directory if specified
                if (!string.IsNullOrEmpty(parentDirectoryId))
                {
                    var parentDir = await _directoryRepository.GetDirectoryMetadataById(parentDirectoryId);
                    if (parentDir == null)
                    {
                        _logService.Warning($"Parent directory not found: {parentDirectoryId}");
                        return Enumerable.Empty<DirectoryMetadata>();
                    }
                    
                    // Check if the parent directory belongs to the user
                    if (parentDir.UserId != userId)
                    {
                        _logService.Warning($"User {userId} attempted to list directories in directory owned by {parentDir.UserId}");
                        return Enumerable.Empty<DirectoryMetadata>();
                    }
                }
                
                var directories = await _directoryRepository.GetDirectoriesByParentId(parentDirectoryId, userId);
                
                _logService.Debug($"Retrieved {directories.Count()} directories for user {userId} in parent {parentDirectoryId ?? "root"}");
                
                return directories;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting directories in parent: {ex.Message}", ex);
                throw new FileOperationException("Failed to get directories.", ex);
            }
        }

        public async Task<(IEnumerable<FileMetadata> Files, IEnumerable<DirectoryMetadata> Directories)> GetDirectoryContents(string userId, string directoryId = null)
        {
             if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
             try
             {
                 // Validate directory if specified
                 if (!string.IsNullOrEmpty(directoryId))
                 {
                     var dir = await _directoryRepository.GetDirectoryMetadataById(directoryId);
                     if (dir == null)
                     {
                         _logService.Warning($"Directory not found: {directoryId}");
                         return (Enumerable.Empty<FileMetadata>(), Enumerable.Empty<DirectoryMetadata>());
                     }
                    
                     // Check if the directory belongs to the user
                     if (dir.UserId != userId)
                     {
                         _logService.Warning($"User {userId} attempted to list contents of directory owned by {dir.UserId}");
                         return (Enumerable.Empty<FileMetadata>(), Enumerable.Empty<DirectoryMetadata>());
                     }
                 }
                
                 // Get files and directories in parallel
                 var filesTask = _fileRepository.GetFilesByDirectoryId(directoryId, userId);
                 var directoriesTask = _directoryRepository.GetDirectoriesByParentId(directoryId, userId);
                
                 await Task.WhenAll(filesTask, directoriesTask);
                
                 var files = await filesTask;
                 var directories = await directoriesTask;
                
                 _logService.Debug($"Retrieved {files.Count()} files and {directories.Count()} directories for user {userId} in directory {directoryId ?? "root"}");
                
                 return (files, directories);
             }
             catch (Exception ex)
             {
                 _logService.Error($"Error getting directory contents: {ex.Message}", ex);
                 throw new FileOperationException("Failed to get directory contents.", ex);
             }
        }

        public async Task<bool> RenameDirectory(string directoryId, string newName, string userId)
        {
            if (string.IsNullOrEmpty(directoryId))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));
            
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentException("New name cannot be empty.", nameof(newName));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Get the directory metadata
                var metadata = await _directoryRepository.GetDirectoryMetadataById(directoryId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Directory not found: {directoryId}");
                    return false;
                }
                
                // Check if the user owns the directory
                if (metadata.UserId != userId)
                {
                    _logService.Warning($"User {userId} attempted to rename directory owned by {metadata.UserId}");
                    return false;
                }
                
                // Sanitize the new name
                newName = SanitizeDirectoryName(newName);
                
                // Check if a directory with the new name already exists under the same parent
                bool exists = await _directoryRepository.DirectoryExistsWithName(newName, metadata.ParentDirectoryId, userId);
                if (exists)
                {
                    _logService.Warning($"Directory with name '{newName}' already exists under the same parent");
                    return false;
                }
                
                // Update the metadata
                string oldName = metadata.Name;
                metadata.Rename(newName);
                
                // Update the physical directory path
                string oldPath = metadata.DirectoryPath;
                string parentPath = Path.GetDirectoryName(oldPath);
                string newPath = Path.Combine(parentPath, newName);
                
                // Handle physical path update if necessary
                if (Path.GetFileName(oldPath) != newName)
                {
                    // Check if the target path already exists
                    if (Directory.Exists(newPath))
                    {
                        _logService.Warning($"Target path already exists: {newPath}");
                        return false;
                    }
                    
                    // Create a new directory at the target path
                    _storageService.CreateDirectory(newPath);
                    
                    // Move all contents from old directory to new directory
                    string[] files = Directory.GetFiles(oldPath);
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(newPath, fileName);
                        _storageService.MoveFile(file, destFile);
                    }
                    
                    // Move subdirectories recursively
                    string[] dirs = Directory.GetDirectories(oldPath);
                    foreach (string dir in dirs)
                    {
                        string dirName = Path.GetFileName(dir);
                        string destDir = Path.Combine(newPath, dirName);
                        Directory.CreateDirectory(destDir);
                        
                        // Move contents of subdirectory
                        string[] subFiles = Directory.GetFiles(dir);
                        foreach (string file in subFiles)
                        {
                            string fileName = Path.GetFileName(file);
                            string destFile = Path.Combine(destDir, fileName);
                            _storageService.MoveFile(file, destFile);
                        }
                        
                        // Handle deeper nesting through recursion if needed
                        string[] subDirs = Directory.GetDirectories(dir);
                        if (subDirs.Length > 0)
                        {
                            _logService.Warning($"Complex nested directory structure detected during rename. Some subdirectories may need manual adjustment.");
                        }
                    }
                    
                    // Delete the old directory after moving everything
                    _storageService.DeleteDirectory(oldPath);
                    
                    // Update the path in metadata
                    metadata.DirectoryPath = newPath;
                }
                
                // Update the metadata in the repository
                bool success = await _directoryRepository.UpdateDirectoryMetadata(metadata);
                
                if (success)
                {
                    _logService.Info($"Directory renamed: {oldName} to {newName} (ID: {directoryId})");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error renaming directory: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteDirectory(string directoryId, string userId, bool recursive = false)
        {
            if (string.IsNullOrEmpty(directoryId))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Get the directory metadata
                var metadata = await _directoryRepository.GetDirectoryMetadataById(directoryId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Directory not found: {directoryId}");
                    return false;
                }
                
                // Check if the user owns the directory
                if (metadata.UserId != userId)
                {
                    _logService.Warning($"User {userId} attempted to delete directory owned by {metadata.UserId}");
                    return false;
                }
                
                // Check if the directory contains files
                var files = await _fileRepository.GetFilesByDirectoryId(directoryId, userId);
                if (files.Any() && !recursive)
                {
                    _logService.Warning($"Directory {directoryId} is not empty and recursive deletion is not specified");
                    return false;
                }
                
                // Check if the directory has subdirectories
                var subdirectories = await _directoryRepository.GetDirectoriesByParentId(directoryId, userId);
                if (subdirectories.Any() && !recursive)
                {
                    _logService.Warning($"Directory {directoryId} has subdirectories and recursive deletion is not specified");
                    return false;
                }
                
                // If recursive, delete all files and subdirectories
                if (recursive)
                {
                    // Get all subdirectories recursively
                    var allSubdirectories = await _directoryRepository.GetAllSubdirectoriesRecursive(directoryId);
                    
                    // Delete files in all subdirectories starting from the deepest level
                    foreach (var subdir in allSubdirectories.OrderByDescending(d => d.DirectoryPath.Count(c => c == Path.DirectorySeparatorChar)))
                    {
                        var subFiles = await _fileRepository.GetFilesByDirectoryId(subdir.Id, userId);
                        foreach (var file in subFiles)
                        {
                            _storageService.DeleteFile(file.FilePath);
                            await _fileRepository.DeleteFileMetadataAsync(file.Id);
                        }
                    }
                    
                    // Delete files in the directory itself
                    foreach (var file in files)
                    {
                        _storageService.DeleteFile(file.FilePath);
                        await _fileRepository.DeleteFileMetadataAsync(file.Id);
                    }
                    
                    // Delete subdirectory metadata starting from the deepest level
                    foreach (var subdir in allSubdirectories.OrderByDescending(d => d.DirectoryPath.Count(c => c == Path.DirectorySeparatorChar)))
                    {
                        await _directoryRepository.DeleteDirectoryMetadata(subdir.Id);
                        _storageService.DeleteDirectory(subdir.DirectoryPath);
                    }
                }
                
                // Delete the directory metadata
                bool success = await _directoryRepository.DeleteDirectoryMetadata(directoryId);
                
                if (success)
                {
                    // Delete the physical directory
                    _storageService.DeleteDirectory(metadata.DirectoryPath, recursive);
                    _logService.Info($"Directory deleted: {metadata.Name} (ID: {directoryId})");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error deleting directory: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> MoveFilesToDirectory(IEnumerable<string> fileIds, string targetDirectoryId, string userId)
        {
            if (fileIds == null)
                throw new ArgumentNullException(nameof(fileIds));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            var fileIdList = fileIds.ToList();
            if (fileIdList.Count == 0)
                return true;
            
            try
            {
                // Validate target directory if specified
                if (!string.IsNullOrEmpty(targetDirectoryId))
                {
                    var targetDir = await _directoryRepository.GetDirectoryMetadataById(targetDirectoryId);
                    if (targetDir == null)
                    {
                        _logService.Warning($"Target directory not found: {targetDirectoryId}");
                        return false;
                    }
                    
                    // Check if the target directory belongs to the user
                    if (targetDir.UserId != userId)
                    {
                        _logService.Warning($"User {userId} attempted to move files to directory owned by {targetDir.UserId}");
                        return false;
                    }
                    
                    // Ensure the physical directory exists
                    _storageService.CreateDirectory(targetDir.DirectoryPath);
                }
                
                bool allSuccessful = true;
                int successCount = 0;
                
                foreach (string fileId in fileIdList)
                {
                    // Use the file service's MoveFileToDirectory method for each file
                    bool moved = await MoveFileToDirectory(fileId, targetDirectoryId, userId);
                    
                    if (moved)
                    {
                        successCount++;
                    }
                    else
                    {
                        allSuccessful = false;
                        _logService.Warning($"Failed to move file {fileId} to directory {targetDirectoryId ?? "root"}");
                    }
                }
                
                _logService.Info($"Moved {successCount}/{fileIdList.Count} files to directory {targetDirectoryId ?? "root"} for user {userId}");
                
                return allSuccessful;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error moving files to directory: {ex.Message}", ex);
                return false;
            }
        }

       public async Task<bool> MoveFileToDirectory(string fileId, string targetDirectoryId, string userId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
                
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                // Get the file metadata
                var fileMetadata = await _fileRepository.GetFileMetadataById(fileId);
                if (fileMetadata == null)
                {
                    _logService.Warning($"File not found: {fileId}");
                    return false;
                }
                
                // Check if the user owns the file
                if (fileMetadata.UserId != userId)
                {
                    _logService.Warning($"User {userId} attempted to move file owned by {fileMetadata.UserId}");
                    return false;
                }
                
                // If the file is already in the target directory, nothing to do
                if ((targetDirectoryId == null && fileMetadata.DirectoryId == null) ||
                    (fileMetadata.DirectoryId == targetDirectoryId))
                {
                    _logService.Debug($"File {fileId} is already in the specified directory");
                    return true;
                }
                
                // Determine the target directory path
                string targetPath;
                if (string.IsNullOrEmpty(targetDirectoryId))
                {
                    // Target is root directory
                    targetPath = _storageService.GetUserDirectory(userId);
                }
                else
                {
                    // Target is a specific directory
                    var targetDir = await _fileRepository.GetDirectoryById(targetDirectoryId);
                    if (targetDir == null || targetDir.UserId != userId)
                    {
                        _logService.Warning($"Target directory not found or not owned by user: {targetDirectoryId}");
                        return false;
                    }
                    
                    targetPath = targetDir.DirectoryPath;
                }
                
                // Get just the filename part of the current path
                string fileName = Path.GetFileName(fileMetadata.FilePath);
                string newFilePath = Path.Combine(targetPath, fileName);
                
                // If a file with the same name already exists in the target, create a unique name
                if (new FileInfo(newFilePath).Exists && newFilePath != fileMetadata.FilePath)
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    string uniqueName = $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    newFilePath = Path.Combine(targetPath, uniqueName);
                }
                
                // Move the physical file using the storage service
                bool moveSuccess = _storageService.MoveFile(fileMetadata.FilePath, newFilePath);
                if (!moveSuccess && new FileInfo(fileMetadata.FilePath).Exists)
                {
                    _logService.Warning($"Failed to move file from {fileMetadata.FilePath} to {newFilePath}");
                    return false;
                }
                
                // Update the file metadata
                string oldDirectoryId = fileMetadata.DirectoryId;
                string oldFilePath = fileMetadata.FilePath;
                
                fileMetadata.DirectoryId = targetDirectoryId;
                fileMetadata.FilePath = newFilePath;
                fileMetadata.UpdatedAt = DateTime.Now;
                
                // Update the metadata in the repository
                bool success = await _fileRepository.UpdateFileMetadataAsync(fileMetadata);
                
                if (success)
                {
                    _logService.Info($"File moved: {fileId} from directory {oldDirectoryId ?? "root"} to {targetDirectoryId ?? "root"}, path updated from {oldFilePath} to {newFilePath}");
                    return true;
                }
                else
                {
                    _logService.Error($"Failed to update metadata for file {fileId} after moving");
                    
                    // Try to move the file back if we moved it
                    if (new FileInfo(newFilePath).Exists && oldFilePath != newFilePath)
                    {
                        _storageService.MoveFile(newFilePath, oldFilePath);
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error moving file to directory: {ex.Message}", ex);
                return false;
            }
        }

       // Gets a directory by its ID
        public async Task<DirectoryMetadata> GetDirectoryById(string directoryId, string userId)
        {
            if (string.IsNullOrEmpty(directoryId))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));
            
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            try
            {
                var directory = await _directoryRepository.GetDirectoryMetadataById(directoryId);
                
                // Check if the directory exists and belongs to the user
                if (directory == null || directory.UserId != userId)
                {
                    return null;
                }
                
                return directory;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error getting directory: {ex.Message}", ex);
                return null;
            }
        }
        
        // Sanitizes a directory name to ensure it's safe for the file system
        private string SanitizeDirectoryName(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
                return "unnamed_directory";
            
            // Replace invalid characters with underscores
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                directoryName = directoryName.Replace(c, '_');
            }
            
            // Ensure the directory name isn't too long
            const int maxDirectoryNameLength = 100;
            if (directoryName.Length > maxDirectoryNameLength)
            {
                directoryName = directoryName.Substring(0, maxDirectoryNameLength);
            }
            
            return directoryName;
        }
    }
}
