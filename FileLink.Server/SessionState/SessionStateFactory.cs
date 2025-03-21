using FileLink.Server.Authentication;
using FileLink.Server.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.SessionState
{
    // Factory for creating session states
    // Implements the factory pattern to create different states for client sessions
    public class SessionStateFactory
    {
        private readonly AuthenticationService _authService;
        private readonly FileService _fileService;
        private readonly LogService _logService;
        
        // Initializes a new instance of the SessionStateFactory
        public SessionStateFactory(AuthenticationService authService, FileService fileService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Creates a new authentication required state for a client session 
        public ISessionState CreateAuthRequiredState(ClientSession clientSession)
        {
            ArgumentNullException.ThrowIfNull(clientSession);
            return new AuthRequiredState(clientSession, _authService, _logService);
        }

        public ISessionState CreateAuthenticatedState(ClientSession clientSession)
        {
            ArgumentNullException.ThrowIfNull(clientSession);
            return new AuthenticatedState(clientSession, _fileService, _logService);
        }

        public ISessionState CreateTransferState(ClientSession clientSession, FileMetadata fileMetadata, bool isUploading)
        {
            ArgumentNullException.ThrowIfNull(clientSession);
            return new TransferState(clientSession, _fileService, fileMetadata, isUploading, _logService);
            return null;
        }

        public ISessionState CreateDisconnectingState(ClientSession clientSession)
        {
            ArgumentNullException.ThrowIfNull(clientSession);
            return new DisconnectingState(clientSession, _logService);
        }
    }
}