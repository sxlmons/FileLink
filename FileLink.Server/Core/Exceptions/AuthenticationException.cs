namespace FileLink.Server.Core.Exceptions;

public class AuthenticationException  : FileLinkServerException
{
    // Initializes a new instance of the FileLinkServerException class
    public AuthenticationException() : base() { }
    
    // Initializes a new instance of the CloudFileServerException class with a specified error message
    public AuthenticationException(string message) : base(message) { }
    
    // Initializes a new instance of the CloudFileServerException class with a specified error message
    // and a reference to the inner exception that is the cause of this exception
    public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}