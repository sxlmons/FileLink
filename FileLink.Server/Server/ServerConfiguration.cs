namespace FileLink.Server.Server
{
    public class ServerConfiguration
    {
        public int Port { get; set; }
        public string UsersDataPath { get; set; } = "data/users";
        public string FileMetadataPath { get; set; }  = "data/metadata";
        public string FileStoragePath { get; set; } = "data/files";
        public string LogFilePath { get; set; }  = "data/server.log";
        public int MaxConcurrentClients { get; set; } = 100;
        public int ChunkSize { get; set; } = 1024 * 1024; // 1MB
        public int SessionTimeoutMinutes { get; set; } = 30;
        public int NetworkBufferSize { get; set; } = 8192; //8KB
        public bool EnableDebugLogging { get; set; } = false;
        public bool LogPacketContents { get; set; } = false;

        ServerConfiguration()
        {
            // intentionally left blank
        }

        // Validates the configuration settings to ensure they are valid
        public bool Validate()
        {
            // Port must be a valid TCP port
            if (Port < 1 || Port > 65535)
                return false;

            // Paths must not be empty
            if (string.IsNullOrWhiteSpace(UsersDataPath) ||
                string.IsNullOrWhiteSpace(FileMetadataPath) ||
                string.IsNullOrWhiteSpace(FileStoragePath) ||
                string.IsNullOrWhiteSpace(LogFilePath))
                return false;

            // MaxConcurrentClients must be positive
            if (MaxConcurrentClients <= 0)
                return false;

            // ChunkSize must be positive
            if (ChunkSize <= 0)
                return false;

            // SessionTimeoutMinutes must be positive
            if (SessionTimeoutMinutes <= 0)
                return false;

            // NetworkBufferSize must be positive
            if (NetworkBufferSize <= 0)
                return false;

            return true;
        }
        
        // Ensures all required directories exist
        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(UsersDataPath);
            Directory.CreateDirectory(FileMetadataPath);
            Directory.CreateDirectory(FileStoragePath);
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
        }
    }
}