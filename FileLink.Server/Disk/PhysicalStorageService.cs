using FileLink.Server.Services.Logging;

namespace FileLink.Server.Disk
{
    // Service that handles physical storage operations for both files and directories.
    // This separates physical storage concerns from logical organization.
    public class PhysicalStorageService
    {
        private readonly string _storagePath;
        private readonly LogService _logService;

        public PhysicalStorageService(string storagePath, LogService logService)
        {
            _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Ensure root storage exists
            Directory.CreateDirectory(_storagePath);
        }
        
        // Gets the physical path for a file in the root directory
        public string GetRootFilePath(string userId, string fileName, string fileId)
        {
            string userDirectory = GetUserDirectory(userId);
            return Path.Combine(userDirectory, $"{fileName}_{fileId}");
        }
        
        // Gets the physical path for a directory
        public string GetDirectoryPath(string userId, string directoryName, string parentPath = null)
        {
            if (string.IsNullOrEmpty(parentPath))
            {
                // Root-level directory
                string userDirectory = GetUserDirectory(userId);
                return Path.Combine(userDirectory, directoryName);
            }
            else
            {
                // Nested directory
                return Path.Combine(parentPath, directoryName);
            } 
        }
        
        // Gets the physical path for a file in a specific directory
        public string GetFilePathInDirectory(string directoryPath, string fileName, string fileId)
        {
            return Path.Combine(directoryPath, $"{fileId}_{fileName}");
        }
        
        // Gets the users root directory path
        public string GetUserDirectory(string userId)
        {
            string userDirectory = Path.Combine(_storagePath, userId);
            
            // Ensure user directory exists
            if (!Directory.Exists(userDirectory))
            {
                Directory.CreateDirectory(userDirectory);
            }
            
            return userDirectory;
        }
        
        // Creates a physical directory
        public bool CreateDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    return false; // Already exists
                }
                
                Directory.CreateDirectory(directoryPath);
                _logService.Debug($"Created physical directory at {directoryPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error creating directory {directoryPath}: {ex.Message}", ex);
                return false;
            }
        }
        
        // Deletes a physical directory
        public bool DeleteDirectory(string directoryPath, bool recursive = false)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    return false; // Doesn't exist
                }
                
                Directory.Delete(directoryPath, recursive);
                _logService.Debug($"Deleted physical directory at {directoryPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error deleting directory {directoryPath}: {ex.Message}", ex);
                return false;
            }
        }
        
        // Creates an empty file or overwrites an existing file
        public bool CreateEmptyFile(string filePath)
        {
            try
            {
                using (var fs = File.Create(filePath))
                {
                    // Just create the file
                }
                
                _logService.Debug($"Created empty file at {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error creating file {filePath}: {ex.Message}", ex);
                return false;
            }
        }
        
        // Moves a file from one location to another
        public bool MoveFile(string sourcePath, string destinationPath)
        {
            try
            {
                // Ensure destination directory exists
                string destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                
                // Move the file
                if (File.Exists(sourcePath))
                {
                    // Only move if paths are different
                    if (sourcePath != destinationPath)
                    {
                        File.Move(sourcePath, destinationPath);
                        _logService.Debug($"Moved file from {sourcePath} to {destinationPath}");
                    }
                    return true;
                }
                else
                {
                    _logService.Warning($"Source file not found at {sourcePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error moving file from {sourcePath} to {destinationPath}: {ex.Message}", ex);
                return false;
            }
        }
        
        // Deletes a file
        public bool DeleteFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logService.Warning($"File not found at {filePath}");
                    return false;
                }
                
                File.Delete(filePath);
                _logService.Debug($"Deleted file at {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error deleting file {filePath}: {ex.Message}", ex);
                return false;
            }
        }
        
        // Writes data to a file
        public async Task<bool> WriteFileChunk(string filePath, byte[] data, long offset)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logService.Warning($"File not found at {filePath}");
                    return false;
                }
                
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    await fileStream.WriteAsync(data, 0, data.Length);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error writing to file {filePath}: {ex.Message}", ex);
                return false;
            }
        }
        
        // Reads data from a file
        public async Task<int> ReadFileChunk(string filePath, byte[] buffer, long offset, int count)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logService.Warning($"File not found at {filePath}");
                    return -1;
                }
                
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    return await fileStream.ReadAsync(buffer, 0, count);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error reading from file {filePath}: {ex.Message}", ex);
                return -1;
            }
        }
    }
}