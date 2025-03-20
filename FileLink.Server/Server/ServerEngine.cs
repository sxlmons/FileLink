using FileLink.Server.Authentication;
using FileLink.Server.Commands;
using FileLink.Server.FileManagement;
using FileLink.Server.Network;
using FileLink.Server.Services.Logging;
using FileLink.Server.SessionState;

namespace FileLink.Server.Server;

// Main application class that initiates and coordinates all components of the server
public class ServerEngine
{
    private FileRepository _fileRepository;
    private FileService _fileService;
    private TcpServer _tcpServer;
    private ClientSessionManager _clientSessionManager;
    private LogService _logService;
    private FileLogger _fileLogger;
    private IUserRepository _userRepository;
    private AuthenticationService _authService;
    private SessionStateFactory _sessionStateFactory;
    private CommandHandlerFactory _commandHandlerFactory;
    private bool _initialized = false;
    
    // Gets the server configuration
    public static ServerConfiguration Configuration { get; private set; }
    
    // Initializes an instances oof the ServerEngine class
    public ServerEngine(ServerConfiguration config)
    {
        Configuration = config ?? throw new ArgumentNullException(nameof(config));
    }
    
    // Initialize all components
    public void Initialize()
    {
        if (_initialized) return;

        try
        {
            // Ensure directories exist
            Configuration.EnsureDirectoriesExist();
            
            // Initialize logging
            _fileLogger = new FileLogger(Configuration.LogFilePath);
            _logService = new LogService(_fileLogger);
            _logService.Info("Initializing server engine");
            _logService.Info($"Server configuration: port={Configuration.Port}, MaxConcurrentClients={Configuration.MaxConcurrentClients}");
                
            // Initialize authentication components
            _userRepository = new UserRepository(Configuration.UsersDataPath, _logService);
            /* auth service */
            
            // Initialize file management components
            _fileRepository = new FileRepository(Configuration.FileMetadataPath, Configuration.FileStoragePath, _logService);
            _fileService = new FileService(_fileRepository, Configuration.FileStoragePath, _logService, Configuration.ChunkSize);
            
            // Initialize client session management
            
            // Initialize state and command factories
            
            // Initialize TCP server 
        }
        catch (Exception ex)
        {
            
        }
        
    }
    
    // Starts the server
    public async Task Start()
    {
        
    }
    
    // Stops the server 
    public async Task Stop()
    {
        
    }
}