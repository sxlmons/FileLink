namespace FileLink.Server.Disk.FileManagement;

    // Represents metadata for a file stored in our server
    public class FileMetadata
    {
        // General info
        public string Id { get; set; }
        public string UserId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string FilePath { get; set; }
        public bool IsComplete { get; set; }
        public int ChunksReceived { get; set; }
        public int TotalChunks { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Maybe:
        public DateTime? LastAccessedAt { get; set; }
        
        // New for the Directory Feature
        public string DirectoryId { get; set; }
        
        // Initializes a new instance of the FileMetadata class.
        public FileMetadata()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsComplete = false;
            ChunksReceived = 0;
        }
        
        // Initializes a new instance of the FileMetadata class with the specified user ID and file name.
        public FileMetadata(string userId, string fileName, long fileSize, string contentType, string filePath) : this()
        {
            UserId = userId;
            FileName = fileName;
            FileSize = fileSize;
            ContentType = contentType;
            FilePath = filePath;
        }
        
        // Updates the metadata to mark a chunk as received 
        public void AddChunk()
        {
            ChunksReceived++;
            UpdatedAt = DateTime.UtcNow;
            
            // Check if all chunks have been received
            if (ChunksReceived >= TotalChunks)
            {
                IsComplete = true;
            }
        }
        
        public void MoveToDirectory(string directoryId, string newFilePath)
        {
            DirectoryId = directoryId;
            FilePath = newFilePath;
            UpdatedAt = DateTime.Now;
        }

        // Marks the file as complete
        public void MarkComplete()
        {
            IsComplete = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
