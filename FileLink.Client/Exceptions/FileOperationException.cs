namespace CloudFileClient.Exceptions
{
    /// <summary>
    /// Exception thrown when there's an error during file operations.
    /// </summary>
    public class FileOperationException : CloudFileClientException
    {
        /// <summary>
        /// Initializes a new instance of the FileOperationException class.
        /// </summary>
        public FileOperationException() : base() { }

        /// <summary>
        /// Initializes a new instance of the FileOperationException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FileOperationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the FileOperationException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public FileOperationException(string message, Exception innerException) 
            : base(message, innerException) { }

        /// <summary>
        /// Gets or sets the file ID associated with this exception.
        /// </summary>
        public string? FileId { get; set; }
    }
}