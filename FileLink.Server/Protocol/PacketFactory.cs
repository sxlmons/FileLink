//-----------------------------------------------------------------------------
// Copyright (c) 2025 FileLink Project. All rights reserved.
// Licensed under the MIT License.
//-----------------------------------------------------------------------------
//
// PacketFactory.cs
//
// This factory provides a clean interface for creating properly formatted
// packets throughout the FileLink application. It abstracts away the details of
// packet construction, ensuring that all packets follow the same structure and
// conventions regardless of where they're created in the codebase. Each method creates
// a specific type of packet (request or response) with appropriate command codes,
// serialized payloads, and metadata. This approach to packet creation
// makes protocol changes easy, improves maintainability, and reduces the risk of
// inconsistencies in the communication protocol. Methods are organized by functionality
// (authentication, file operations, etc.) and follow a consistent naming pattern.
//-----------------------------------------------------------------------------

using System.Text.Json;

namespace FileLink.Server.Protocol;

// Factory for creating packets with specific command codes and payloads.
// Implements the Factory pattern to create different types of packets.
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

    // [SERVER METHOD] Creates an account creation response packet
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

    // [CLIENT] Create a login request packet
    public Packet CreateLoginRequest(string username, string password)
    {
        var credentials = new { username = username, password = password };
        var payload = JsonSerializer.SerializeToUtf8Bytes(credentials);

        return new Packet
        {
            CommandCode = Commands.CommandCode.LOGIN_REQUEST,
            Payload = payload
        };
    }

    // [SERVER] Create a login response packet
    public Packet CreateLoginResponse(bool success, string message, string userId = "")
    {
        var response = new
        {
            Success = success,
            Message = message
        };
        
        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.LOGIN_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString()
            }
        };

        return packet;
    }

    // [CLIENT] Create logout request packet
    public Packet CreateLogoutRequest(string userId)
    {
        return new Packet
        {
            CommandCode = Commands.CommandCode.LOGOUT_REQUEST,
            UserId = userId
        };
    }

    // [SERVER] Create logout response packet
    public Packet CreateLogoutResponse(bool success, string message)
    {
        // Create an anonymous object with the success flag and message to be serialized into the payload.
        // This structured data will be deserialized by the client to understand the logout result.
        var response = new
        {
            Success = success,
            Message = message
        };
        
        // Serialize the response object to a UTF-8 encoded byte array that will become the packet payload.
        // This transforms our C# object into a binary format that can be transmitted over the network.
        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.LOGOUT_RESPONSE,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString()
            }
        };

        return packet;
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
    