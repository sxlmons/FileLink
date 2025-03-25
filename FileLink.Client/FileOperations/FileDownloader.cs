using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FileLink.Client.Connection;
using FileLink.Client.FileOperations;
using FileLink.Client.Protocol;
using FileLink.Client.Session;

namespace FileLink.Client.FileOperations
{
    // Handles file download operations from the cloud file server
    public class FileDownloader
    {
        private readonly CloudServerConnection _connection;
        private readonly AuthenticationManager _authManager;
        private readonly PacketFactory _packetFactory;

        // Initializes a new instance of the FileDownloader class.
        public FileDownloader(CloudServerConnection connection, AuthenticationManager authManager, PacketFactory packetFactory)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            _packetFactory = packetFactory ?? throw new ArgumentNullException(nameof(packetFactory));
        }
        
        // Downloads a file from the server
        public async Task<FileMetadata> DownloadFileAsync(
            string fileId,
            string destinationPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // Ensure authenticated
            await _authManager.EnsureAuthenticatedAsync(cancellationToken);

            // Ensure connected
            await _connection.EnsureConnectedAsync(cancellationToken);

            // Initialize download
            var fileMetadata = await InitializeDownloadAsync(fileId, cancellationToken);

            // Prepare destination directory
            string? destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // Download chunks
            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await DownloadChunksAsync(fileId, fileMetadata.TotalChunks, fileStream, progress, cancellationToken);
            }

            // Complete download
            bool completed = await CompleteDownloadAsync(fileId, cancellationToken);
            if (!completed)
            {
                throw new FileOperationException("Failed to complete file download");
            }

            return fileMetadata;
        }
        
        // Initializes a file download with the server
        private async Task<FileMetadata> InitializeDownloadAsync(
            string fileId,
            CancellationToken cancellationToken)
        {
            // Create and send download initialization request
            var request = _packetFactory.CreateFileDownloadInitRequest(_authManager.UserId, fileId);
            var response = await _connection.SendAndReceiveAsync(
                request,
                Commands.CommandCode.FILE_DOWNLOAD_INIT_RESPONSE,
                cancellationToken: cancellationToken);

            // Check for success and extract file metadata
            if (!response.IsSuccess() || response.Payload == null)
            {
                throw new FileOperationException($"Failed to initialize file download: {response.GetMessage()}");
            }

            try
            {
                var initResponse = JsonSerializer.Deserialize<FileDownloadInitResponse>(response.Payload);
                if (initResponse == null || string.IsNullOrEmpty(initResponse.FileId))
                {
                    throw new FileOperationException("Invalid file metadata received from server");
                }

                // Convert the response to a FileMetadata object
                return new FileMetadata
                {
                    Id = initResponse.FileId,
                    FileName = initResponse.FileName,
                    FileSize = initResponse.FileSize,
                    ContentType = initResponse.ContentType,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsComplete = true,
                    // Store the total chunks as an extension property
                    TotalChunks = initResponse.TotalChunks
                };
            }
            catch (JsonException ex)
            {
                throw new FileOperationException($"Error parsing download initialization response: {ex.Message}", ex);
            }
        }
        
        // Downloads file chunks from the server
        private async Task DownloadChunksAsync(
            string fileId,
            int totalChunks,
            FileStream fileStream,
            IProgress<int>? progress,
            CancellationToken cancellationToken)
        {
            for (int currentChunk = 0; currentChunk < totalChunks; currentChunk++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Create and send chunk download request
                var request = _packetFactory.CreateFileDownloadChunkRequest(_authManager.UserId, fileId, currentChunk);
                var response = await _connection.SendAndReceiveAsync(
                    request,
                    Commands.CommandCode.FILE_DOWNLOAD_CHUNK_RESPONSE,
                    cancellationToken: cancellationToken);

                // Check for success
                if (!response.IsSuccess() || response.Payload == null)
                {
                    throw new FileOperationException($"Failed to download chunk {currentChunk}: {response.GetMessage()}");
                }

                // Write chunk to file
                await fileStream.WriteAsync(response.Payload, 0, response.Payload.Length, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);

                // Update progress
                int progressPercentage = (int)((double)(currentChunk + 1) / totalChunks * 100);
                progress?.Report(progressPercentage);

                // Check if this is the last chunk
                if (response.Metadata.TryGetValue("IsLastChunk", out string? isLastChunkStr) &&
                    bool.TryParse(isLastChunkStr, out bool isLastChunk) &&
                    isLastChunk)
                {
                    break;
                }
            }
        }
        
        // Completes a file download with the server
        private async Task<bool> CompleteDownloadAsync(string fileId, CancellationToken cancellationToken)
        {
            // Create and send download completion request
            var request = _packetFactory.CreateFileDownloadCompleteRequest(_authManager.UserId, fileId);
            var response = await _connection.SendAndReceiveAsync(
                request,
                Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_RESPONSE,
                cancellationToken: cancellationToken);

            // Check for success
            return response.IsSuccess();
        }
    }
}