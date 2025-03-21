using FileLink.Server.Server;

namespace FileLink.Server
{
    // Serves as the main entry point of the program, will initialize 
    // the server engine and begin listening for connections.
    internal class Program
    {
        private static ServerEngine _app;
        private static ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        
        // Entry point for the server
        static async Task Main(string[] args)
        {
            Console.WriteLine("FileLink Server Starting...");

            try
            {
                // Create server configuration
                var config = new ServerConfiguration
                {
                    Port = GetPortFromArgs(args, 9000)
                    // If we need to, add other configurations
                };

                // Create and initialize the application
                _app = new ServerEngine(config);
                _app.Initialize();

                // Set up the console event handlers for shutdown
                Console.CancelKeyPress += OnCancelKeyPress;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

                // Start the server
                await _app.Start();

                // Wait for shutdown signal
                _shutdownEvent.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                await ShutdownAsync();
            }
        }
        
        // Handles Ctrl+C key press
        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Shutdown requested (Ctrl+C)...");
            e.Cancel = true;
            _shutdownEvent.Set();
        }
        
        // Handle process exit
        private static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Process exit detected...");
            _shutdownEvent.Set();
            
            // Ensure synchronous shutdown on process exit
            ShutdownAsync().Wait();
        }
        
        // Shuts down the application
        private static async Task ShutdownAsync()
        {
            try
            {
                if (_app != null)
                {
                    Console.WriteLine("Shutting down server...");
                    await _app.Stop();
                    Console.WriteLine("Server shutdown complete.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during shutdown: {ex.Message}");
            }
        }
        
        // Gets the server port from the command line arguments 
        private static int GetPortFromArgs(string[] args, int defaultPort)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int port) && port > 0 && port < 65536)
            {
                return port;
            }
            return defaultPort;
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
    }
}