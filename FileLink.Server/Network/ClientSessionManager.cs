using System.Collections.Concurrent;
using FileLink.Server.FileManagement;
using FileLink.Server.Server;
using FileLink.Server.Services.Logging;

namespace FileLink.Server.Network
{
    // Manages all active connections
    // Provides functionality for tracking, monitoring, and cleaning up sessions 
    public class ClientSessionManager : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, ClientSession> _sessions = new ConcurrentDictionary<Guid, ClientSession>();
        private readonly LogService _logService;
        private readonly ServerConfiguration _config;
        private readonly Timer _cleanupTimer;
        private bool _disposed;
        
        // Initialize instance of ClientSessionManager
        public ClientSessionManager(LogService logService, ServerConfiguration config)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            // Start a timer to periodically cleanup inactive sessions 
            _cleanupTimer = new Timer(CleanupTimerCallBack, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
        
        // Gets number of active connections
        public int SessionCount => _sessions.Count;
        
        // Adds a client session to the manager
        public bool AddSession(ClientSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            
            // Check if we've reached the max number of concurrent clients
            if (_sessions.Count >= _config.MaxConcurrentClients)
            {
                _logService.Warning($"Maximum number of concurrent clients reached ({_config.MaxConcurrentClients}). Rejecting new connection.");
                return false;
            }
            
            // Add the session to the dictionary
            bool added = _sessions.TryAdd(session.SessionId, session);

            if (added)
            {
                _logService.Info($"Session added: {session.SessionId}, Total active sessions: {_sessions.Count}");
            }
            else
            {
                _logService.Warning($"Failed to add session {session.SessionId} to the manager.");
            }
            return added;
        }

        // Method to remove client
        public bool RemoveSession(Guid sessionId)
        {
            bool removed = _sessions.TryRemove(sessionId, out ClientSession session);
            if (removed)
            {
                _logService.Info($"Session removed: {session.SessionId}, Total active sessions: {_sessions.Count}");
                
                // Dispose the session
                session?.Dispose();
            }
            return removed;
        }
        
        // Gets a client session by ID
        public ClientSession GetSession(Guid sessionId)
        {
            _sessions.TryGetValue(sessionId, out ClientSession session);
            return session;
        }
        
        // Gets all active client sessions 
        public IEnumerable<ClientSession> GetAllSessions()
        {
            return _sessions.Values;
        }
        
        // Gets a client via specific user
        public IEnumerable<ClientSession> GetSessionsByUserId(string userId)
        {
            return  _sessions.Values.Where(s => s.UserId == userId);
        }
        
        // Cleans up inactive sessions that have timed out 
        public async Task CleanupInactiveSessions()
        {
            var sessionTimeoutMinutes = _config.SessionTimeoutMinutes;
            var timedOutSessions = _sessions.Values.Where(s => s.HasTimedOut(sessionTimeoutMinutes)).ToList();

            foreach (var session in timedOutSessions)
            {
                _logService.Info($"Session {session.SessionId} timed out after {sessionTimeoutMinutes} minutes of inactivity");
                
                // Disconnect the session
                await session.Disconnect("Session timed out");
                
                // Remove the session from the manager
                RemoveSession(session.SessionId);
            }

            if (timedOutSessions.Count > 0)
            {
                _logService.Info($"Cleaned up {timedOutSessions.Count} inactive sessions. Remaining sessions: {_sessions.Count}");
            }
        }
        
        // Callback method for the cleanup timer
        private async void CleanupTimerCallBack(object state)
        {
            try
            {
                await CleanupInactiveSessions();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error in session cleanup timer: {ex.Message}", ex);
            }
        }
        
        // Disconnects all active sessions
        public async Task DisconnectAllSessions(string reason)
        {
            _logService.Info($"Disconnecting all sessions: {reason}");
            
            var tasks = new List<Task>();
            
            foreach (var session in _sessions.Values)
            {
                tasks.Add(session.Disconnect(reason));
            }
                
            await Task.WhenAll(tasks);
            
            // Clear the sessions dictionary
            _sessions.Clear();
        }
        
        // Disposes resources used by the client session manager
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Stop the cleanup timer
                _cleanupTimer?.Dispose();
                
                // Disconnect all sessions
                DisconnectAllSessions("Server shutting down").Wait();
                
                // Dispose all sessions
                foreach (var session in _sessions.Values)
                {
                    session.Dispose();
                }
                
                // Clear the sessions dictionary
                _sessions.Clear();
            }
            catch (Exception ex)
            {
                _logService.Error($"Error disposing session manager: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}