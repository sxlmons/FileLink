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
    
    // Creates an account creation request packet
    public Packet CreateAccountCreationRequest(string username, string password, string email)
    {
        throw new NotImplementedException();        
    }
    
    // Creates an account creation response packet
    public Packet CreateAccountCreationResponse(bool success, string message, string userId = "")
    {
        throw new NotImplementedException();
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
    public Packet CreateFileUploadRequest(string userId, string fileName, long fileSize, string contentType)
    {
        throw new NotImplementedException();
    }
    
    // Create a file upload initialization response packet
    public Packet CreateFileUploadInitResponse(bool success, string fileId, string message, string userId)
    {
        throw new NotImplementedException();
    }

    // Create a file upload chunk request packet
    public Packet CreateFileUploadChunkRequest(string userId, string fileId, int chunkIndex, bool isLastChunk, byte[] data)
    {
        throw new NotImplementedException();
    }

    // Create a file upload chunk response packet
    public Packet CreateFileUploadChunkResponse(bool success, string fileId, int chunkIndex, string message, string userId)
    {
        throw new NotImplementedException();
    }

    // Create a file upload complete request packet
    public Packet CreateFileUploadCompleteRequest(string userId, string fileId)
    {
        throw new NotImplementedException();
    }

    // Create a file upload complete response packet
    public Packet CreateFileUploadCompleteResponse(bool success, string fileId, string message, string userId)
    {
        throw new NotImplementedException();
    }
    
    // Create a file download init request packet
    public Packet CreateFileDownloadInitRequest(string userId, string fileId)
    {
        throw new NotImplementedException();
    }
    
    // Create a file download init response packet
    public Packet CreateFileDownloadInitResponse(bool success, string fileId, string fileName, long fileSize, string contentType, int totalChunks, string message, string userId)
    {
        throw new NotImplementedException();
    }
    
    // Create a file download chunk request packet
    public Packet CreateFileDownloadChunkRequest(string userId, string fileId, int chunkIndex)
    {
        throw new NotImplementedException();
    }
    
    // Create a file download chunk response packet
    public Packet CreateFileDownloadChunkResponse(bool success, string fileId, int chunkIndex, bool isLastChunk, byte[] data, string message, string userId)
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
    
    // Create a Error response packet
    public Packet CreateErrorResponse(int originalCommandCode, string message, string userId = "")
    {
        throw new NotImplementedException();
    }
}