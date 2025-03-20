namespace FileLink.Server.Core.Exceptions;

public class FileOperationException : FileLinkServerException
{
    // Initializes a new instance of the FileLinkServerException class
    public FileOperationException() : base() { }
    
    // Initializes a new instance of the CloudFileServerException class with a specified error message
    public FileOperationException(string message) : base(message) { }
    
    // Initializes a new instance of the CloudFileServerException class with a specified error message
    // and a reference to the inner exception that is the cause of this exception
    public FileOperationException(string message, Exception innerException) : base(message, innerException) { }
    
    // Gets/Sets the file ID associated with this exception
    public string? FileId { get; set; }
}