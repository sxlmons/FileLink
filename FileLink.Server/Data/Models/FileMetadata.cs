namespace FileLink.Server.Data.Models;

    // Represents metadata for a file stored in our server
    public class FileMetadata
    {
        // Unique ID for the file.
        public string Id { get; set; }
        
        // The user ID of the file owner.
        public string UserId { get; set; }
        
        // Original name of the file.
        public string FileName { get; set; }
        
        // The size of the file in bytes.
        public long FileSize { get; set; }
        
        // The type of file
        public string ContentType { get; set; }
        
        // The path where the file is stored on the server.
        public string StoragePath { get; set; }
        
        // Timestamp of when the file was created.
        public DateTime CreatedAt { get; set; }
        
        // Timestamp of when the file was last updated.
        public DateTime UpdatedAt { get; set; }
        
        // Timestamp of when the file was last accessed.
        public DateTime? LastAccessedAt { get; set; }
        
        // Initializes a new instance of the FileMetadata class.
        public FileMetadata()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
        }
        
        // Initializes a new instance of the FileMetadata class with the specified user ID and file name.
        public FileMetadata(string userId, string fileName) : this()
        {
            UserId = userId;
            FileName = fileName;
        }
        
        // Gets the file extension
        public string GetFileExtension()
        {
            return Path.GetExtension(FileName);
        }
        
        // Get the file name without the extension
        public string GetFileNameWithoutExtension()
        {
            return Path.GetFileNameWithoutExtension(FileName);
        }
    }
