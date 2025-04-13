namespace FileLink.Server.Core.Storage
{
    // Provides an abstraction for storage operations, allowing different
    // storage mechanisms (file system, database, cloud) to be used interchangeably.
    public interface IStorageProvider
    {
        // Reads data from the specified location and deserializes it to type T.
        Task<T> ReadAsync<T>(string location) where T : class;
        
        // Writes the serialized data to the specified location.
        Task WriteAsync<T>(string location, T data) where T : class;
        
        // Deletes data at the specified location.
        Task DeleteAsync(string location);
        
        // Checks if data exists at the specified location.
        Task<bool> ExistsAsync(string location);
        
        // Lists all locations matching the specified pattern.
        Task<IEnumerable<string>> ListLocationsAsync(string pattern);
        
        // Creates a directory at the specified location if it doesn't exist.
        Task EnsureDirectoryExistsAsync(string directoryPath);
        
        // Copies data from source location to destination location.
        Task CopyAsync(string sourceLocation, string destinationLocation);
        
        // Moves data from source location to destination location.
        Task MoveAsync(string sourceLocation, string destinationLocation);
    }
}