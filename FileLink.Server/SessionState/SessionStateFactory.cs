using FileLink.Server.Authentication;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Disk.FileManagement;
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
        private readonly DirectoryService _directoryService;
        private readonly LogService _logService;
        
        // Initializes a new instance of the SessionStateFactory
        public SessionStateFactory(AuthenticationService authService, FileService fileService, DirectoryService directoryService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        
        // Creates a new authentication required state for a client session 
        public ISessionState CreateAuthRequiredState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));

            return new AuthRequiredState(clientSession, _authService, _logService);
        }

        public ISessionState CreateAuthenticatedState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));

            return new AuthenticatedState(clientSession, _fileService, _directoryService, _logService);
        }

        public ISessionState CreateTransferState(ClientSession clientSession, FileMetadata fileMetadata, bool isUploading)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));
            if (fileMetadata == null)
                throw new ArgumentNullException(nameof(fileMetadata));

            return new TransferState(clientSession, _fileService, fileMetadata, isUploading, _logService);
        }

        public ISessionState CreateDisconnectingState(ClientSession clientSession)
        {
            if (clientSession == null)
                throw new ArgumentNullException(nameof(clientSession));

            return new DisconnectingState(clientSession, _logService);
        }
    }
}