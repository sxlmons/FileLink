using FileLink.Server.Authentication;
using FileLink.Server.FileManagement;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Commands 
{   
    // Factory for creating command handlers
    // Implements the factory pattern to create the appropriate handler for each command code
    public class CommandHandlerFactory
    {
        private readonly AuthenticationService _authService;
        private readonly FileService _fileService;
        private readonly LogService  _logService;
        private readonly List<ICommandHandler> _handlers = new List<ICommandHandler>();
        
        // Initializes a new instance of the CommandHandlerFactory class
        public CommandHandlerFactory(AuthenticationService authService, FileService fileService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Register all command handlers
            RegisterDefaultHandlers();
        }
        
        // Registers the default set of command handlers
        private void RegisterDefaultHandlers()
        {
            // Authentication handlers
            RegisterHandler(new CreateAccountCommandHandler(_authService, _logService));
            RegisterHandler(new LoginCommandHandler(_authService, _logService));
            RegisterHandler(new LogoutCommandHandler(_authService, _logService));
            
            // File operation handlers
            RegisterHandler(new FileListCommandHandler(_fileService, _logService));
            RegisterHandler(new FileUploadCommandHandler(_fileService, _logService));
            RegisterHandler(new FileDownloadCommandHandler(_fileService, _logService));
            RegisterHandler(new FileDeleteCommandHandler(_fileService, _logService));
        }
        
        // Creates a command handler for the specified command code
        public ICommandHandler CreateHandler(int commandCode)
        {
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(commandCode))
                {
                    return handler;
                }
            }
            _logService.Warning($"No handler found for command code: {Protocol.Commands.CommandCode.GetCommandName(commandCode)} ({commandCode})");
                return null!;
        }
        
        // Registers a command handler
        private void RegisterHandler(ICommandHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            _handlers.Add(handler);
            _logService.Info($"Registered command handler: {handler.GetType().Name}");
        }
    }
}