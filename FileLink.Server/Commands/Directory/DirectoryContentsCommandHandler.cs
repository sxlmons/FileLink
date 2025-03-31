using FileLink.Server.Commands;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands.Directory
{
    /// <summary>
    /// Command handler for directory contents requests.
    /// Implements the Command pattern.
    /// </summary>
    public class DirectoryContentsCommandHandler : ICommandHandler
    {
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        // Initializes a new instance of the DirectoryContentsCommandHandler class
        public DirectoryContentsCommandHandler(DirectoryService directoryService, LogService logService)
        {
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        // Determines whether this handler can process the specified command code
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST;
        }

        // Handles a directory contents request packet
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received directory contents request from unauthenticated session");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "You must be logged in to list directory contents.", "");
                }

                // Check if the user ID in the packet matches the session's user ID
                if (!string.IsNullOrEmpty(packet.UserId) && packet.UserId != session.UserId)
                {
                    _logService.Warning($"User ID mismatch in directory contents request: {packet.UserId} vs session: {session.UserId}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the authenticated user.", session.UserId);
                }

                // Get directory ID from metadata
                string directoryId = null;
                if (packet.Metadata.TryGetValue("DirectoryId", out string dirId) && dirId != "root")
                {
                    directoryId = dirId;

                    // Validate directory if specified
                    var dir = await _directoryService.GetDirectoryById(directoryId, session.UserId);
                    if (dir == null)
                    {
                        _logService.Warning($"Directory not found or not owned by user: {directoryId}");
                        return _packetFactory.CreateErrorResponse(packet.CommandCode, "Directory not found or you do not have permission to access it.", session.UserId);
                    }
                }

                _logService.Debug($"Fetching contents for directory {directoryId ?? "root"} for user {session.UserId}");

                // Get directory contents
                var (files, directories) = await _directoryService.GetDirectoryContents(session.UserId, directoryId);

                // Project the files to a simpler format for the client
                var fileList = files.Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.FileSize,
                    f.ContentType,
                    f.CreatedAt,
                    f.UpdatedAt,
                    f.IsComplete,
                    DirectoryId = f.DirectoryId ?? "root"
                }).ToList();

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

                _logService.Info($"Returning directory contents with {fileList.Count} files and {directoryList.Count} directories for user {session.UserId} in directory {directoryId ?? "root"}");

                // Create and return the response
                return _packetFactory.CreateDirectoryContentsResponse(fileList, directoryList, directoryId, session.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing directory contents request: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "An error occurred while listing directory contents.", session.UserId);
            }
        }
    }
}
