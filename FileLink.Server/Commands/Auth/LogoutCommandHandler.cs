using FileLink.Server.Authentication;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands.Auth
{
    // Command handler for logout requests
    // implements the command pattern
    public class LogoutCommandHandler : ICommandHandler
    {
        private readonly IAuthenticationService _authService; // Changed to interface
        private readonly LogService _logService;
        private readonly PacketFactory _packetFactory = new PacketFactory();

        // Initializes a new instance of the command handler class
        public LogoutCommandHandler(IAuthenticationService authService, LogService logService) // Changed to interface
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        // Determines whether this handler can process the specified command code.
        public bool CanHandle(int commandCode)
        {
            return commandCode == FileLink.Server.Protocol.Commands.CommandCode.LOGOUT_REQUEST;
        }

        // Handles a logout request packet
        public async Task<Packet> Handle(Packet packet, ClientSession session)
        {
            try
            {
                // Check if the session is authenticated
                if (string.IsNullOrEmpty(session.UserId))
                {
                    _logService.Warning("Received logout request from unauthenticated session");
                    return _packetFactory.CreateLogoutResponse(false, "You are not logged in.");
                }

                _logService.Info($"User {session.UserId} is logging out");

                // Create response before clearing user ID
                var response = _packetFactory.CreateLogoutResponse(true, "Logout successful.");

                // Schedule disconnect after sending response
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000); // Give time for the response to be sent
                    await session.Disconnect("User logged out");
                });

                return response;
            }
            catch (Exception ex)
            {
                _logService.Error($"Error processing logout request: {ex.Message}", ex);
                return _packetFactory.CreateLogoutResponse(false, "An error occurred during logout.");
            }
        }
    }
}