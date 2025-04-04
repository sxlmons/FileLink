using System.Net;
using System.Net.Sockets;
using FileLink.Server.Commands;
using FileLink.Server.Server;
using FileLink.Server.Services.Logging;
using FileLink.Server.SessionState;

namespace FileLink.Server.Network
{
    // Manages the TCP listener and accepts incoming connections.
    // To be used by serverEngine.cs
    public class TcpServer : IDisposable
    {
        private readonly int _port;
        private readonly TcpListener _listener;
        private readonly ClientSessionManager _clientManager;
        private readonly LogService _logService;
        private readonly CommandHandlerFactory _commandHandlerFactory;
        private readonly SessionStateFactory _sessionStateFactory;
        private readonly ServerConfiguration _config;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;
        private bool _disposed = false;
        
        // Initializes a new instance of the TcpServer class
        public TcpServer(
            int port,
            LogService logService,
            ClientSessionManager clientManager,
            CommandHandlerFactory commandHandlerFactory,
            SessionStateFactory sessionStateFactory,
            ServerConfiguration config)
        {
            _port = port;
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
            _commandHandlerFactory = commandHandlerFactory ?? throw new ArgumentNullException(nameof(commandHandlerFactory));
            _sessionStateFactory = sessionStateFactory ?? throw new ArgumentNullException(nameof(sessionStateFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            // Create the TCP listener
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }
        
        // Starts a TCP server to listen for client connections 
        public async Task Start()
        {
            // Ensure the server isn’t started more than once
            if (_isRunning)
                throw new InvalidOperationException("Server is already running");

            _isRunning = true;

            // This token will be used to signal when the server should stop
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                // Begin listening for client connections on the configured port
                _listener.Start();
                _logService.Info($"Server started on port {_port}");

                // Keep accepting new client connections until a cancellation is requested
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        _logService.Debug("Waiting for client connection...");

                        // Wait for a client connection
                        var client = await _listener.AcceptTcpClientAsync(token);

                        // Configure the client to match the server's buffer settings
                        client.ReceiveBufferSize = _config.NetworkBufferSize;
                        client.SendBufferSize = _config.NetworkBufferSize;

                        // Disable Nagle's algorithm to send packets immediately
                        client.NoDelay = true;

                        _logService.Info($"Client connected from {client.Client.RemoteEndPoint}");

                        // Handle this client connection in a separate background task
                        // Passing the same cancellation token so it can be cancelled if needed
                        _ = Task.Run(() => HandleClientConnection(client, token), token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Thrown when cancellation is requested, so we break out of the loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Log any unexpected errors while waiting for or accepting new clients
                        _logService.Error($"Error accepting client connection: {ex.Message}", ex);
                    }
                }
            }
            finally
            {
                _isRunning = false;
                _listener.Stop();
                _logService.Info($"Server stopped on port {_port}");
            }
        }
        
        // Stops the TCP server
        public async Task Stop()
        {
            if (!_isRunning)
                return;

            try
            {
                _logService.Info("Stopping server...");

                // Cancel all operations
                await _cancellationTokenSource.CancelAsync();

                // Wait for all clients to disconnect
                await _clientManager.DisconnectAllSessions("Server shutting down");

                // Stop the listener
                _listener.Stop();

                _isRunning = false;
                _logService.Info($"Server stopped on port {_port}");
            }
            catch (Exception ex)
            {
                _logService.Error($"Error stopping server: {ex.Message}", ex);
            }
        }
        
        // Handles a client connection
        private async Task HandleClientConnection(TcpClient client, CancellationToken cancellationToken)
        {
            var session = new ClientSession(
                client,
                _logService,
                _sessionStateFactory,
                _commandHandlerFactory,
                _config,
                cancellationToken);

            try
            {
                // Add the session to the manager 
                if (!_clientManager.AddSession(session))
                {
                    _logService.Warning("Failed to add session to manager. Closing connection.");
                    await session.Disconnect("Session manager rejected the connection");
                    return;
                }

                // Start the session processing loop
                await session.StartSession();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error handling client session: {ex.Message}", ex);
            }
            finally
            {
                // Remove the session from the manager 
                _clientManager.RemoveSession(session.SessionId);
                
                // Dispose the session
                session.Dispose();
            }
        }
        
        // Disposes resources used by the TCP server
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Stop the server if its running
                if (_isRunning)
                {
                    Stop().Wait();
                }

                // Dispose the cancellation token source
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error disposing server: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}