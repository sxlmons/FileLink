using FileLink.Server.FileManagement;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands;

// Command handler for file uploads
// Implements the command pattern
public class FileUploadCommandHandler
{
    private readonly FileService _fileService;
    private readonly LogService _logService;
    private readonly PacketFactory _packetFactory = new PacketFactory();
    
    // Constructor
    
    // Protocol check
    
    // Handle a file upload packet yo
    
    // Handle a file upload initialization request
    
    // Handle a file upload chunk request
    
    // Handle a file complete request
    
    // Class for deserializing file upload initialization info
    private class FileUploadInitInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
    }
}