using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FileLink.Client.Protocol
{
  
    // Factory for creating packets with specific command codes and payloads.
    // </summary>
    public class PacketFactory
    {
      
        //---------------------------
        // Authentication Commands
        //---------------------------

        // Creates an account creation request packet
        public Packet CreateAccountCreationRequest(string username, string password, string email = "")
        {
            var accountInfo = new { Username = username, Password = password, Email = email };
            var payload = JsonSerializer.SerializeToUtf8Bytes(accountInfo);

            return new Packet
            {
                CommandCode = Commands.CommandCode.CREATE_ACCOUNT_REQUEST,
                Payload = payload
            };
        }

      
        // Creates a login request packet
        public Packet CreateLoginRequest(string username, string password)
        {
            var credentials = new { Username = username, Password = password };
            var payload = JsonSerializer.SerializeToUtf8Bytes(credentials);

            return new Packet
            {
                CommandCode = Commands.CommandCode.LOGIN_REQUEST,
                Payload = payload
            };
        }

      
        // Creates a logout request packet
        public Packet CreateLogoutRequest(string userId)
        {
            return new Packet
            {
                CommandCode = Commands.CommandCode.LOGOUT_REQUEST,
                UserId = userId
            };
        }
        
        // Extracts login response data from packet
        public (bool Success, string Message, string UserId) ExtractLoginResponse(Packet packet)
        {
            if (packet.CommandCode != Commands.CommandCode.LOGIN_RESPONSE)
                return (false, "Invalid packet type", "");

            if (packet.Payload == null || packet.Payload.Length == 0)
                return (false, "Empty response payload", "");

            try
            {
                var responseData = JsonSerializer.Deserialize<LoginResponse>(packet.Payload);
                return (responseData.Success, responseData.Message, packet.UserId);
            }
            catch
            {
                return (false, "Error parsing response", "");
            }
        }
        
        // Extracts account creation response data from a packet
        public (bool Success, string Message, string UserId) ExtractAccountCreationResponse(Packet packet)
        {
            if (packet.CommandCode != Commands.CommandCode.CREATE_ACCOUNT_RESPONSE)
                return (false, "Invalid packet type", "");

            if (packet.Payload == null || packet.Payload.Length == 0)
                return (false, "Empty response payload", "");

            try
            {
                var responseData = JsonSerializer.Deserialize<AccountCreationResponse>(packet.Payload);
                return (responseData.Success, responseData.Message, responseData.UserId);
            }
            catch
            {
                return (false, "Error parsing response", "");
            }
        }
        
        // Extracts logout response data from a packet
        public (bool Success, string Message) ExtractLogoutResponse(Packet packet)
        {
            if (packet.CommandCode != Commands.CommandCode.LOGOUT_RESPONSE)
                return (false, "Invalid packet type");

            if (packet.Payload == null || packet.Payload.Length == 0)
                return (false, "Empty response payload");

            try
            {
                var responseData = JsonSerializer.Deserialize<LogoutResponse>(packet.Payload);
                return (responseData.Success, responseData.Message);
            }
            catch
            {
                return (false, "Error parsing response");
            }
        }
        
        // Response object classes for deserialization
        private class LoginResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
        }

        private class AccountCreationResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public string UserId { get; set; } = "";
        }

        private class LogoutResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
        }
        
        //---------------------------
        // File Commands
        //---------------------------
      
        // Creates a file list request packet
        public Packet CreateFileListRequest(string userId)
        {
            return new Packet
            {
                CommandCode = Commands.CommandCode.FILE_LIST_REQUEST,
                UserId = userId
            };
        }

      
        // Creates a file upload initialization request packet
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
                Payload = payload
            };

            packet.Metadata["FileName"] = fileName;
            packet.Metadata["FileSize"] = fileSize.ToString();
            packet.Metadata["ContentType"] = contentType;

            return packet;
        }

      
        // Creates a file upload chunk request packet
        public Packet CreateFileUploadChunkRequest(string userId, string fileId, int chunkIndex, bool isLastChunk, byte[] data)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST,
                UserId = userId,
                Payload = data
            };

            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();
            packet.Metadata["IsLastChunk"] = isLastChunk.ToString();

            return packet;
        }

      
        // Creates a file upload complete request packet
        public Packet CreateFileUploadCompleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

      
        // Creates a file download initialization request packet
        public Packet CreateFileDownloadInitRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

      
        // Creates a file download chunk request packet
        public Packet CreateFileDownloadChunkRequest(string userId, string fileId, int chunkIndex)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;
            packet.Metadata["ChunkIndex"] = chunkIndex.ToString();

            return packet;
        }

      
        // Creates a file download complete request packet
        public Packet CreateFileDownloadCompleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }

      
        // Creates a file delete request packet
        public Packet CreateFileDeleteRequest(string userId, string fileId)
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.FILE_DELETE_REQUEST,
                UserId = userId
            };

            packet.Metadata["FileId"] = fileId;

            return packet;
        }
    }
}