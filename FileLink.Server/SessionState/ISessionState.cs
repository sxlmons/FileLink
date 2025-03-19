using FileLink.Server.Network;
using FileLink.Server.Protocol;

namespace FileLink.Server.SessionState;

// Interface for session state in the state pattern
// Each concrete state represents a different state of a client session
public interface ISessionState
{
    // Gets the client session this state is associated with 
    ClientSession ClientSession { get; }
    
    // Handles a packet received while in this state
    Task<Packet> HandlePacket(Packet packet);

    // Called when entering a state
    Task OnEnter();
    
    // Called when exiting a state
    Task OnExit();
}