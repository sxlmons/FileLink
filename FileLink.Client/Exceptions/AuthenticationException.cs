namespace CloudFileClient.Exceptions
{
    /// <summary>
    /// Exception thrown when there's an error during authentication.
    /// </summary>
    public class AuthenticationException : CloudFileClientException
    {
        /// <summary>
        /// Initializes a new instance of the AuthenticationException class.
        /// </summary>
        public AuthenticationException() : base() { }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AuthenticationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the AuthenticationException class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public AuthenticationException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}