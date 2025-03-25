using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FileLink.Client.Connection;
using FileLink.Client.Protocol;
using FileLink.Client.Session;

namespace FileLink.Client.FileOperations
{
    // Handles file upload operations to the cloud file server.
    public class FileUploader
    {
        private readonly CloudServerConnection _connection;
        private readonly AuthenticationManager _authManager;
        private readonly PacketFactory _packetFactory;
        private const int DefaultChunkSize = 1024 * 1024; // 1MB chunks by default
        
        // Initializes a new instance of the FileUploader class.
        public FileUploader(CloudServerConnection connection, AuthenticationManager authManager, PacketFactory packetFactory)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            _packetFactory = packetFactory ?? throw new ArgumentNullException(nameof(packetFactory));
        }

        // Uploads a file to the server
        public async Task<FileMetadata> UploadFileAsync(
            string filePath,
            string? contentType = null,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // Verify file exists
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }
            
            // Get file info
            var fileInfo = new FileInfo(filePath);
            string fileName = Path.GetFileName(filePath);
            long fileSize = fileInfo.Length;

            // Determine content type if not provided
            contentType ??= DetermineContentType(fileName);
            
            // Ensure authenticated
            await _authManager.EnsureAuthenticatedAsync(cancellationToken);

            // Ensure connected
            await _connection.EnsureConnectedAsync(cancellationToken);

            // Initialize upload
            string fileId = await InitializeUploadAsync(fileName, fileSize, contentType, cancellationToken);

            // Upload file chunks
            await UploadChunksAsync(fileId, filePath, fileSize, progress, cancellationToken);

            // Complete upload
            bool completed = await CompleteUploadAsync(fileId, cancellationToken);
            if (!completed)
            {
                throw new FileOperationException("Failed to complete file upload");
            }

            // Return file metadata
            return new FileMetadata
            {
                Id = fileId,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsComplete = true
            };
        }

        // Determines the content type based on the file extension
        private string DetermineContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "text/javascript",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                ".wav" => "audio/wav",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".tar" => "application/x-tar",
                _ => "application/octet-stream"
            };
        }
        
        // Initializes a file upload with the server
        private async Task<string> InitializeUploadAsync(
            string fileName,
            long fileSize,
            string contentType,
            CancellationToken cancellationToken)
        {
            // Create and send upload initialization request
            var request = _packetFactory.CreateFileUploadInitRequest(_authManager.UserId, fileName, fileSize, contentType);
            var response = await _connection.SendAndReceiveAsync(
                request,
                Commands.CommandCode.FILE_UPLOAD_INIT_RESPONSE,
                cancellationToken: cancellationToken);

            // Check for success and extract file ID
            if (!response.IsSuccess() || response.Payload == null)
            {
                throw new FileOperationException($"Failed to initialize file upload: {response.GetMessage()}");
            }

            try
            {
                var initResponse = JsonSerializer.Deserialize<FileUploadInitResponse>(response.Payload);
                if (initResponse?.FileId == null || string.IsNullOrEmpty(initResponse.FileId))
                {
                    throw new FileOperationException("Invalid file ID received from server");
                }

                return initResponse.FileId;
            }
            catch (JsonException ex)
            {
                throw new FileOperationException($"Error parsing upload initialization response: {ex.Message}", ex);
            }
        }

       
        // Uploads file chunks to the server
        private async Task UploadChunksAsync(
            string fileId,
            string filePath,
            long fileSize,
            IProgress<int>? progress,
            CancellationToken cancellationToken)
        {
            int totalChunks = (int)Math.Ceiling((double)fileSize / DefaultChunkSize);
            int currentChunk = 0;

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] buffer = new byte[DefaultChunkSize];

            while (currentChunk < totalChunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Read chunk from file
                int bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0)
                {
                    break; // End of file
                }

                // Check if this is the last chunk
                bool isLastChunk = (currentChunk == totalChunks - 1) || (bytesRead < buffer.Length);

                // Resize buffer if necessary
                byte[] chunkData = bytesRead < buffer.Length
                    ? buffer.AsSpan(0, bytesRead).ToArray()
                    : buffer;

                // Create and send chunk upload request
                var request = _packetFactory.CreateFileUploadChunkRequest(
                    _authManager.UserId, fileId, currentChunk, isLastChunk, chunkData);

                var response = await _connection.SendAndReceiveAsync(
                    request,
                    Commands.CommandCode.FILE_UPLOAD_CHUNK_RESPONSE,
                    cancellationToken: cancellationToken);

                // Check for success
                if (!response.IsSuccess())
                {
                    throw new FileOperationException($"Failed to upload chunk {currentChunk}: {response.GetMessage()}");
                }

                // Update progress
                currentChunk++;
                int progressPercentage = (int)((double)currentChunk / totalChunks * 100);
                progress?.Report(progressPercentage);
            }
        }

       
        // Completes a file upload with the server
        private async Task<bool> CompleteUploadAsync(string fileId, CancellationToken cancellationToken)
        {
            // Create and send upload completion request
            var request = _packetFactory.CreateFileUploadCompleteRequest(_authManager.UserId, fileId);
            var response = await _connection.SendAndReceiveAsync(
                request,
                Commands.CommandCode.FILE_UPLOAD_COMPLETE_RESPONSE,
                cancellationToken: cancellationToken);

            // Check for success
            return response.IsSuccess();
        }
    }
}