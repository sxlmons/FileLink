using System;
using System.Threading.Tasks;
using FileLink.Server.Protocol;

namespace FileLink.Server.Network
{
    // Interface for client sessions to enable testing
    public interface IClientSession
    {
        Guid SessionId { get; }
        string UserId { get; set; }
        bool IsAuthenticated { get; }
        Task Disconnect(string reason);
        Task SendPacket(Packet packet);
    }
}