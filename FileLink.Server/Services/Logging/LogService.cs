using FileLink.Server.Protocol;

namespace FileLink.Server.Services.Logging
{
    // Centralized logging service that can log to multiple loggers
    // Also provides packet logging functionality
    public class LogService : ILogger
    {
        // implement ILogger
        private readonly List<ILogger> _loggers = new List<ILogger>();

        // initialize LogService class
        public LogService(params ILogger[] loggers)
        {
            _loggers.AddRange(loggers);
        }
        
        // Add Logger
        public void AddLogger(ILogger logger)
        {
            _loggers.Add(logger);
        }
        
        // Log message with specified level
        public void Log(LogLevel level, string message)
        {
            foreach (var logger in _loggers)
            {
                logger.Log(level, message);
            }
        }
        
        // log message with specified level and exception
        public void Log(LogLevel level, string message, Exception exception)
        {
            foreach (var logger in _loggers)
            {
                logger.Log(level, message, exception);
            }
        }
        
        // log packet being sent or received
        public void LogPacket(Packet packet, bool isSending, Guid sessionId)
        {
            string direction = isSending ? "Sending" : "Receiving";
            string commandName = CommandCode.GetCommandName(packet.CommandCode);
            
            // Determine payload description
            string payloadDesc = packet.Payload == null || packet.Payload.Length == 0 ? "No Payload" : $"Payload Size: {packet.Payload.Length} Bytes";
            
            // Build metadata string
            string metadata = string.Join(", ", packet.Metadata.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            metadata = string.IsNullOrWhiteSpace(metadata) ? "No Metadata" : $"Metadata: {metadata}";
            
            // Log basic packet info at Info level
            Log(LogLevel.Info, $"[{direction}] Session: {sessionId}, Command: {commandName}, Payload: {payloadDesc}");
            
            // Log detailed metadata at Debug level
            Log(LogLevel.Info, $"[{direction}] Session: {sessionId}, Command: {commandName}, Metadata: {metadata}");
        }
        
        public void Debug(string message) => Log(LogLevel.Debug, message);
        
        public void Info(string message) => Log(LogLevel.Info, message);
        
        public void Warning(string message) => Log(LogLevel.Warning, message);
        
        public void Error(string message) => Log(LogLevel.Error, message);
        
        public void Error(string message, Exception exception) => Log(LogLevel.Error, message, exception);
        
        public void Fatal(string message) => Log(LogLevel.Fatal, message);
        
        public void Fatal(string message, Exception exception) => Log(LogLevel.Fatal, message, exception);
    }
}