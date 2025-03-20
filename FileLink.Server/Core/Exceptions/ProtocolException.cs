namespace FileLink.Server.Core.Exceptions;

// Exception is thrown when there's an error in protocol serialization or deserialization
public class ProtocolException : FileLinkServerException
{
    // Initializes a new instance of the ProtocolException class
    public ProtocolException() : base() { }
    
    // Initializes a new instance of the ProtocolException class with a specified error message
    public  ProtocolException(string message) : base(message) { }
    
    // Initializes a new instance of the ProtocolException class with a specified error message
    // and a reference to the inner exception that is the cause of this exception
    public ProtocolException(string message, Exception innerException) : base(message, innerException) { }
}