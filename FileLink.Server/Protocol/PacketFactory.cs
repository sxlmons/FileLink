using System.Text.Json;

namespace FileLink.Server.Protocol;

// Factory for producing packets yo
// These methods will be used by the CommandHandler classes
public class PacketFactory
{
    // - Account Creation Packet Request/Response
    // - Login Request/Response
    // - Logout Request/Response
    // - List Files Request/Response

    // - File Upload Init Request/Response
    // - File Upload Chunk Request/Response
    // - File Upload Complete Request/Response

    // - File Download Init Request/Response
    // - File Download Chunk Request/Response
    // - File Download Complete Request/Response

    // - File Delete Request/Response
    // - Error Response Packet

    // [CLIENT METHOD]: Creates an account creation request packet
    public Packet CreateAccountCreationRequest(string username, string password, string email)
    {
        var accountInfo = new { username = username, password = password, email = email };
        var payload = JsonSerializer.SerializeToUtf8Bytes(accountInfo);

        return new Packet
        {
            CommandCode = Commands.CommandCode.CREATE_ACCOUNT_REQUEST,
            Payload = payload
        };
    }

    // Creates an account creation response packet
    public Packet CreateAccountCreationResponse(bool success, string message, string userId = "")
    {
        var response = new
        {
            Success = success, 
            Message = message, 
            UserId = userId
        };
        
        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.CREATE_ACCOUNT_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString()
            }
        };

        return packet;

    }

    // Create a login request packet
    public Packet CreateLoginRequest(string username, string password)
    {
        throw new NotImplementedException();
    }

    // Create a login response packet
    public Packet CreateLoginResponse(bool success, string message, string userId = "")
    {
        throw new NotImplementedException();
    }

    // Create logout request packet
    public Packet CreateLogoutRequest(string userId)
    {
        throw new NotImplementedException();
    }

    // Create logout response packet
    public Packet CreateLogoutResponse(bool success, string message)
    {
        throw new NotImplementedException();
    }

    // Create file list request packet
    public Packet CreateFileListRequest(string userId)
    {
        throw new NotImplementedException();
    }

    // Create file list response packet
    public Packet CreateFileListResponse(IEnumerable<object> files, string userId)
    {
        throw new NotImplementedException();
    }

    // Create a file upload initialization request packet
    public Packet CreateFileUploadInitRequest(string userId, string fileName, long fileSize, string contentType)
    {
        var initData = new
        {
            FileName = fileName,
            FileSize = fileSize,
            ContentType = contentType
        };
        
        var payload = JsonSerializer.SerializeToUtf8Bytes(initData);
        
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["FileName"] = fileName,
                ["FileSize"] = fileSize.ToString(),
                ["ContentType"] = contentType
            }
        };
        return packet;
    }

    // Create a file upload initialization response packet
    public Packet CreateFileUploadInitResponse(bool success, string fileId, string message, string userId)
    {
        var response = new
        {
            Success = success,
            FileId = fileId,
            Message = message
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString(),
                ["FileId"] = fileId
            }
        };
        return packet;
    }

    // Create a file upload chunk request packet
    public Packet CreateFileUploadChunkRequest(string userId, string fileId, int chunkIndex, bool isLastChunk, byte[] data)
    {
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST,
            UserId = userId,
            Payload = data,
            Metadata =
            {
                ["FileId"] = fileId,
                ["ChunkIndex"] = chunkIndex.ToString(),
                ["IsLastChunk"] = isLastChunk.ToString()
            }
        };
        return packet;
    }

    // Create a file upload chunk response packet
    public Packet CreateFileUploadChunkResponse(bool success, string fileId, int chunkIndex, string message, string userId)
    {
        var response = new
        {
            Success = success,
            FileId = fileId,
            ChunkIndex = chunkIndex,
            Message = message
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString(),
                ["FileId"] = fileId,
                ["ChunkIndex"] = chunkIndex.ToString()
            }
        };

        return packet;
    }

    // Create a file upload complete request packet
    public Packet CreateFileUploadCompleteRequest(string userId, string fileId)
    {
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST,
            UserId = userId,
            Metadata =
            {
                ["FileId"] = fileId
            }
        };

        return packet;    }

    // Create a file upload complete response packet
    public Packet CreateFileUploadCompleteResponse(bool success, string fileId, string message, string userId)
    {
        var response = new
        {
            Success = success,
            FileId = fileId,
            Message = message
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString(),
                ["FileId"] = fileId
            }
        };
        return packet;
    }

    // Create a file download init request packet
    public Packet CreateFileDownloadInitRequest(string userId, string fileId)
    {
        throw new NotImplementedException();
    }

    // Create a file download init response packet
    public Packet CreateFileDownloadInitResponse(bool success, string fileId, string fileName, 
            long fileSize, string contentType, int totalChunks, string message, string userId)
        {
            var response = new
            {
                Success = success,
                FileId = fileId,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                TotalChunks = totalChunks,
                Message = message
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(response);

            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_INIT_RESPONSE,
                UserId = userId,
                Payload = payload,
                Metadata =
                {
                    ["Success"] = success.ToString(),
                    ["FileId"] = fileId,
                    ["FileName"] = fileName,
                    ["FileSize"] = fileSize.ToString(),
                    ["ContentType"] = contentType,
                    ["TotalChunks"] = totalChunks.ToString()
                }
            };

            return packet;
        }

    // Create a file download chunk request packet
    public Packet CreateFileDownloadChunkRequest(string userId, string fileId, int chunkIndex)
    {
        throw new NotImplementedException();
    }

    // Create a file download chunk response packet
    public Packet CreateFileDownloadChunkResponse(bool success, string fileId, int chunkIndex, bool isLastChunk,
        byte[] data, string message, string userId)
    {
        throw new NotImplementedException();
    }

    // Create a file download complete request packet
    public Packet CreateFileDownloadCompleteRequest(string userId, string fileId)
    {
        throw new NotImplementedException();
    }

    // Create a file download complete response packet
    public Packet CreateFileDownloadCompleteResponse(bool success, string fileId, string message, string userId)
    {
        throw new NotImplementedException();
    }

    // Create a file delete request packet
    public Packet CreateFileDeleteRequest(string userId, string fileId)
    {
        throw new NotImplementedException();
    }

    // Create a file delete response packet
    public Packet CreateFileDeleteResponse(bool success, string fileId, string message, string userId)
    {
        throw new NotImplementedException();
    }

    // Create an Error response packet
    public Packet CreateErrorResponse(int originalCommandCode, string message, string userId = "")
    {
        throw new NotImplementedException();
    }
}
    