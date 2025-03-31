using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FileLink.Client.Connection;
using FileLink.Client.FileOperations;

//-------------------------------
// WE ARE REMOVING THIS FILE
//-------------------------------

namespace FileLink.Client.Session
{
    // Manages a session with the cloud file server.
    public class CloudSession : IDisposable
    {
        private readonly CloudServerConnection _connection;
        private readonly AuthenticationManager _authManager;
        private readonly FileManager _fileManager;
        private bool _disposed;
       
        // Gets the authentication manager for this session
        public AuthenticationManager AuthManager => _authManager;
        
        // Gets the file manager for this session
        public FileManager FileManager => _fileManager;
       
        // Gets a value indicating whether the session is connected to the server
        public bool IsConnected => _connection.IsConnected;
       
        // Gets a value indicating whether the session is authenticated
        public bool IsAuthenticated => _authManager.IsAuthenticated;
       
        // Gets the user ID for this session
        public string UserId => _authManager.UserId;
       
        // Event that is triggered when the connection is closed
        public event EventHandler? ConnectionClosed;
       
        // Initializes a new instance of the CloudSession class
        public CloudSession(string serverHost, int serverPort = 9000)
        {
            _connection = new CloudServerConnection(serverHost, serverPort);
            _connection.ConnectionClosed += OnConnectionClosed;
            _authManager = new AuthenticationManager(_connection);
            _fileManager = new FileManager(_connection, _authManager);
        }

       
        // Connects to the server
        public Task ConnectAsync(int timeout = 10000, CancellationToken cancellationToken = default)
        {
            return _connection.ConnectAsync(timeout, cancellationToken);
        }

       
        // Disconnects from the server
        public async Task DisconnectAsync()
        {
            try
            {
                // Attempt to logout if authenticated
                if (IsAuthenticated)
                {
                    await _authManager.LogoutAsync();
                }
            }
            finally
            {
                // Disconnect from the server
                await _connection.DisconnectAsync();
            }
        }

       
        // Handles the connection closed event
        private void OnConnectionClosed(object? sender, EventArgs e)
        {
            // Forward the event
            ConnectionClosed?.Invoke(this, e);
        }

       
        // Disposes of resources used by the session
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Disconnect from the server
                DisconnectAsync().Wait();
                
                // Dispose the connection
                _connection.Dispose();
            }
            catch
            {
                // Ignore exceptions during disposal
            }

            _disposed = true;
        }
    }
}
