using FileLink.Server.Disk.FileManagement;
using FileLink.Server.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Server;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.SessionState
{
    // Represents the transfer state in the session state machine 
    // In this state, the client is in the process of uploading or downloading a file
    public class TransferState : ISessionState
    {
        private readonly FileService _fileService;
        private readonly FileMetadata _fileMetadata;
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();
        private DateTime _transferStartTime;
        private readonly bool _isUploading;
        private int _currentChunkIndex = 0;
        private int _totalChunks;
        private readonly string _transferType;
        
        // Gets the client session this state is associated with
        public ClientSession ClientSession { get; }
        
        // Initializes a new instance of the TransferState class
        public TransferState(ClientSession clientSession, FileService fileService, FileMetadata fileMetadata, bool isUploading, LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _fileMetadata = fileMetadata ?? throw new ArgumentNullException(nameof(fileMetadata));
            _isUploading = isUploading;
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _transferType = _isUploading ? "Upload" : "Download";
            
            // Calculate total chunks based on file size and chunk size
            _totalChunks = (int)Math.Ceiling((double)_fileMetadata.FileSize / _fileService.ChunkSize);
        }
        
        // Handles a packet received while in transfer state
        public async Task<Packet> HandlePacket(Packet packet)
        {
            // Verify user ID in the packet matches the users session ID
            if (!string.IsNullOrEmpty(ClientSession.UserId) 
                && !string.IsNullOrEmpty(packet.UserId) 
                && packet.UserId != ClientSession.UserId)
            {
                _logService.Warning($"User ID mismatch in packet: {packet.UserId} vs session: {ClientSession.UserId}");
                return _packetFactory.CreateErrorResponse(packet.CommandCode, "User ID in packet does not match the current User ID in session.", ClientSession.UserId);
            }

            // File operations start
            try
            {
                if (_isUploading)
                {
                    // File upload commands
                    switch (packet.CommandCode)
                    {
                        case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST:
                            return await HandleFileUploadChunkRequest(packet);
                        case FileLink.Server.Protocol.Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST:
                            return await HandleFileUploadCompleteRequest(packet);
                        default:
                            _logService.Warning($"Received unexpected command in TransferState (upload): {FileLink.Server.Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)}");
                            return _packetFactory.CreateErrorResponse(packet.CommandCode, $"Command {FileLink.Server.Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)} not supported during file upload.", ClientSession.UserId);
                    }
                }
                else
                {
                    // file download commands
                    switch (packet.CommandCode)
                    {
                        case FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST:
                            return await HandleFileDownloadChunkRequest(packet);
                        case FileLink.Server.Protocol.Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_RESPONSE:
                            return await HandleFileDownloadCompleteRequest(packet);
                        default:
                            _logService.Warning($"Received unexpected command in TransferState (download): {FileLink.Server.Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)}");
                            return _packetFactory.CreateErrorResponse(packet.CommandCode, $"Command {FileLink.Server.Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)} not supported during file download.", ClientSession.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling packet in TransferState: {ex.Message}", ex);
                
                // Transition back to authenticated state on error and send response
                ClientSession.TransitionToState(ClientSession.StateFactory.CreateAuthenticatedState(ClientSession));
                return _packetFactory.CreateErrorResponse(packet.CommandCode, $"An error occurred during file {_transferType}: {ex.Message}", ClientSession.UserId);
            }
        }
        
        private async Task<Packet> HandleFileUploadChunkRequest(Packet packet)
        {
            try
            {
                // Validate file ID
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || fileId != _fileMetadata.Id)
                {
                    _logService.Warning($"File ID mismatch in chunk request: {fileId} vs {_fileMetadata.Id}");
                    return _packetFactory.CreateFileUploadChunkResponse(false, _fileMetadata.Id, -1, "fileId mismatch", ClientSession.UserId);
                }
                
                // Validate chunk index
                if (!packet.Metadata.TryGetValue("ChunkIndex", out string chunkIndexStr) 
                    || !int.TryParse(chunkIndexStr, out int chunkIndex) 
                    || chunkIndex != _currentChunkIndex)
                {
                    _logService.Warning($"Chunk index mismatch in request: {chunkIndexStr} vs expected {_currentChunkIndex}");
                    return _packetFactory.CreateFileUploadChunkResponse(false, _fileMetadata.Id, _currentChunkIndex, $"Expected chunk index {_currentChunkIndex}.", ClientSession.UserId);
                }
                
                // Check if this is the last chunk
                bool isLastChunk = false;
                if (packet.Metadata.TryGetValue("IsLastChunk", out string isLastChunkStr))
                {
                    bool.TryParse(isLastChunkStr, out isLastChunk);
                }
                
                // Process the chunk 
                if (packet.Payload == null || packet.Payload.Length == 0)
                {
                    _logService.Warning($"Received empty chunk for file {_fileMetadata.Id}");
                    return _packetFactory.CreateFileUploadChunkResponse(false, _fileMetadata.Id, chunkIndex, "Chunk data is empty.", ClientSession.UserId);
                }
                
                bool success = await _fileService.ProcessFileChunk(_fileMetadata.Id, chunkIndex, isLastChunk, packet.Payload);

                if (success)
                {
                    _logService.Debug($"Processed chunk {chunkIndex}/{_totalChunks} for file {_fileMetadata.Id}");
                    
                    // Log progress periodically
                    if (_currentChunkIndex % 10 == 0 || isLastChunk)
                    {
                        double percentComplete = (double)(_currentChunkIndex + 1) / _totalChunks * 100;
                        _logService.Info($"File upload progress: {percentComplete:F1}% ({_currentChunkIndex + 1}/{_totalChunks} chunks) for file {_fileMetadata.FileName}");
                    }
                    
                    // Increment the chunk index for the next chunk
                    _currentChunkIndex++;
                    
                    return _packetFactory.CreateFileUploadChunkResponse(true, _fileMetadata.Id, chunkIndex, "Chunk processed successfully.", ClientSession.UserId);
                }
                else
                {
                    _logService.Error($"Failed to process chunk {chunkIndex} for file {_fileMetadata.Id}");
                    return _packetFactory.CreateFileUploadChunkResponse(false, _fileMetadata.Id, chunkIndex, "Failed to process chunk.", ClientSession.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing upload chunk: {ex.Message}", ex);
                return _packetFactory.CreateFileUploadChunkResponse(false, _fileMetadata.Id, _currentChunkIndex, $"Error processing chunk: {ex.Message}", ClientSession.UserId);
            }
        }

            
        private async Task<Packet> HandleFileUploadCompleteRequest(Packet packet)
        {
            try
            {
                // Validate file ID
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || fileId != _fileMetadata.Id)
                {
                    _logService.Warning($"File ID mismatch in upload complete request: {fileId} vs {_fileMetadata.Id}");
                    return _packetFactory.CreateFileUploadCompleteResponse(false, _fileMetadata.Id, "File ID mismatch.",
                        ClientSession.UserId);
                }
                
                // Finalize the upload 
                bool success = await _fileService.FinalizeFileUpload(_fileMetadata.Id);
                
                // Calculate transfer statistics
                TimeSpan transferTime = DateTime.Now - _transferStartTime;
                double transferSpeedMBps = _fileMetadata.FileSize / (1024.0 * 1024.0) / transferTime.TotalSeconds;
                
                _logService.Info($"File upload complete: {_fileMetadata.FileName} ({_fileMetadata.FileSize} bytes) in {transferTime.TotalSeconds:F1} seconds ({transferSpeedMBps:F2} MB/s)");                
                
                // Transition back to authenticated state
                ClientSession.TransitionToState(ClientSession.StateFactory.CreateAuthenticatedState(ClientSession));

                if (success)
                {
                    return _packetFactory.CreateFileUploadCompleteResponse(true, _fileMetadata.Id, "File upload completed sucessfully", ClientSession.UserId);
                }
                else
                {
                    return _packetFactory.CreateFileUploadCompleteResponse(false, _fileMetadata.Id, "Failed to finalize file upload.", ClientSession.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error finalizing file upload: {ex.Message}", ex);
                
                // Transition back to authenticated state on error
                ClientSession.TransitionToState(ClientSession.StateFactory.CreateAuthenticatedState(ClientSession));
                
                return _packetFactory.CreateFileUploadCompleteResponse(false, _fileMetadata.Id, $"Error finalizing file upload: {ex.Message}", ClientSession.UserId);
            }
        }

        // Handles a file download chunk request
        private async Task<Packet> HandleFileDownloadChunkRequest(Packet packet)
        {
            try
            {
                // Validate file ID
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || fileId != _fileMetadata.Id)
                {
                    _logService.Warning($"File ID mismatch in download chunk request: {fileId} vs {_fileMetadata.Id}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "File ID mismatch.", ClientSession.UserId);
                }
                
                // Get requested chunk index
                if (!packet.Metadata.TryGetValue("ChunkIndex", out string chunkIndexStr) || 
                    !int.TryParse(chunkIndexStr, out int chunkIndex))
                {
                    _logService.Warning($"Invalid chunk index in download request: {chunkIndexStr}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, "Invalid chunk index.", ClientSession.UserId);
                }
                
                // Retrieve the chunk
                var (chunkData, isLastChunk) = await _fileService.GetFileChunk(_fileMetadata.Id, chunkIndex);
                
                if (chunkData != null)
                {
                    _logService.Debug($"Sending chunk {chunkIndex}/{_totalChunks} for file {_fileMetadata.Id}");
                    
                    // Log progress periodically
                    if (chunkIndex % 10 == 0 || isLastChunk)
                    {
                        double percentComplete = (double)(chunkIndex + 1) / _totalChunks * 100;
                        _logService.Info($"File download progress: {percentComplete:F1}% ({chunkIndex + 1}/{_totalChunks} chunks) for file {_fileMetadata.FileName}");
                    }
                    
                    // Update current chunk index
                    _currentChunkIndex = chunkIndex + 1;
                    
                    return _packetFactory.CreateFileDownloadChunkResponse(true, _fileMetadata.Id, chunkIndex, isLastChunk, chunkData, "Chunk sent successfully.", ClientSession.UserId);
                }
                else
                {
                    _logService.Error($"Failed to retrieve chunk {chunkIndex} for file {_fileMetadata.Id}");
                    return _packetFactory.CreateErrorResponse(packet.CommandCode, $"Failed to retrieve chunk {chunkIndex}.", ClientSession.UserId);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"Error retrieving download chunk: {ex.Message}", ex);
                return _packetFactory.CreateErrorResponse(packet.CommandCode, $"Error retrieving chunk: {ex.Message}", ClientSession.UserId);
            }
        }

        private async Task<Packet> HandleFileDownloadCompleteRequest(Packet packet)
        {
            try
            {
                // Validate file ID
                if (!packet.Metadata.TryGetValue("FileId", out string fileId) || fileId != _fileMetadata.Id)
                {
                    _logService.Warning($"File ID mismatch in download complete request: {fileId} vs {_fileMetadata.Id}");
                    return _packetFactory.CreateFileDownloadCompleteResponse(false, _fileMetadata.Id, "File ID mismatch.", ClientSession.UserId);
                }
                
                // Calculate transfer statistics
                TimeSpan transferTime = DateTime.Now - _transferStartTime;
                double transferSpeedMBps = _fileMetadata.FileSize / (1024.0 * 1024.0) / transferTime.TotalSeconds;
                
                _logService.Info($"File download complete: {_fileMetadata.FileName} ({_fileMetadata.FileSize} bytes) in {transferTime.TotalSeconds:F1} seconds ({transferSpeedMBps:F2} MB/s)");
                
                // Transition back to authenticated state
                ClientSession.TransitionToState(ClientSession.StateFactory.CreateAuthenticatedState(ClientSession));
                
                return _packetFactory.CreateFileDownloadCompleteResponse(true, _fileMetadata.Id, "File download completed successfully.", ClientSession.UserId);
            }
            catch (Exception ex)
            {
                _logService.Error($"Error finalizing file download: {ex.Message}", ex);
                
                // Transition back to authenticated state on error
                ClientSession.TransitionToState(ClientSession.StateFactory.CreateAuthenticatedState(ClientSession));
                
                return _packetFactory.CreateFileDownloadCompleteResponse(false, _fileMetadata.Id, $"Error finalizing file download: {ex.Message}", ClientSession.UserId);
            }
        }

        public Task OnEnter()
        {
            _transferStartTime = DateTime.Now;
            _currentChunkIndex = 0;
            
            _logService.Debug($"Session {ClientSession.SessionId} entered TransferState for {_transferType} of file {_fileMetadata.FileName} (ID: {_fileMetadata.Id})");
            _logService.Info($"Starting file {_transferType}: {_fileMetadata.FileName} ({_fileMetadata.FileSize} bytes, {_totalChunks} chunks)");
            
            return Task.CompletedTask;
        }

        public Task OnExit()
        {
            _logService.Debug($"Session {ClientSession.SessionId} exited TransferState for {_transferType} of file {_fileMetadata.FileName} (ID: {_fileMetadata.Id})");
            return Task.CompletedTask;
        }
    }
}