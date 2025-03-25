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