using System.Net.Sockets;
using FileLink.Server.Commands;
using FileLink.Server.Protocol;
using FileLink.Server.Server;
using FileLink.Server.Services.Logging;
using FileLink.Server.SessionState;

namespace FileLink.Server.Network
{
    // Represents a client connection to the server 
    // Manages the communication with the client and implements the session state machine
    public class ClientSession : IDisposable
    {
        private SessionState.ISessionState _currentState;
        private readonly TcpClient _client;
        private readonly NetworkStream _stream; 
        private readonly PacketSerializer _packetSerializer = new PacketSerializer();
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _receiveLock = new SemaphoreSlim(1, 1);   
        private readonly CancellationToken _cancellationToken;
        private readonly ServerConfiguration _config;
        private bool _disposed = false; 
        
        public Guid SessionId { get; }
        public string UserId { get; set; } = string.Empty;
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
        public LogService LogService { get; }
        public CommandHandlerFactory CommandHandlerFactory { get; }
        public SessionStateFactory StateFactory { get; }
        public DateTime LastActiviyTime { get; private set; }
        
        // IMPORTANT: This controls how large of files we can send. Right now it is set to 5MB
        private const int MaxPacketSize = 5 * 1024 * 1024;

        public ClientSession(
            // Params
            TcpClient client,
            LogService logService,
            SessionStateFactory stateFactory,
            CommandHandlerFactory commandHandlerFactory,
            ServerConfiguration config,
            CancellationToken cancellationToken)
        {
            // Initialization
            _client = client ?? throw new ArgumentNullException(nameof(client));
            LogService = logService ?? throw new ArgumentNullException(nameof(logService));
            StateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
            CommandHandlerFactory = commandHandlerFactory ?? throw new ArgumentNullException(nameof(commandHandlerFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cancellationToken = cancellationToken;
            
            _stream = client.GetStream();
            SessionId = Guid.NewGuid();
            LastActiviyTime = DateTime.Now;
            
            // Set initial state to AuthRequiredState
            _currentState = stateFactory.CreateAuthRequiredState(this);
        }

        public async Task StartSession()
        {
            try
            {
                // Enter initial state
                await _currentState.OnEnter();
                LogService.Info($"Session started: {SessionId} from {GetClientAddress()}");

                // Process packets until cancelled or disconnected
                while (!_cancellationToken.IsCancellationRequested && _client.Connected)
                {
                    try
                    {
                        // Receive that fricken packet yo
                        var packet = await ReceivePacket();
                        if (packet == null)
                        {
                            LogService.Debug($"Null packet received, client may have been disconnected: {SessionId}");
                            break;
                        }

                        // Update last activity time 
                        LastActiviyTime = DateTime.Now;

                        // Log the packet yo
                        LogService.LogPacket(packet, false, SessionId);

                        // Process the packet
                        var response = await _currentState.HandlePacket(packet);

                        // Send the response 
                        if (response == null)
                        {
                            LogService.LogPacket(response, true, SessionId);
                            await SendPacket(response);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogService.Info($"Session operation cancelled: {SessionId}");
                    }
                    catch (IOException ex)
                    {
                        LogService.Error($"IO Exception: {SessionId}: {ex.Message}, {ex}");
                    }
                    catch (Exception ex)
                    {
                        // Continue processing the next packet unless disconnected
                        LogService.Error($"Error processing packet in session: {SessionId}: {ex.Message}, {ex}");
                        if (!_client.Connected)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Session loop terminated with error: {ex.Message}, {ex}");
            }
            finally
            {
                await Disconnect("Session loop terminated");
            }
        }
        
        // Receives a packet from the client 
        public async Task<Packet> ReceivePacket()
        {
            await _receiveLock.WaitAsync(_cancellationToken);
            try
            {
                // Read the packet length
                byte[] lengthBuffer = new byte[4];
                int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, _cancellationToken);
                if (bytesRead < 4)
                {
                    LogService.Debug($"Connection closed while reading packet length: {bytesRead} bytes read");
                    return null;
                }
                
                // Convert to integer (packet length)
                int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (packetLength <= 0 || packetLength > MaxPacketSize)
                {
                    LogService.Warning($"Invalid packet length: {packetLength}");
                    return null;
                }
                
                // Read the packet data
                byte[] packetBuffer = new byte[packetLength];
                int totalBytesRead = 0;
                while (totalBytesRead < packetLength)
                {
                    int bytesRemaining = packetLength - totalBytesRead;
                    int readSize = Math.Min (bytesRemaining, _config.NetworkBufferSize);
                    
                    bytesRead = await _stream.ReadAsync(
                        packetBuffer, 
                        totalBytesRead, 
                        readSize, 
                        _cancellationToken);

                    if (bytesRead == 0)
                    {
                        LogService.Debug($"Connection closed while reading packet data");
                        return null!;
                    }
                    totalBytesRead += bytesRead;
                }
                // Deserialize the packet yo
                var packet = _packetSerializer.Deserialize(packetBuffer);
                return packet;
            }
            finally 
            {
                _receiveLock.Release();
            }
        }
        
        // Sends a packet to the client 
        public async Task SendPacket(Packet packet)
        {
            if  (packet == null)
                throw new ArgumentNullException(nameof(packet));
            
            await _sendLock.WaitAsync(_cancellationToken);
            try
            {
                // Serialize the packet
                byte[] packetData = _packetSerializer.Serialize(packet);
                
                // Create a buffer with the packet length prefix
                byte[] lengthPrefix = BitConverter.GetBytes(packetData.Length);
                
                // Send the length prefix
                await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length, _cancellationToken);
                
                // Send the packet data
                await _stream.WriteAsync(packetData, 0, packetData.Length, _cancellationToken);
                
                // Flush the stream
                await _stream.FlushAsync(_cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }
        
        // Transitions the session to a new state 
        public void TransitionToState(SessionState.ISessionState newState)
        {
            if  (newState == null)
                throw new ArgumentNullException(nameof(newState));
            
            LogService.Debug($"Sessions {SessionId} transitioning from {_currentState.GetType().Name} to {newState.GetType().Name}");
            
            // Exit the current state
            _currentState.OnExit().Wait();
            
            // Set the new state
            _currentState = newState;
            
            // Enter the new state 
            _currentState.OnEnter().Wait();
        } 
        
        // Disconnects the client session
        public async Task Disconnect(string reason)
        {
            if (_disposed)
                return;

            try
            {
                LogService.Info($"Disconnecting session {SessionId}: {reason}");
                
                // Ensure we're in the disconnecting state
                if (!(_currentState is DisconnectingState))
                {
                    TransitionToState(StateFactory.CreateDisconnectingState(this));
                }
                // Close the connection
                _client.Close();
            }
            catch (Exception ex)
            {
                LogService.Error($"Error during disconnect: {ex.Message}", ex);
            }
            finally
            {
                Dispose();
            }
        }
        
        // Gets the clients IP address and port
        public string GetClientAddress()
        {
            try
            {
                return _client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            }
            catch 
            {
                return "Unknown";
            }
        }
        
        // Checks if the session has timed out 
        public bool HasTimedOut(int timeoutMinutes)
        {
            return DateTime.Now.Subtract(LastActiviyTime).TotalMinutes > timeoutMinutes;
        }
        
        // Disposes resources used by the client session
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _stream?.Dispose();
                _client?.Dispose();
                _sendLock?.Dispose();
                _receiveLock?.Dispose();
            }
            catch (Exception ex)
            {
                LogService.Error($"Error during dispose: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;   
            }
        }
    }
}