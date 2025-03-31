using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FileLink.Client.Connection;
using FileLink.Client.Protocol;
using FileLink.Client.Session;
//-------------------------------
// WE ARE REMOVING THIS FILE
//-------------------------------
namespace FileLink.Client.FileOperations
{
    // Manages file operations with the cloud file server
    public class FileManager
    {
        private readonly CloudServerConnection _connection;
        private readonly AuthenticationManager _authManager;
        private readonly PacketFactory _packetFactory;
        private readonly FileUploader _fileUploader;
        private readonly FileDownloader _fileDownloader;

   
        // Initializes a new instance of the FileManager class
       
        public FileManager(CloudServerConnection connection, AuthenticationManager authManager)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            _packetFactory = new PacketFactory();
            _fileUploader = new FileUploader(connection, authManager, _packetFactory);
            _fileDownloader = new FileDownloader(connection, authManager, _packetFactory);
        }

   
        // Gets the list of files from the server
        // 
        // Will be integrated with the new navigation method 
        //
        public async Task<List<FileMetadata>> GetFileListAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure authenticated
                await _authManager.EnsureAuthenticatedAsync(cancellationToken);

                // Ensure connected
                await _connection.EnsureConnectedAsync(cancellationToken);

                // Create and send the file list request
                var request = _packetFactory.CreateFileListRequest(_authManager.UserId);
                var response = await _connection.SendAndReceiveAsync(
                    request,
                    Commands.CommandCode.FILE_LIST_RESPONSE,
                    cancellationToken: cancellationToken);

                // Parse the response
                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return new List<FileMetadata>();
                }

                try
                {
                    var files = JsonSerializer.Deserialize<List<FileMetadata>>(response.Payload);
                    return files ?? new List<FileMetadata>();
                }
                catch (JsonException ex)
                {
                    throw new FileOperationException($"Error parsing file list response: {ex.Message}", ex);
                }
            }
            catch (Exception ex) when (!(ex is FileOperationException))
            {
                throw new FileOperationException($"Error getting file list: {ex.Message}", ex);
            }
        }

   
        // Deletes a file from the server
        public async Task<(bool Success, string Message)> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure authenticated
                await _authManager.EnsureAuthenticatedAsync(cancellationToken);

                // Ensure connected
                await _connection.EnsureConnectedAsync(cancellationToken);

                // Create and send the file delete request
                var request = _packetFactory.CreateFileDeleteRequest(_authManager.UserId, fileId);
                var response = await _connection.SendAndReceiveAsync(
                    request,
                    Commands.CommandCode.FILE_DELETE_RESPONSE,
                    cancellationToken: cancellationToken);

                // Check if the delete was successful
                bool success = response.IsSuccess();
                string message = response.GetMessage();

                return (success, message);
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting file: {ex.Message}");
            }
        }
        
        // Uploads a file to the server
        public Task<FileMetadata> UploadFileAsync(
            string filePath,
            string? contentType = null,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return _fileUploader.UploadFileAsync(filePath, contentType, progress, cancellationToken);
        }
        
        // Downloads a file from the server
        public Task<FileMetadata> DownloadFileAsync(
            string fileId,
            string destinationPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return _fileDownloader.DownloadFileAsync(fileId, destinationPath, progress, cancellationToken);
        }
    }

    // Exception thrown when there's an error during file operations
    public class FileOperationException : Exception
    {
        public FileOperationException(string message) : base(message) { }
        public FileOperationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
