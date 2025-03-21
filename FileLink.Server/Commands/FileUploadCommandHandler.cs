using FileLink.Server.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands;

// Command handler for file uploads
// Implements the command pattern
public class FileUploadCommandHandler : ICommandHandler
{
    private readonly FileService _fileService;
    private readonly LogService _logService;
    private readonly PacketFactory _packetFactory = new PacketFactory();
    
    // Constructor
    public FileUploadCommandHandler(FileService fileService, LogService logService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }
    
    // Determines whether this handler can process the specified command code 
    public bool CanHandle(int commandCode)
    {
        return commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST ||
               commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST ||
               commandCode == FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST;    
    }
    
    // Handle a file upload packet yo
    public async Task<Packet> Handle(Packet packet, ClientSession session)
    {
        try
        {
            // Check if the session is authenticated 
            if (string.IsNullOrEmpty(session.UserId))
            {
                _logService.Warning("Received file upload request from unauthenticated session.");
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to upload files.", "");
            }
            
            // Check if the user ID in the packet matches the session user ID
            if (!string.IsNullOrEmpty(session.UserId) && packet.UserId != session.UserId)
            {
                _logService.Warning($"User ID mismatch in file upload request: {packet.UserId} vs session: {session.UserId}");
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
            }
            
            // Handle the appropriate upload command
            switch (packet.CommandCode)
            {
                case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST:
                    return await HandleFileUploadInitRequest(packet, session);
                
                case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST:
                    return await HandleFileUploadChunkRequest(packet, session);
                        
                case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST:
                    return await HandleFileUploadCompleteRequest(packet, session);
                
                default:
                    _logService.Warning($"Unexpected command code in file upload handler: {packet.CommandCode}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "Unexpected command code for file upload operation.", session.UserId);
            }
        }
        catch (Exception ex)
        {
            _logService.Error($"Error processing file upload request: {ex.Message}", ex);
            return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred during the file upload operation.", session.UserId);
        }
    }

    private async Task<Packet> HandleFileUploadInitRequest(Packet packet, ClientSession session)
    {
        return packet;
    }

    private async Task<Packet> HandleFileUploadChunkRequest(Packet packet, ClientSession session)
    {
        return packet;
    }

    private async Task<Packet> HandleFileUploadCompleteRequest(Packet packet, ClientSession session)
    {
        return packet;
    }
    
    private class FileUploadInitInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
    }
}