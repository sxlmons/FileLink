namespace FileLink.Server.Disk.DirectoryManagement
{
    // Represents metadata for a directory stored in the system
    public class DirectoryMetadata
    {
        // Directory fields
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string ParentDirectoryId { get; set; }
        public string DirectoryPath { get; set; }
        
        // Time stamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Initializes a new instance of the DirectoryMetadata class
        public DirectoryMetadata()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        
        // Initializes a new instance of the DirectoryMetadata class with the specified parameters
        public DirectoryMetadata(string userId, string name, string parentDirectoryId, string directoryPath) : this()
        {
            UserId = userId;
            Name = name;
            ParentDirectoryId = parentDirectoryId;
            DirectoryPath = directoryPath;
        }
        
        // Updates the metadata when the directory is renamed
        public void Rename(string newName)
        {
            Name = newName;
            UpdatedAt = DateTime.Now;
        }
        
        // Updates the metadata when the directory is moved
        public void Move(string newParentDirectoryId, string newDirectoryPath)
        {
            ParentDirectoryId = newParentDirectoryId;
            DirectoryPath = newDirectoryPath;
            UpdatedAt = DateTime.Now;
        }
        
        // Checks if this directory is a root directory
        public bool IsRoot()
        {
            return string.IsNullOrEmpty(ParentDirectoryId);
        }
    }
}