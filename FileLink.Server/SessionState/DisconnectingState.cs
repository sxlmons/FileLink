using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.SessionState
{
    public class DisconnectingState : ISessionState
    {
        private readonly LogService _logService;
        private readonly PacketFactory  _packetFactory = new PacketFactory();
        
        // Gets the new session this state is associated with
        public ClientSession ClientSession { get; }
        
        // Initializes a new instance of the DisconnectingState class
        public DisconnectingState(ClientSession clientSession, LogService logService)
        {
            ClientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public Task<Packet> HandlePacket(Packet packet)
        {
            _logService.Debug($"Received packet in disconnecting state: {FileLink.Server.Protocol.Commands.CommandCode.GetCommandName(packet.CommandCode)}");
            
            // Always respond with an error in this state 
            var response = _packetFactory.CreateErrorResponse(packet.CommandCode, "Session is disconnecting", ClientSession.UserId);
            
            return Task.FromResult(response);   
        }

        public Task OnEnter()
        {
            _logService.Debug($"Session {ClientSession.SessionId} entered DisconnectingState");
            return Task.CompletedTask;
        }

        public Task OnExit()
        {
            _logService.Debug($"Session {ClientSession.SessionId} exited DisconnectingState");
            return Task.CompletedTask;
            
        }
    }
}