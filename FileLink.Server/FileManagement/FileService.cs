using FileLink.Server.Core.Exceptions;
using FileLink.Server.Services.Logging;
using System.Buffers;

namespace FileLink.Server.FileManagement;

// Service that provides file management functionality
// These methods will be used by the CommandHandler classes
public class FileService
{
    private readonly IFileRepository _fileRepository;
    private readonly string _storagePath;
    private readonly LogService _logService;
    private readonly ArrayPool<byte> _bufferPool;
    
    // Gets the chunk size for file transfers
    public int ChunkSize { get; }
    
    // Initializes new instance of the FileService class
    public FileService(IFileRepository fileRepository, string storagePath, LogService logService, int chunkSize = 1024 * 1024)
    {
        _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
        _storagePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
        _logService = logService  ?? throw new ArgumentNullException(nameof(logService));
        ChunkSize = chunkSize;
        
        // Initialize the buffer pool for efficient memory usage yo!
        _bufferPool = ArrayPool<byte>.Shared;
        
        // Ensure the storage directory exists
        Directory.CreateDirectory(_storagePath);
    }
    
    // Initializes a file upload
    public async Task<FileMetadata> InitializeFileUpload(string userId, string fileName, long fileSize, string contentType)
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
            
            // Create the user's directory if it doesn't exist 
            string userDirectory = Path.Combine(_storagePath, userId);
            Directory.CreateDirectory(userDirectory);
            
            // Generate a unique file ID
            string fileId = Guid.NewGuid().ToString();

            // Generate a unique file path
            string filePath = Path.Combine(userDirectory, fileId + "_" + fileName);
            
            // Create file metadata
            var metadata = new FileMetadata(userId, fileName, fileSize, contentType, filePath)
            {
                TotalChunks = CalculateTotalChunks(fileSize),
                ChunksReceived = 0,
                IsComplete = false
            };
            
            // Add the metadata to the repository
            bool success = await _fileRepository.AddFileMetadataAsync(metadata);

            if (!success)
            {
                throw new FileOperationException("Failed to initialize file upload.");
            }
            
            // Create an empty file
            await using (var fs = File.Create(filePath))
            {
                // No need to write until the chunking process
            }
            
            _logService.Info($"File upload initialized: {fileName} (ID: {fileId}, Size: {fileSize}, User ID: {userId}).");
            
            return metadata;
        }
        catch(Exception ex) when (!(ex is FileOperationException))
        {
            _logService.Error($"Error initializing file upload: {ex.Message}", ex);
            throw new FileOperationException($"Failed to initialize upload", ex);
        }
    }

    // Processes a file chunk, the second step in the file upload process
    public async Task<bool> ProcessFileChunk(string fileId, int chunkIndex, bool isLastChunk, byte[] chunkData)
    {
        throw new NotImplementedException();
    }
    
    // Finalizes a file upload
    public async Task<bool> FinalizeFileUpload(string fileId)
    {
        throw new NotImplementedException();
    }
    
    // initialize file download
    public async Task<FileMetadata> InitializeFileDownload(string fileId, string userId)
    {
        throw new NotImplementedException();
    }
    
    // get a file chunk
    public async Task<(byte[] data, bool isLastChunk)> GetFileChunk(string fileId, int chunkIndex)
    {
        throw new NotImplementedException();
    }
    
    // deletes a file
    public async Task<bool> DeleteFile(string fileId, string userId)
    {
        throw new NotImplementedException();
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
}