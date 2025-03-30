using FileLink.Server.Authentication;
using FileLink.Server.Commands;
using FileLink.Server.Disk.FileManagement;
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
            _authService = new AuthenticationService(_userRepository, _logService);
            
            // Initialize file management components
            _fileRepository = new FileRepository(Configuration.FileMetadataPath, Configuration.FileStoragePath, _logService);
            _fileService = new FileService(_fileRepository, Configuration.FileStoragePath, _logService, Configuration.ChunkSize);
            
            // Initialize client session management
            _clientSessionManager = new ClientSessionManager(_logService, Configuration);

            // Initialize state and command factories
            _sessionStateFactory = new SessionStateFactory(_authService, _fileService, _logService);
            _commandHandlerFactory = new CommandHandlerFactory(_authService, _fileService, _logService);

            // Initialize TCP server
            _tcpServer = new TcpServer(Configuration.Port, _logService, _clientSessionManager, _commandHandlerFactory, _sessionStateFactory, Configuration);

            _initialized = true;
            _logService.Info("Cloud File Server initialized successfully");
        }
        catch (Exception ex)
        {
            _logService?.Fatal("Failed to initialize Cloud File Server", ex);
            throw;
        }
        
    }
    
    // Starts the server
    public async Task Start()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Server must be initialized before starting.");
        }

        try
        {
            _logService.Info("Starting Cloud File Server...");
            await _tcpServer.Start();
        }
        catch (Exception ex)
        {
            _logService.Fatal("Failed to start Cloud File Server", ex);
            throw;
        }
    }
    
    // Stops the server 
    public async Task Stop()
    {
        try
        {
            _logService.Info("Stopping Cloud File Server...");
                
            // Stop the server components in reverse order
            if (_tcpServer != null)
            {
                await _tcpServer.Stop();
            }
                
            if (_clientSessionManager != null)
            {
                await _clientSessionManager.DisconnectAllSessions("Server shutting down");
            }
                
            _logService.Info("Cloud File Server stopped");
        }
        catch (Exception ex)
        {
            _logService.Error("Error stopping Cloud File Server", ex);
            throw;
        }
    }
}