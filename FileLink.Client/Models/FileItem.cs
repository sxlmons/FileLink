using System;

namespace FileLink.Client.Models
{
    // Represents a file in the server
    public class FileItem
    {
        public string Id { get; set; } = "";
        
        public string FileName { get; set; } = "";
        
        public string? DirectoryId { get; set; }
        
        public long FileSize { get; set; }
        
        public string ContentType { get; set; } = "";
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public string FormattedSize
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                else if (FileSize < 1024 * 1024 * 1024)
                    return $"{FileSize / (1024.0 * 1024.0):F1} MB";
                else
                    return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

        // Gets the extension
        public string Extension
        {
            get
            {
                return Path.GetExtension(FileName).ToLowerInvariant();
            }
        }

        // Gets the file type icon based on its extension.
        public string FileTypeIcon
        {
            get
            {
                string ext = Extension;
                
                return ext switch
                {
                    ".pdf" => "pdf_icon.png",
                    ".doc" or ".docx" => "doc_icon.png",
                    ".xls" or ".xlsx" => "xls_icon.png",
                    ".ppt" or ".pptx" => "ppt_icon.png",
                    ".txt" => "txt_icon.png",
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "image_icon.png",
                    ".mp3" or ".wav" or ".ogg" or ".flac" => "audio_icon.png",
                    ".mp4" or ".avi" or ".mov" or ".wmv" => "video_icon.png",
                    ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "archive_icon.png",
                    _ => "generic_file_icon.png",
                };
            }
        }
    }
}