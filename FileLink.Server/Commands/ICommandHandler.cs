using FileLink.Server.Network;
using FileLink.Server.Protocol;

namespace FileLink.Server.Commands
{
// Interface for command handlers in the command pattern
// Each command handler is responsible for processing a specific type of packet yo
    public interface ICommandHandler
    {
        // Determines whether this handler can process the specified command code
        bool CanHandle(int commandCode);

        // Handles the processing of the packet yo
        Task<Packet> Handle(Packet packet, ClientSession session);
    }
}