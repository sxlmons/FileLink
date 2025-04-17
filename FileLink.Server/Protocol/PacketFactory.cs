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
//
// General data flow patterns:
//
// For Request Packets:
//   Client input → method params → payload object → serialized payload → packet → network
//   Structure: packet = { CommandCode: REQUEST_TYPE, UserId: string, Payload: byte[], Metadata: {key-value pairs for quick access} }
//
// For Response Packets:
//   Server input → method params → response object → serialized payload → packet → network
//
//   Structure: response = { Success: bool, Message: string, [Other data fields] }
//              packet   = { CommandCode: RESPONSE_TYPE, UserId: string, Payload: byte[], Metadata: {"Success": bool, [Other metadata]} }
//-----------------------------------------------------------------------------

using System.Text.Json;

namespace FileLink.Server.Protocol;

// Factory for creating packets with specific command codes and payloads.
// Implements the Factory pattern to create different types of packets.
// These methods will be used by the CommandHandler classes
public class PacketFactory
{
    // - Account Creation Packet Request/Response - Detailed protocol explanation provided
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
        // Outline the credential information
        var accountInfo = new { username = username, password = password, email = email };
        
        // [Applied across all method payloads]
        // Serialize the response object to a UTF-8 encoded byte array that will become the packet payload.
        // This transforms our C# object into a binary format that can be transmitted over the network.
        var payload = JsonSerializer.SerializeToUtf8Bytes(accountInfo);

        // [Applicable across all returned packets]
        // Construct a new Packet object with the appropriate command code and payload.
        // This packet will be transmitted to the Server for an account creation request.
        return new Packet
        {
            // Sets the command code to indicate this is an account
            CommandCode = Commands.CommandCode.CREATE_ACCOUNT_REQUEST,
            
            // Attaches the serialized response data as the packet's payload
            Payload = payload
        };
    }

    // [SERVER METHOD] Creates an account creation response packet
    // Data flow: 
    // Input → params(success, message, userId) → response object → serialized payload → packet → network
    //
    // Structure:
    // response = { Success: bool, Message: string, UserId: string }
    // Payload = UTF8 serialized JSON of response
    // Packet = { CommandCode: CREATE_ACCOUNT_RESPONSE, UserId: string, Payload: byte[], Metadata: {"Success": bool} }
    //
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
            Payload = payload
        };

        packet.Metadata["Success"] = success.ToString();
            
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
            Payload = payload
        };

        packet.Metadata["Success"] = success.ToString();
            
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
        var response = new 
        { 
            Success = success, 
            Message = message
        };
            
        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.LOGOUT_RESPONSE,
            Payload = payload
        };

        packet.Metadata["Success"] = success.ToString();
            
        return packet;
    }

    // Create file list request packet
    public Packet CreateFileListRequest(string userId)
    {
        return new Packet
        {
            CommandCode = Commands.CommandCode.FILE_LIST_REQUEST,
            UserId = userId
        };
    }

    // Creates a packet containing a list of files for responding to a file list request
    // Takes a collection of file metadata objects to be sent to the client and the userId.
    // returns a Packet object configured with FILE_LIST_RESPONSE command code,
    // the user's ID, and a JSON-serialized payload containing file data
    public Packet CreateFileListResponse(IEnumerable<object> files, string userId)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(files);

        return new Packet
        {
            CommandCode = Commands.CommandCode.FILE_LIST_RESPONSE,
            UserId = userId,
            Payload = payload
        };
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
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST,
            UserId = userId,
            Metadata =
            {
                ["FileId"] = fileId
            }
        };

        return packet;
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
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST,
            UserId = userId,
            Metadata =
            {
                ["FileId"] = fileId,
                ["ChunkIndex"] = chunkIndex.ToString()
            }
        };

        return packet;
    }

    // Create a file download chunk response packet
    public Packet CreateFileDownloadChunkResponse(bool success, string fileId, int chunkIndex, bool isLastChunk, byte[] data, string message, string userId)
    {
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_DOWNLOAD_CHUNK_RESPONSE,
            UserId = userId,
            Payload = data,
            Metadata =
            {
                ["Success"] = success.ToString(),
                ["FileId"] = fileId,
                ["ChunkIndex"] = chunkIndex.ToString(),
                ["IsLastChunk"] = isLastChunk.ToString(),
                ["Message"] = message
            }
        };

        return packet;
    }

    // Create a file download complete request packet
    public Packet CreateFileDownloadCompleteRequest(string userId, string fileId)
    {
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_REQUEST,
            UserId = userId,
            Metadata =
            {
                ["FileId"] = fileId
            }
        };

        return packet;
    }

    // Create a file download complete response packet
    public Packet CreateFileDownloadCompleteResponse(bool success, string fileId, string message, string userId)
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
            CommandCode = Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_RESPONSE,
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

    // Create a file delete request packet
    public Packet CreateFileDeleteRequest(string userId, string fileId)
    {
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_DELETE_REQUEST,
            UserId = userId,
            Metadata =
            {
                ["FileId"] = fileId
            }
        };

        return packet;
    }

    // Create a file delete response packet
    public Packet CreateFileDeleteResponse(bool success, string fileId, string message, string userId)
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
            CommandCode = Commands.CommandCode.FILE_DELETE_RESPONSE,
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

    // Create an Error response packet
    public Packet CreateErrorResponse(int originalCommandCode, string message, string userId = "")
    {
        var response = new
        {
            OriginalCommandCode = originalCommandCode,
            Message = message
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.ERROR,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["OriginalCommandCode"] = originalCommandCode.ToString(),
                ["OriginalCommandName"] = Commands.CommandCode.GetCommandName(originalCommandCode)
            }
        };
        return packet;
    }
    
    //------------------------------------------
    // NEW DIRECTORY FACTORY METHODS EXTENSION - 
    //------------------------------------------
    
    // Creates a directory creation request packet
    public Packet CreateDirectoryCreateRequest(string userId, string directoryName, string parentDirectoryId = null)
    {
        var directoryInfo = new
        {
            DirectoryName = directoryName,
            ParentDirectoryId = parentDirectoryId
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(directoryInfo);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CREATE_REQUEST,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["DirectoryName"] = directoryName
            }
        };

        if (!string.IsNullOrEmpty(parentDirectoryId))
        {
            packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
        }

        return packet;
    }
    
    // Creates a directory response packet
    public Packet CreateDirectoryCreateResponse(bool success, string directoryId, string directoryName, string message, string userId)
    {
        var response = new
        {
            Success = success,
            DirectoryId = directoryId,
            DirectoryName = directoryName,
            Message = message
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CREATE_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString(),
                ["DirectoryId"] = directoryId,
                ["DirectoryName"] = directoryName
            }
        };

        return packet;
    }
    
    // Create a directory list request
    public Packet CreateDirectoryListRequest(string userId, string parentDirectoryId = null)
    {
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_LIST_REQUEST,
            UserId = userId
        };

        if (!string.IsNullOrEmpty(parentDirectoryId))
        {
            packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
        }

        return packet;
    }
    
    // Create a directory response packet 
    public Packet CreateDirectoryListResponse(IEnumerable<object> directories, string parentDirectoryId, string userId)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(directories);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_LIST_RESPONSE,
            UserId = userId,
            Payload = payload
        };

        if (!string.IsNullOrEmpty(parentDirectoryId))
        {
            packet.Metadata["ParentDirectoryId"] = parentDirectoryId;
        }

        packet.Metadata["Count"] = directories is ICollection<object> collection ? collection.Count.ToString() : "unknown";

        return packet;
    }
    
    // Create a directory rename request
    public Packet CreateDirectoryRenameRequest(string userId, string directoryId, string newName)
    {
        var renameInfo = new
        {
            DirectoryId = directoryId,
            NewName = newName
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(renameInfo);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_RENAME_REQUEST,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["DirectoryId"] = directoryId,
                ["NewName"] = newName
            }
        };

        return packet;
    }
    
    // Creates a directory rename response packet
    public Packet CreateDirectoryRenameResponse(bool success, string directoryId, string newName, string message, string userId)
    {
        var response = new
        {
            Success = success,
            DirectoryId = directoryId,
            NewName = newName,
            Message = message
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_RENAME_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString(),
                ["DirectoryId"] = directoryId,
                ["NewName"] = newName
            }
        };

        return packet;
    }
    
    // Create a directory delete request packet
    public Packet CreateDirectoryDeleteRequest(string userId, string directoryId, bool recursive)
    {
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_DELETE_REQUEST,
            UserId = userId,
            Metadata =
            {
                ["DirectoryId"] = directoryId,
                ["Recursive"] = recursive.ToString()
            }
        };

        return packet;
    }
    
    // Create a directory delete response 
    public Packet CreateDirectoryDeleteResponse(bool success, string directoryId, string message, string userId)
    {
        var response = new
        {
            Success = success,
            DirectoryId = directoryId,
            Message = message
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_DELETE_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["Success"] = success.ToString(),
                ["DirectoryId"] = directoryId
            }
        };

        return packet;
    }
    
    // Create a directory move request packet
    public Packet CreateFileMoveRequest(string userId, IEnumerable<string> fileIds, string targetDirectoryId)
    {
        var moveInfo = new
        {
            FileIds = fileIds,
            TargetDirectoryId = targetDirectoryId
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(moveInfo);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_MOVE_REQUEST,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["FileCount"] = fileIds is ICollection<string> collection ? collection.Count.ToString() : "unknown"
            }
        };

        if (!string.IsNullOrEmpty(targetDirectoryId))
        {
            packet.Metadata["TargetDirectoryId"] = targetDirectoryId;
        }
        else
        {
            packet.Metadata["TargetDirectoryId"] = "root";
        }

        return packet;
    }
    
    // Creates a file move response packet
    public Packet CreateFileMoveResponse(bool success, int fileCount, string targetDirectoryId, string message, string userId)
    {
        var response = new
        {
            Success = success,
            FileCount = fileCount,
            TargetDirectoryId = targetDirectoryId,
            Message = message
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_MOVE_RESPONSE,
            UserId = userId,
            Payload = payload
        };

        packet.Metadata["Success"] = success.ToString();
        packet.Metadata["FileCount"] = fileCount.ToString();
        if (!string.IsNullOrEmpty(targetDirectoryId))
        {
            packet.Metadata["TargetDirectoryId"] = targetDirectoryId;
        }
        else
        {
            packet.Metadata["TargetDirectoryId"] = "root";
        }

        return packet;
    }
    
    // Creates a directory contents request packet
    public Packet CreateDirectoryContentsRequest(string userId, string directoryId = null)
    {
        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_REQUEST,
            UserId = userId
        };

        if (!string.IsNullOrEmpty(directoryId))
        {
            packet.Metadata["DirectoryId"] = directoryId;
        }
        else
        {
            packet.Metadata["DirectoryId"] = "root";
        }

        return packet;
    }
    
    // Creates a directory contents response
    public Packet CreateDirectoryContentsResponse(IEnumerable<object> files, IEnumerable<object> directories, string directoryId, string userId)
    {
        var contentsInfo = new
        {
            Files = files,
            Directories = directories,
            DirectoryId = directoryId
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(contentsInfo);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_RESPONSE,
            UserId = userId,
            Payload = payload,
            Metadata =
            {
                ["FileCount"] = files is ICollection<object> fileCollection ? fileCollection.Count.ToString() : "unknown",
                ["DirectoryCount"] = directories is ICollection<object> dirCollection ? dirCollection.Count.ToString() : "unknown"
            }
        };

        if (!string.IsNullOrEmpty(directoryId))
        {
            packet.Metadata["DirectoryId"] = directoryId;
        }
        else
        {
            packet.Metadata["DirectoryId"] = "root";
        }

        return packet;
    }
    
    // [CLIENT] Create an update user names request packet
    public Packet CreateUpdateUserNamesRequest(string userId, string firstName, string lastName)
    {
        var userInfo = new 
        { 
            FirstName = firstName, 
            LastName = lastName 
        };
        
        var payload = JsonSerializer.SerializeToUtf8Bytes(userInfo);

        return new Packet
        {
            CommandCode = Commands.CommandCode.UPDATE_USER_NAMES_REQUEST,
            UserId = userId,
            Payload = payload
        };
    }

    // [SERVER] Create an update user names response packet
    public Packet CreateUpdateUserNamesResponse(bool success, string message)
    {
        var response = new 
        { 
            Success = success, 
            Message = message
        };
            
        var payload = JsonSerializer.SerializeToUtf8Bytes(response);

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.UPDATE_USER_NAMES_RESPONSE,
            Payload = payload
        };

        packet.Metadata["Success"] = success.ToString();
            
        return packet;
    }
}
    