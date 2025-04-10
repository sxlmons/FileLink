using FileLink.Server.Authentication;
using FileLink.Server.Services.Logging;
using FileLink.Server.Commands.Auth;
using FileLink.Server.Commands.Directory;
using FileLink.Server.Commands.File;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Disk.FileManagement;

namespace FileLink.Server.Commands 
{   
    // Factory for creating command handlers
    // Implements the factory pattern to create the appropriate handler for each command code
    public class CommandHandlerFactory
    {
        private readonly AuthenticationService _authService;
        private readonly FileService _fileService;
        private readonly DirectoryService _directoryService;
        private readonly LogService  _logService;
        private readonly List<ICommandHandler> _handlers = new List<ICommandHandler>();
        
        // Initializes a new instance of the CommandHandlerFactory class
        public CommandHandlerFactory(AuthenticationService authService, FileService fileService, DirectoryService directoryService, LogService logService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            // Register all command handlers
            RegisterDefaultHandlers();
        }
        
        // Registers the default set of command handlers
        private void RegisterDefaultHandlers()
        {
            // Authentication handlers
            RegisterHandler(new LoginCommandHandler(_authService, _logService));
            RegisterHandler(new LogoutCommandHandler(_authService, _logService));
            RegisterHandler(new CreateAccountCommandHandler(_authService, _logService));
            
            // File operation handlers
            RegisterHandler(new FileListCommandHandler(_fileService, _logService));
            RegisterHandler(new FileUploadCommandHandler(_fileService, _directoryService, _logService));
            RegisterHandler(new FileDownloadCommandHandler(_fileService, _logService));
            RegisterHandler(new FileDeleteCommandHandler(_fileService, _logService));
            RegisterHandler(new FileMoveCommandHandler(_directoryService, _logService));
            
            // Directory operation handlers
            RegisterHandler(new DirectoryCreateCommandHandler(_directoryService, _logService));
            RegisterHandler(new DirectoryListCommandHandler(_directoryService, _logService));
            RegisterHandler(new DirectoryRenameCommandHandler(_directoryService, _logService));
            RegisterHandler(new DirectoryDeleteCommandHandler(_directoryService, _logService));
            RegisterHandler(new DirectoryContentsCommandHandler(_directoryService, _logService));
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