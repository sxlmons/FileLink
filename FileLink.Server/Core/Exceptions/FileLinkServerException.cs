namespace FileLink.Server.Core.Exceptions;

// Base exception class for all FileLink.Server exceptions
public class FileLinkServerException : Exception
{
    // Initializes a new instance of the FileLinkServerException class
    public FileLinkServerException() : base() { }
    
    // Initializes a new instance of the CloudFileServerException class with a specified error message
    public FileLinkServerException(string message) : base(message) { }
    
    // Initializes a new instance of the CloudFileServerException class with a specified error message
    // and a reference to the inner exception that is the cause of this exception
    public FileLinkServerException(string message, Exception innerException) : base(message, innerException) { }
}