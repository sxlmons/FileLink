using System.Text.Json;
using FileLink.Client.Models;
using FileLink.Client.Protocol;

namespace FileLink.Client.Services;

public class FileService
{
    private readonly NetworkService _networkService;
    private readonly PacketFactory _packetFactory = new PacketFactory();
    private const int ChunkSize = 1024 * 1024; // 1 MB chunks
    
    // Initializes a new instance of the FileService class
    public FileService(NetworkService networkService)
    {
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
    }
    
    
    // UPLOAD ASYNC ALL PUT TOGETHER
    public async Task<FileItem?> UploadFileAsync(string filePath, string? directoryId, string userId, Action<int, int>? progressCallback = null)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var fileInfo = new FileInfo(filePath);
            string fileName = Path.GetFileName(filePath);
            long fileSize = fileInfo.Length;
            string contentType = GetContentType(fileName);

            // Step 1: Initialize upload
            string? fileId = await InitializeUploadAsync(fileName, fileSize, contentType, directoryId, userId);
            
            if (string.IsNullOrEmpty(fileId))
                return null;

            // Step 2: Upload file chunks
            int totalChunks = (int)Math.Ceiling((double)fileSize / ChunkSize);
            int currentChunk = 0;

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[ChunkSize];
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] chunk = bytesRead < buffer.Length
                        ? buffer.Take(bytesRead).ToArray()
                        : buffer;

                    bool isLastChunk = currentChunk == totalChunks - 1;
                    
                    bool success = await UploadChunkAsync(fileId, chunk, currentChunk, isLastChunk, userId);
                    
                    if (!success)
                        return null;

                    currentChunk++;
                    progressCallback?.Invoke(currentChunk, totalChunks);
                }
            }

            // Step 3: Complete the upload
            bool completed = await CompleteUploadAsync(fileId, userId);
            
            if (!completed)
                return null;

            // Return file metadata
            return new FileItem
            {
                Id = fileId,
                FileName = fileName,
                DirectoryId = directoryId,
                FileSize = fileSize,
                ContentType = contentType,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
            return null;
        }
    }
    
    
        // UPLOAD INIT
        private async Task<string?> InitializeUploadAsync(string fileName, long fileSize, string contentType, string? directoryId, string userId)
        {
            try
            {
                // Create the file upload initialization info
                var uploadInfo = new
                {
                    FileName = fileName,
                    FileSize = fileSize,
                    ContentType = contentType
                };

                // Serialize to JSON
                var payload = JsonSerializer.SerializeToUtf8Bytes(uploadInfo);

                // Create the upload initialization request packet
                var packet = new Packet(Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST)
                {
                    UserId = userId,
                    Payload = payload
                };

                // Add directory ID to metadata if specified
                if (!string.IsNullOrEmpty(directoryId))
                    packet.Metadata["DirectoryId"] = directoryId;
                
                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                    return null;
                if (response.Payload == null || response.Payload.Length == 0) 
                    return null;
                

                // Deserialize the response
                var initResponse = JsonSerializer.Deserialize<UploadInitResponse>(response.Payload);

                if (initResponse == null || !initResponse.Success)
                    return null;
                
                return initResponse.FileId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing upload: {ex.Message}");
                return null;
            }
        }
        
        
        // UPLOAD CHUNK
        private async Task<bool> UploadChunkAsync(string fileId, byte[] chunkData, int chunkIndex, bool isLastChunk, string userId)
        {
            try
            {
                // Create the chunk upload request packet
                var packet = new Packet(Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST)
                {
                    UserId = userId,
                    Payload = chunkData
                };

                packet.Metadata["FileId"] = fileId;
                packet.Metadata["ChunkIndex"] = chunkIndex.ToString();
                packet.Metadata["IsLastChunk"] = isLastChunk.ToString();

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                    return false;
                if (response.Payload == null || response.Payload.Length == 0)
                    return false;

                // Deserialize the response
                var chunkResponse = JsonSerializer.Deserialize<ChunkResponse>(response.Payload);

                return chunkResponse?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading chunk {chunkIndex}: {ex.Message}");
                return false;
            }
        }
        
        // UPLOAD COMPLETE
        private async Task<bool> CompleteUploadAsync(string fileId, string userId)
        {
            try
            {
                // Create the upload complete request packet
                var packet = new Packet(Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST)
                {
                    UserId = userId
                };

                packet.Metadata["FileId"] = fileId;

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                    return false;
                if (response.Payload == null || response.Payload.Length == 0)
                    return false;

                // Deserialize the response
                var completeResponse = JsonSerializer.Deserialize<UploadCompleteResponse>(response.Payload);

                return completeResponse?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing upload: {ex.Message}");
                return false;
            }
        }

        
        // ... Download functionality ...
        
        public async Task<(bool Success, string FilePath)> DownloadFileAsync(string fileId, string? destinationPath, string userId, Action<int, int>? progressCallback = null)
        {
            try
            {
                // Step 1: Initialize download
                var initResponse = await InitializeDownloadAsync(fileId, userId);
                
                if (initResponse == null)
                    return (false, string.Empty);

                string fileName = initResponse.FileName;
                long fileSize = initResponse.FileSize;
                int totalChunks = initResponse.TotalChunks;

                // If no specific path is provided, use a default downloads folder
                if (string.IsNullOrEmpty(destinationPath))
                {
                    string downloadsFolder = Path.Combine(FileSystem.CacheDirectory, "Downloads");
                    destinationPath = Path.Combine(downloadsFolder, fileName);
                }

                // Create destination directory if it doesn't exist
                string? directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Step 2: Download file chunks
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                    {
                        var chunk = await DownloadChunkAsync(fileId, chunkIndex, userId);
                        
                        if (chunk == null)
                            return (false, string.Empty);

                        await fileStream.WriteAsync(chunk, 0, chunk.Length);
                        progressCallback?.Invoke(chunkIndex + 1, totalChunks);
                    }
                }

                // Step 3: Complete the download
                bool completed = await CompleteDownloadAsync(fileId, userId);
                
                return (completed, destinationPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return (false, string.Empty);
            }
        }
        
        private async Task<DownloadInitResponse?> InitializeDownloadAsync(string fileId, string userId)
        {
            try
            {
                // Create the download initialization request packet
                var packet = new Packet(Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST)
                {
                    UserId = userId
                };

                packet.Metadata["FileId"] = fileId;

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return null;
                }

                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return null;
                }

                // Deserialize the response
                var initResponse = JsonSerializer.Deserialize<DownloadInitResponse>(response.Payload);

                if (initResponse == null || !initResponse.Success)
                {
                    return null;
                }

                return initResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing download: {ex.Message}");
                return null;
            }
        }

        private async Task<byte[]?> DownloadChunkAsync(string fileId, int chunkIndex, string userId)
        {
            try
            {
                // Create the chunk download request packet
                var packet = new Packet(Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST)
                {
                    UserId = userId
                };

                packet.Metadata["FileId"] = fileId;
                packet.Metadata["ChunkIndex"] = chunkIndex.ToString();

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return null;
                }

                // Extract success flag and message from metadata
                if (response.Metadata.TryGetValue("Success", out string? successString) &&
                    bool.TryParse(successString, out bool success) &&
                    !success)
                {
                    return null;
                }

                // For chunk responses, the payload is the binary chunk data
                return response.Payload;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading chunk {chunkIndex}: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> CompleteDownloadAsync(string fileId, string userId)
        {
            try
            {
                // Create the download complete request packet
                var packet = new Packet(Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_REQUEST)
                {
                    UserId = userId
                };

                packet.Metadata["FileId"] = fileId;

                // Send the packet and get the response
                var response = await _networkService.SendAndReceiveAsync(packet);

                if (response == null || response.CommandCode == Commands.CommandCode.ERROR)
                {
                    return false;
                }

                if (response.Payload == null || response.Payload.Length == 0)
                {
                    return false;
                }

                // Deserialize the response
                var completeResponse = JsonSerializer.Deserialize<DownloadCompleteResponse>(response.Payload);

                return completeResponse?.Success ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing download: {ex.Message}");
                return false;
            }
        }



        private string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".txt" => "text/plain",
                ".html" or ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".pdf" => "application/pdf",
                ".doc" or ".docx" => "application/msword",
                ".xls" or ".xlsx" => "application/vnd.ms-excel",
                ".ppt" or ".pptx" => "application/vnd.ms-powerpoint",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".tar" => "application/x-tar",
                ".gif" => "image/gif",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                _ => "application/octet-stream",
            };
        }

        private class UploadInitResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; } = "";
            public string Message { get; set; } = "";
        }

        private class ChunkResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; } = "";
            public int ChunkIndex { get; set; }
            public string Message { get; set; } = "";
        }

        private class UploadCompleteResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; } = "";
            public string Message { get; set; } = "";
        }

        private class DownloadInitResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; } = "";
            public string FileName { get; set; } = "";
            public long FileSize { get; set; }
            public string ContentType { get; set; } = "";
            public int TotalChunks { get; set; }
            public string Message { get; set; } = "";
        }

        private class DownloadCompleteResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; } = "";
            public string Message { get; set; } = "";
        }

        private class DeleteFileResponse
        {
            public bool Success { get; set; }
            public string FileId { get; set; } = "";
            public string Message { get; set; } = "";
        }
}