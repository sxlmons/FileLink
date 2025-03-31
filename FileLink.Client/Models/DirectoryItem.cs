using System;

namespace FileLink.Client.Models
{
    // Represents a directory in the server
    public class DirectoryItem
    {
        public string Id { get; set; } = "";
        
        public string Name { get; set; } = "";
        
        public string? ParentDirectoryId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public bool IsRoot => ParentDirectoryId == null;
    }
}