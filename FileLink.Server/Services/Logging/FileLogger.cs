namespace FileLink.Server.Services.Logging;

// Implements the ILogger interface to log messages to a file.
public class FileLogger : ILogger
{
    private readonly string _logFilePath;
    private readonly object _lockObj = new object();
    
    public FileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
            
        // Ensure the directory exists
        string directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
            
        // Create or clear the log file
        File.WriteAllText(_logFilePath, $"Log started at {DateTime.Now}\n");
    }
    public void Log(LogLevel level, string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            
        lock (_lockObj)
        {
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    
                // Also print to console for debugging purposes
                Console.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                // If we can't log to file, at least try to output to console
                Console.WriteLine($"Error logging to file: {ex.Message}");
                Console.WriteLine(logEntry);
            }
        }    
    }
    
    public void Log(LogLevel level, string message, Exception exception)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        string exceptionDetails = $"Exception: {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}";
            
        lock (_lockObj)
        {
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine + exceptionDetails + Environment.NewLine);
                    
                // Also print to console for debugging purposes
                Console.WriteLine(logEntry);
                Console.WriteLine(exceptionDetails);
            }
            catch (Exception ex)
            {
                // If we can't log to file, at least try to output to console
                Console.WriteLine($"Error logging to file: {ex.Message}");
                Console.WriteLine(logEntry);
                Console.WriteLine(exceptionDetails);
            }
        }
    }

    public void LogException(LogLevel level, string message, Exception exception)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        string exceptionDetails = $"Exception: {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}";
            
        lock (_lockObj)
        {
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine + exceptionDetails + Environment.NewLine);
                    
                // Also print to console for debugging purposes
                Console.WriteLine(logEntry);
                Console.WriteLine(exceptionDetails);
            }
            catch (Exception ex)
            {
                // If we can't log to file, at least try to output to console
                Console.WriteLine($"Error logging to file: {ex.Message}");
                Console.WriteLine(logEntry);
                Console.WriteLine(exceptionDetails);
            }
        }
    }
    
    public void Debug(string message) => Log(LogLevel.Debug, message);

    public void Info(string message) => Log(LogLevel.Info, message);

    public void Warning(string message) => Log(LogLevel.Warning, message);
    
    public void Error(string message) => Log(LogLevel.Error, message);
    
    public void Fatal(string message) => Log(LogLevel.Fatal, message);
    
    public void Fatal(string message, Exception exception) => Log(LogLevel.Fatal, message, exception);
}