using System.Net.Sockets;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Network;

// Represents connection to a single client.
// Handles socket logic/communication.
public class ClientSession
{
    // Unique ID for Client Connection.
    public Guid Id { get; } 
    
    // Event is triggered when a message is received from the client.
    public event Action<byte[]>? OnMessageReceived;
    
    // Event triggered when the client disconnects.
    public event Action OnDisconnected;
    
    // Network 
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly ILogger _logger;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isConnected;
    
    // Initializes instance of the ClientSession class.
    public ClientSession(TcpClient tcpClient, ILogger logger)
    {
        Id = Guid.NewGuid();
        _tcpClient = tcpClient;
        _stream = tcpClient.GetStream();
        _logger = logger;
        _isConnected = true;
    }

    public async Task StartAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Start reading messages 
            await ReadMessagesAsync(_cancellationTokenSource.Token);
        }
        catch(Exception ex)
        {
            _logger.Log(LogLevel.Error, $"Error handling client {Id}: {ex.Message}");
            await DisconnectAsync();
        }
    }
    
    // Starts handling client connection asynchronously.
    public async Task SendAsync(byte[] data)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        try
        {
            // Send the length of the data as a 4-byte integer
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            await _stream.WriteAsync(lengthBytes, 0, lengthBytes.Length).ConfigureAwait(false);

            // Send the actual data
            await _stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);

            // Log the data sent
            _logger.Log(LogLevel.Debug, $"Sent {data.Length} bytes to client. {Id}");
        }
        catch (Exception ex)
        {
            // If an error occurs log that too
            _logger.Log(LogLevel.Error, $"Error sending data to client: {Id}: {ex.Message}");
            await DisconnectAsync();
        }
    }
    
    // Disconnects the client.
    public async Task DisconnectAsync()
    {
        if (!_isConnected)
        {
            return;
        }
        
        _isConnected = false;
        _cancellationTokenSource.Cancel();

        try
        {
            _stream.Close();
            _tcpClient.Close();
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"Error disconnecting client {Id}: {ex.Message}");
        }
        
        // Trigger the OnDisconnected event
        OnDisconnected?.Invoke();
    }
    
    // Continuously read messages from the client until disconnected.
    private async Task ReadMessagesAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _isConnected)
            {
                // Read the length of the incoming message
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                // If no bytes are read that means the client disconnected, and log it
                if (bytesRead == 0)
                {
                    _logger.Log(LogLevel.Info, $"Disconnected from client {Id}.");
                    break;
                }
                
                // Convert the length bytes to an integer
                int messageLength = BitConverter.ToInt32(buffer, 0);
                
                // Read the full message data, and keep track of the amount of data received
                byte[] messageBuffer = new byte[messageLength];
                int totalBytesRead = 0;

                // Nested loop to read the entire message in the case of multiple chunks
                while (totalBytesRead < messageLength)
                {
                    // Calculate remaining bytes to be read, read data from _stream into messageBuffer
                    int remaining = messageLength - totalBytesRead;
                    int bytesReceived = await _stream.ReadAsync(messageBuffer, totalBytesRead, remaining, cancellationToken);

                    if (bytesReceived == 0)
                    {
                        // Client disconnected mid-message
                        _logger.Log(LogLevel.Warning, $"Disconnected from client {Id}.");
                        _isConnected = false;
                        break;
                    }
                    
                    // Increment the bytesRead counter
                    totalBytesRead += bytesReceived;
                }
                
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation request
            _logger.Log(LogLevel.Debug, $"Operation cancelled for client: {Id}.");
        }
        catch (Exception ex)
        {
            // Error log
            _logger.Log(LogLevel.Error, $"Error reading messages from client: {Id}: {ex.Message}");
        }
        finally
        {
            // Ensure Disconnection
            await DisconnectAsync();
        }
    }
}