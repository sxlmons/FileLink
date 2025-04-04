using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using FileLink.Client.Protocol;

namespace FileLink.Client.Services
{
    // Provides network communication services with the CloudFileServer.
    public class NetworkService
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly PacketSerializer _packetSerializer = new PacketSerializer();
        private string _serverAddress = "localhost";
        private int _serverPort = 9000;
        private bool _isConnected = false;

        // Gets a value indicating whether the client is connected to the server.
        public bool IsConnected => _isConnected && _client?.Connected == true;
        
        // Sets the server address and port
        public void SetServer(string address, int port)
        {
            _serverAddress = address;
            _serverPort = port;
        }

        // Connects to the server.
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (IsConnected)
                    return true;

                _client = new TcpClient();
                await _client.ConnectAsync(_serverAddress, _serverPort);
                _stream = _client.GetStream();
                _isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }
        
        // Disconnects from the server.
        public void Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnect error: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
            }
        }
        
        // Sends a packet to the server and receives a response
        public async Task<Packet?> SendAndReceiveAsync(Packet packet)
        {
            if (!IsConnected)
            {
                bool connected = await ConnectAsync();
                if (!connected)
                    return null;
            }

            try
            {
                // Serialize the packet
                byte[] packetData = _packetSerializer.Serialize(packet);
                
                // Send the length prefix
                byte[] lengthPrefix = BitConverter.GetBytes(packetData.Length);
                await _stream!.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                
                // Send the packet data
                await _stream!.WriteAsync(packetData, 0, packetData.Length);
                
                // Read the response length
                byte[] lengthBuffer = new byte[4];
                int bytesRead = await _stream!.ReadAsync(lengthBuffer, 0, 4);
                if (bytesRead < 4)
                    return null;
                
                int responseLength = BitConverter.ToInt32(lengthBuffer, 0);
                
                // Read the response data
                byte[] responseBuffer = new byte[responseLength];
                int totalBytesRead = 0;
                while (totalBytesRead < responseLength)
                {
                    bytesRead = await _stream!.ReadAsync(
                        responseBuffer, 
                        totalBytesRead, 
                        responseLength - totalBytesRead);
                    
                    if (bytesRead == 0)
                        return null;
                    
                    totalBytesRead += bytesRead;
                }
                
                // Deserialize the response
                return _packetSerializer.Deserialize(responseBuffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Communication error: {ex.Message}");
                Disconnect();
                return null;
            }
        }
        
        // Call this method when disconnection is detected or during logout
        // FIX for logout issue
        public void ResetConnection()
        {
            // Dispose existing connection if any
            if (_client != null)
            {
                try
                {
                    _client.Close();
                    _client.Dispose();
                }
                catch { /* Ignore errors during cleanup */ }
                finally
                {
                    _client = null;
                    _stream = null;
                    _isConnected = false;
                }
            }
        }
    }
}