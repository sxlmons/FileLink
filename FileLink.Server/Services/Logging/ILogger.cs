namespace FileLink.Server.Services.Logging;

// Log level enumeration for categorizing log messages.
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

// Interface for logging services
public interface ILogger
{
    void Log(LogLevel level, string message);
    
    void LogException(LogLevel level, string message, Exception exception);
    
    void Debug(string message);
    
    void Info(string message);
    
    void Warning(string message);
    
    void Error(string message);
    
    void Fatal(string message);
    
    void FatalException(string message, Exception exception);
}