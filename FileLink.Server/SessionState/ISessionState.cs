using FileLink.Server.Network;
using FileLink.Server.Protocol;

namespace FileLink.Server.SessionState;

// Interface for session state in the state pattern
// Each concrete state represents a different state of a client session
public interface ISessionState
{
    ClientSession ClientSession { get; }

    Task<Packet> HandlePacket(Packet packet);

    Task OnEnter();

    Task OnExit();
}