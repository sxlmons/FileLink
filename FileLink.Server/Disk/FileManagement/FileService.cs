using FileLink.Server.Core.Exceptions;
using FileLink.Server.Services.Logging;
using System.Buffers;

namespace FileLink.Server.Disk.FileManagement;

// Service that provides file management functionality
// These methods will be used by the CommandHandler classes
public class FileService
{
    private readonly IFileRepository _fileRepository;
    private readonly PhysicalStorageService _storageService;    
    private readonly LogService _logService;
    private readonly ArrayPool<byte> _bufferPool;
    
    // Gets the chunk size for file transfers
    public int ChunkSize { get; }
    
    // Initializes new instance of the FileService class
    public FileService(IFileRepository fileRepository, PhysicalStorageService storageService, LogService logService, int chunkSize = 1024 * 1024)
    {
        _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logService = logService  ?? throw new ArgumentNullException(nameof(logService));
        ChunkSize = chunkSize;
        
        // Initialize the buffer pool for efficient memory usage yo!
        _bufferPool = ArrayPool<byte>.Shared;
    }
    
    // Initializes a file upload
    public async Task<FileMetadata> InitializeFileUpload(string userId, string fileName, long fileSize, string contentType, string directoryId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be empty.", nameof(fileName));
            
            if (fileSize <= 0)
                throw new ArgumentException("File size must be greater than zero.", nameof(fileSize));
            
            // Sanitize the file name
            fileName = SanitizeFileName(fileName);
            
            // Generate a unique file ID
            string fileId = Guid.NewGuid().ToString();
            
            // Generate the file path
            string filePath;
            
            if (string.IsNullOrEmpty(directoryId))
            {
                // Store in user's root directory
                filePath = _storageService.GetRootFilePath(userId, fileName, fileId);
            }
            else
            {
                // Lookup the directory's physical path
                var directoryMetadata = await _fileRepository.GetDirectoryById(directoryId);
                if (directoryMetadata == null || directoryMetadata.UserId != userId)
                {
                    throw new FileOperationException($"Directory {directoryId} not found or not owned by user {userId}");
                }
                
                // Get the file path in this directory
                filePath = _storageService.GetFilePathInDirectory(directoryMetadata.DirectoryPath, fileName, fileId);
                
                // Ensure the directory exists
                _storageService.CreateDirectory(directoryMetadata.DirectoryPath);
            }
            
            // Create file metadata
            var metadata = new FileMetadata(userId, fileName, fileSize, contentType, filePath)
            {
                TotalChunks = CalculateTotalChunks(fileSize),
                ChunksReceived = 0,
                IsComplete = false,
                DirectoryId = directoryId
            };
            
            // Create an empty file
            if (!_storageService.CreateEmptyFile(filePath))
            {
                throw new FileOperationException($"Failed to create file at {filePath}");
            }
            
            // Add the metadata to the repository
            bool success = await _fileRepository.AddFileMetadataAsync(metadata);
            
            if (!success)
            {
                // Clean up the empty file
                _storageService.DeleteFile(filePath);
                throw new FileOperationException("Failed to initialize file upload.");
            }
            
            _logService.Info($"File upload initialized: {fileName} (ID: {fileId}, Size: {fileSize} bytes, User: {userId}, Directory: {directoryId ?? "root"}, Path: {filePath})");
            
            return metadata;
        }
        catch (Exception ex) when (!(ex is FileOperationException))
        {
            _logService.Error($"Error initializing file upload: {ex.Message}", ex);
            throw new FileOperationException("Failed to initialize file upload.", ex);
        }
    }
    
    // Processes a file chunk for uploads
     public async Task<bool> ProcessFileChunk(string fileId, int chunkIndex, bool isLastChunk, byte[] chunkData)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
            
            if (chunkIndex < 0)
                throw new ArgumentException("Chunk index must be non-negative.", nameof(chunkIndex));
            
            if (chunkData == null || chunkData.Length == 0)
                throw new ArgumentException("Chunk data cannot be empty.", nameof(chunkData));
            
            try
            {
                // Get the file metadata
                var metadata = await _fileRepository.GetFileMetadataById(fileId);
                
                if (metadata == null)
                {
                    _logService.Warning($"Attempted to process chunk for non-existent file: {fileId}");
                    return false;
                }
                
                // Validate chunk index
                if (chunkIndex != metadata.ChunksReceived)
                {
                    _logService.Warning($"Received out-of-order chunk {chunkIndex} for file {fileId}, expected {metadata.ChunksReceived}");
                    return false;
                }
                
                // Calculate the offset in the file
                long offset = (long)chunkIndex * ChunkSize;
                
                // Write the chunk to the file using the storage service
                bool writeSuccess = await _storageService.WriteFileChunk(metadata.FilePath, chunkData, offset);
                if (!writeSuccess)
                {
                    _logService.Warning($"Failed to write chunk {chunkIndex} for file {fileId}");
                    return false;
                }
                
                // Update the metadata
                metadata.AddChunk();
                
                // If this is the last chunk, mark the file as complete
                if (isLastChunk)
                {
                    metadata.MarkComplete();
                }
                
                // Update the metadata in the repository
                await _fileRepository.UpdateFileMetadataAsync(metadata);
                
                _logService.Debug($"Processed chunk {chunkIndex} for file {fileId} (Size: {chunkData.Length} bytes)");
                
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing file chunk: {ex.Message}", ex);
                return false;
            }
        }
    
    
    // Finalizes a file upload
    public async Task<bool> FinalizeFileUpload(string fileId)
    {
        if  (string.IsNullOrEmpty(fileId))
            throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
        try
        {
            // Get the file metadata
            var metadata = await _fileRepository.GetFileMetadataById(fileId);

            if (metadata == null)
            {
                _logService.Warning($"File {fileId} does not exist.");
                return false;
            }
            
            // Check if all chunks have been received 
            if (metadata.ChunksReceived < metadata.TotalChunks)
            {
                _logService.Warning($"Attempted to finalize incomplete upload: {fileId}, (Received: {metadata.ChunksReceived}, Totals)");
            }
            
            // Mark complete
            metadata.MarkComplete();
            
            // Update the metadata in the repository
            await _fileRepository.UpdateFileMetadataAsync(metadata);

            // Verify the file size
            var fileInfo = new FileInfo(metadata.FilePath);
            
            if (fileInfo.Length != metadata.FileSize)
            {
                _logService.Warning($"File {fileId} does not have the expected size of {metadata.FileSize}, Actual {fileInfo.Length}");
                // this is still a success but log the discrepancy
            }
            
            _logService.Debug($"Finalized file {metadata.FileName} (ID: {fileId}, Size: {fileInfo.Length} Bytes).");
            return true;
        }
        catch (Exception ex)
        {
            _logService.Error($"Error while finalizing file {ex.Message}.", ex);
            return false;
        }
    }
    
    // initialize file download
    public async Task<FileMetadata> InitializeFileDownload(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId))
            throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
        
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        
        try
        {
            // Get the file metadata
            var metadata = await _fileRepository.GetFileMetadataById(fileId);
            
            if (metadata == null)
            {
                _logService.Warning($"Attempted to download non-existent file: {fileId}");
                return null;
            }
            
            // Check if the user owns the file
            if (!await ValidateUserOwnership(fileId, userId))
            {
                _logService.Warning($"User {userId} attempted to download file {fileId} owned by {metadata.UserId}");
                return null;
            }
            
            // Check if the file is complete
            if (!metadata.IsComplete)
            {
                _logService.Warning($"Attempted to download incomplete file: {fileId}");
                return null;
            }
            
            // Check if the file exists
            if (!File.Exists(metadata.FilePath))
            {
                _logService.Warning($"File not found at {metadata.FilePath} for file {fileId}");
                return null;
            }
            
            _logService.Info($"File download initialized: {metadata.FileName} (ID: {fileId}, Size: {metadata.FileSize} bytes, User: {userId})");
            
            return metadata;
        }
        catch (Exception ex)
        {
            _logService.Error($"Error initializing file download: {ex.Message}", ex);
            throw new FileOperationException("Failed to initialize file download.", ex);
        }
    }
    
    // get a file chunk
    public async Task<(byte[] data, bool isLastChunk)> GetFileChunk(string fileId, int chunkIndex)
    {
        if (string.IsNullOrEmpty(fileId))
            throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
        
        if (chunkIndex < 0)
            throw new ArgumentException("Chunk index must be non-negative.", nameof(chunkIndex));
        
        try
        {
            // Get the file metadata
            var metadata = await _fileRepository.GetFileMetadataById(fileId);
            
            if (metadata == null)
            {
                _logService.Warning($"Attempted to get chunk for non-existent file: {fileId}");
                return (null, false);
            }
            
            // Check if the file exists
            if (!File.Exists(metadata.FilePath))
            {
                _logService.Warning($"File not found at {metadata.FilePath} for file {fileId}");
                return (null, false);
            }
            
            // Calculate the offset in the file
            long offset = (long)chunkIndex * ChunkSize;
            
            // Check if this is the last chunk
            int totalChunks = CalculateTotalChunks(metadata.FileSize);
            bool isLastChunk = chunkIndex == totalChunks - 1;
            
            // Calculate the chunk size (the last chunk may be smaller)
            int actualChunkSize = isLastChunk
                ? (int)(metadata.FileSize - offset)
                : ChunkSize;
            
            // Check if the offset is valid
            if (offset >= metadata.FileSize)
            {
                _logService.Warning($"Invalid chunk index {chunkIndex} for file {fileId} (Size: {metadata.FileSize}, Offset: {offset})");
                return (null, false);
            }
            
            // Read the chunk from the file
            byte[] buffer = _bufferPool.Rent(actualChunkSize);
            
            try
            {
                using (var fileStream = new FileStream(metadata.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    int bytesRead = await fileStream.ReadAsync(buffer, 0, actualChunkSize);
                    
                    if (bytesRead != actualChunkSize)
                    {
                        _logService.Warning($"Expected to read {actualChunkSize} bytes, but read {bytesRead} bytes for file {fileId}");
                    }
                    
                    // Copy the data to a new array of the exact size
                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    
                    _logService.Debug($"Read chunk {chunkIndex} for file {fileId} (Size: {bytesRead} bytes, IsLastChunk: {isLastChunk})");
                    
                    return (data, isLastChunk);
                }
            }
            finally
            {
                // Return the buffer to the pool
                _bufferPool.Return(buffer);
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error getting file chunk: {ex.Message}", ex);
            return (null, false);
        }
    }
    
    // deletes a file
    public async Task<bool> DeleteFile(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId))
            throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
    
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
    
        try
        {
            // Get the file metadata
            var metadata = await _fileRepository.GetFileMetadataById(fileId);
        
            if (metadata == null)
            {
                _logService.Warning($"Attempted to delete non-existent file: {fileId}");
                return false;
            }
        
            // Check if the user owns the file
            if (metadata.UserId != userId)
            {
                _logService.Warning($"User {userId} attempted to delete file {fileId} owned by {metadata.UserId}");
                return false;
            }
        
            // Delete the file using the storage service
            _storageService.DeleteFile(metadata.FilePath);
        
            // Delete the metadata
            bool success = await _fileRepository.DeleteFileMetadataAsync(fileId);
        
            if (success)
            {
                _logService.Info($"File deleted: {metadata.FileName} (ID: {fileId}, User: {userId})");
            }
        
            return success;
        }
        catch (Exception ex)
        {
            _logService.Error($"Error deleting file: {ex.Message}", ex);
            return false;
        }
    }
    
    // gets a list of all files
    public async Task<IEnumerable<FileMetadata>> GetUserFiles(string userId)
    {
        // Check if user ID is empty
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        
        try
        {
            // Get files by user ID
            var files = await _fileRepository.GetFileMetadataByUserId(userId);
            _logService.Debug($"Retrieved {files.Count()} files for user {userId}");
            return files;
        }   
        catch (Exception ex)
        {
            // Log error
            _logService.Error($"Error getting user files: {ex.Message}", ex);
            throw new FileOperationException($"Failed to get user files for user {userId}.", ex);
        }
    }
        
    // Validates that a user owns selected file
    private async Task<bool> ValidateUserOwnership(string fileId, string userId)
    {
        var metadata = await _fileRepository.GetFileMetadataById(fileId);
        
        if  (metadata == null)
            return false;
            
        return (metadata.UserId == userId);
    }
    
    // Calculates the total number of chunks for a file of the given size
    private int CalculateTotalChunks(long fileSize)
    {
        return (int)Math.Ceiling((double)fileSize / ChunkSize);
    }
    
    // Sanitize a file name to ensure its safe for the file system
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return "unnamed_file";
        }
        // Replace invalid characters with underscores
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        
        // Ensure the file name isn't too long
        const int maxFileNameLength = 100;
        if (fileName.Length > maxFileNameLength)
        {
            string extension = Path.GetExtension(fileName);
            fileName = fileName.Substring(0, maxFileNameLength - extension.Length) + extension;
        }
        
        // return the truncated or same filename 
        return fileName;
    }
    
    public async Task<bool> UpdateFileMetadata(FileMetadata fileMetadata)
    {
        if (fileMetadata == null)
            throw new ArgumentNullException(nameof(fileMetadata));
    
        try
        {
            bool success = await _fileRepository.UpdateFileMetadataAsync(fileMetadata);
        
            if (success)
            {
                _logService.Debug($"File metadata updated: {fileMetadata.FileName} (ID: {fileMetadata.Id})");
            }
            else
            {
                _logService.Warning($"Failed to update metadata for file {fileMetadata.Id}");
            }
        
            return success;
        }
        catch (Exception ex)
        {
            _logService.Error($"Error updating file metadata: {ex.Message}", ex);
            return false;
        }
    }
    
    // FUTURE IMPLEMENTATION: Moving files between directories
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
}