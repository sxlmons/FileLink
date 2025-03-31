namespace CloudFileClient.Exceptions
{
    /// <summary>
    /// Base exception class for all CloudFileServer exceptions.
    /// </summary>
    public class CloudFileClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the CloudFileServerException class.
        /// </summary>
        public CloudFileClientException() : base() { }

        /// <summary>
        /// Initializes a new instance of the CloudFileServerException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CloudFileClientException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the CloudFileServerException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CloudFileClientException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}