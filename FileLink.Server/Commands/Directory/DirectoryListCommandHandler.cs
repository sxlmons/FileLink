using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands.Directory
{
    // Command handler for file list requests
    // Implements the Command pattern
    public class DirectoryListCommandHandler : ICommandHandler
    {
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        
        // Initializes a new instance of the FileListCommandHandler class
        public DirectoryListCommandHandler(DirectoryService directoryService, LogService logService)
        {
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.DIRECTORY_LIST_REQUEST;
        }
        
        // Handles a directory list request packet
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received directory list request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to list directories.", "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in directory list request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
                }

                // Get the parent directory ID from metadata (if any)
                string parentDirectoryId = null;
                if (packet.Metadata.TryGetValue("ParentDirectoryId", out string parentId) && !string.IsNullOrEmpty(parentId))
                {
                    parentDirectoryId = parentId;
                    
                    // Validate parent directory if specified
                    var parentDir = await _directoryService.GetDirectoryById(parentDirectoryId, session.UserId);
                    if (parentDir == null)
                    {
                        _logService.Warning($"Parent directory not found or not owned by user: {parentDirectoryId}");
                        return _packetFactory.CreateErrorResponse(packet.CommandCode, "Parent directory not found or you do not have permission to access it.", session.UserId);
                    }
                }

                _logService.Debug($"Fetching directories for user {session.UserId} in parent {parentDirectoryId ?? "root"}");
                
                // Get the list of directories for the user
                var directories = await _directoryService.GetDirectoriesInDirectory(session.UserId, parentDirectoryId);
                
                // Project the directories to a simpler format for the client
                var directoryList = directories.Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.ParentDirectoryId,
                    d.CreatedAt,
                    d.UpdatedAt,
                    IsRoot = d.ParentDirectoryId == null
                }).ToList();
                
                _logService.Info($"Returning directory list with {directoryList.Count} directories for user {session.UserId} in parent {parentDirectoryId ?? "root"}");
                
                // Create and return the response
                return _packetFactory.CreateDirectoryListResponse(directoryList, parentDirectoryId, session.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory list request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred while listing directories.", session.UserId);
            }
        }
    }
}