using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FileLink.Client.Protocol;
//-------------------------------
// WE ARE REMOVING THIS FILE
//-------------------------------
namespace FileLink.Client.Connection
{
    // Manages the TCP connection to the cloud file server.
    public class CloudServerConnection : IDisposable
    {
        private readonly string _serverHost;
        private readonly int _serverPort;
        private readonly PacketSerializer _packetSerializer;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _receiveLock = new SemaphoreSlim(1, 1);
        private bool _isConnected;
        private bool _disposed;

   
        // Event that is triggered when the connection is closed.
   
        public event EventHandler? ConnectionClosed;

   
        // Gets a value indicating whether the connection is established.
   
        public bool IsConnected => _isConnected && _tcpClient?.Connected == true;
        
        // Initializes a new instance of the CloudServerConnection class.
        public CloudServerConnection(string serverHost, int serverPort)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
            _packetSerializer = new PacketSerializer();
            _isConnected = false;
        }

   
        // Connects to the server
        public async Task ConnectAsync(int timeout = 10000, CancellationToken cancellationToken = default)
        {
            
            Console.WriteLine("ConnectAsync");
            if (IsConnected)
            {
                Console.WriteLine("Already connected");
                return;
            }
            
            try
            {
                // Create a new TCP client
                _tcpClient = new TcpClient();

                // Set up the connection timeout
                using var timeoutCts = new CancellationTokenSource(timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                // Connect to the server
                await _tcpClient.ConnectAsync(_serverHost, _serverPort, combinedCts.Token);

                // Get the network stream
                _stream = _tcpClient.GetStream();

                // Connection successful
                _isConnected = true;
                
                Console.WriteLine($"Connected to {_serverHost}:{_serverPort}");
            }
            catch (OperationCanceledException)
            {
                throw new ConnectionException($"Connection to {_serverHost}:{_serverPort} timed out after {timeout}ms");
            }
            catch (Exception ex)
            {
                throw new ConnectionException($"Failed to connect to {_serverHost}:{_serverPort}: {ex.Message}", ex);
            }
        }
   
        // Disconnects from the server
        public Task DisconnectAsync()
        {
            if (!IsConnected)
                return Task.CompletedTask;

            try
            {
                _stream?.Close();
                _tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnect: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                ConnectionClosed?.Invoke(this, EventArgs.Empty);
            }

            return Task.CompletedTask;
        }

   
        // Sends a packet to the server
        public async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _stream == null)
                throw new ConnectionException("Not connected to server");

            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                await _packetSerializer.WritePacketToStreamAsync(_stream, packet, cancellationToken);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionClosed?.Invoke(this, EventArgs.Empty);
                throw new ConnectionException($"Error sending packet: {ex.Message}", ex);
            }
            finally
            {
                _sendLock.Release();
            }
        }
        
        
        // Receives a packet from the server
        public async Task<Packet> ReceivePacketAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _stream == null)
                throw new ConnectionException("Not connected to server");

            await _receiveLock.WaitAsync(cancellationToken);
            try
            {
                var packet = await _packetSerializer.ReadPacketFromStreamAsync(_stream, cancellationToken);
                return packet;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ConnectionClosed?.Invoke(this, EventArgs.Empty);
                throw new ConnectionException($"Error receiving packet: {ex.Message}", ex);
            }
            finally
            {
                _receiveLock.Release();
            }
        }

   
        // Sends a packet to the server and waits for a response
        public async Task<Packet> SendAndReceiveAsync(Packet packet, int expectedResponseCommandCode, int timeout = 30000, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            try
            {
                // Send the packet
                await SendPacketAsync(packet, combinedCts.Token);

                // Receive the response
                Packet response = await ReceivePacketAsync(combinedCts.Token);

                // Validate the response
                if (response.CommandCode == Commands.CommandCode.ERROR)
                {
                    throw new ConnectionException($"Server returned an error: {response.GetMessage()}");
                }

                if (response.CommandCode != expectedResponseCommandCode)
                {
                    throw new ConnectionException($"Unexpected response command code: {response.CommandCode} (expected {expectedResponseCommandCode})");
                }

                return response;
            }
            catch (OperationCanceledException)
            {
                throw new ConnectionException($"Operation timed out after {timeout}ms");
            }
        }

   
        // Checks if the connection is still active and reconnects if necessary
        public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                await ConnectAsync(cancellationToken: cancellationToken);
            }
        }

   
        // Disposes of resources used by the connection
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                DisconnectAsync().Wait();
                _stream?.Dispose();
                _tcpClient?.Dispose();
                _sendLock.Dispose();
                _receiveLock.Dispose();
            }
            catch
            {
                // Ignore exceptions during disposal
            }

            _disposed = true;
        }
    }

    // Exception thrown when there's an error with the server connection
    public partial class ConnectionException : Exception
    {
        public ConnectionException(string message) : base(message) { }
        public ConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}