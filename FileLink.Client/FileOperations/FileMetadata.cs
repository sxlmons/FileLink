using System;
using System.Text.Json.Serialization;

namespace FileLink.Client.FileOperations
{
    // Represents metadata for a file stored in the cloud file server.
    public class FileMetadata
    {
        
        // Gets or sets the unique identifier for the file.
        [JsonPropertyName("Id")]
        public string Id { get; set; } = string.Empty;
        
        // Gets or sets the name of the file.
        [JsonPropertyName("FileName")]
        public string FileName { get; set; } = string.Empty;
        
        // Gets or sets the size of the file in bytes.
        [JsonPropertyName("FileSize")]
        public long FileSize { get; set; }
        
        // Gets or sets the content type (MIME type) of the file.
        [JsonPropertyName("ContentType")]
        public string ContentType { get; set; } = string.Empty;
        
        // Gets or sets the date and time when the file was created.
        [JsonPropertyName("CreatedAt")]
        public DateTime CreatedAt { get; set; }
        
        // Gets or sets the date and time when the file was last updated.
        [JsonPropertyName("UpdatedAt")]
        public DateTime UpdatedAt { get; set; }
        
        // Gets or sets a value indicating whether the file upload is complete.
        [JsonPropertyName("IsComplete")]
        public bool IsComplete { get; set; }
        
        // Gets or sets the total number of chunks for the file.
        [JsonIgnore]
        public int TotalChunks { get; set; }
        
        // Gets a formatted string representation of the file size.
        [JsonIgnore]
        public string FormattedSize
        {
            get
            {
                const long KB = 1024;
                const long MB = KB * 1024;
                const long GB = MB * 1024;

                return FileSize switch
                {
                    < KB => $"{FileSize} B",
                    < MB => $"{FileSize / KB:F2} KB",
                    < GB => $"{FileSize / MB:F2} MB",
                    _ => $"{FileSize / GB:F2} GB"
                };
            }
        }
        
        // Returns a string representation of this file metadata.
        public override string ToString()
        {
            return $"{FileName} ({FormattedSize}, {ContentType}, Last updated: {UpdatedAt})";
        }
    }

    
    // Response from a file upload initialization request.
    internal class FileUploadInitResponse
    {
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("FileId")]
        public string FileId { get; set; } = string.Empty;

        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;
    }

    // Response from a file download initialization request.
    internal class FileDownloadInitResponse
    {
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("FileId")]
        public string FileId { get; set; } = string.Empty;

        [JsonPropertyName("FileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("FileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("ContentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("TotalChunks")]
        public int TotalChunks { get; set; }

        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;
    }
}