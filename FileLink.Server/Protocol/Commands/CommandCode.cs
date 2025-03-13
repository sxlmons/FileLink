namespace FileLink.Server.Protocol.Commands;

// UNDER CONSTRUCTION
// Contains constants for all valid command codes used in the protocol.
// These codes are sent in packets to indicate the type of request or response.
public static class CommandCode
{
    // Auth Commands
    public const int LOGIN_REQUEST = 100;
    public const int LOGIN_RESPONSE = 101;
    public const int LOGOUT_REQUEST = 102;
    public const int LOGOUT_RESPONSE = 103;
    
    // File operations
    public const int FILE_LIST_REQUEST = 200;
    public const int FILE_LIST_RESPONSE = 201;
    public const int FILE_UPLOAD_REQUEST = 202;
    public const int FILE_UPLOAD_CHUNK = 203;
    public const int FILE_UPLOAD_COMPLETE = 204;
    public const int FILE_UPLOAD_RESPONSE = 205;
    public const int FILE_DOWNLOAD_REQUEST = 206;
    public const int FILE_DOWNLOAD_CHUNK = 207;
    public const int FILE_DOWNLOAD_COMPLETE = 208;
    public const int FILE_DELETE_REQUEST = 209;
    public const int FILE_DELETE_RESPONSE = 210;
    
    // Status responses
    public const int SUCCESS = 300;
    public const int ERROR = 301;
    public const int UNAUTHORIZED = 302;
    
    // Dictionary for lookup
    private static readonly Dictionary<int, string> CommandNames = new()
    {
        { LOGIN_REQUEST, "LOGIN_REQUEST" },
        { LOGIN_RESPONSE, "LOGIN_RESPONSE" },
        { LOGOUT_REQUEST, "LOGOUT_REQUEST" },
        { LOGOUT_RESPONSE, "LOGOUT_RESPONSE" },
        
        { FILE_LIST_REQUEST, "FILE_LIST_REQUEST" },
        { FILE_LIST_RESPONSE, "FILE_LIST_RESPONSE" },
        { FILE_UPLOAD_REQUEST, "FILE_UPLOAD_REQUEST" },
        { FILE_UPLOAD_CHUNK, "FILE_UPLOAD_CHUNK" },
        { FILE_UPLOAD_COMPLETE, "FILE_UPLOAD_COMPLETE" },
        { FILE_UPLOAD_RESPONSE, "FILE_UPLOAD_RESPONSE" },
        { FILE_DOWNLOAD_REQUEST, "FILE_DOWNLOAD_REQUEST" },
        { FILE_DOWNLOAD_CHUNK, "FILE_DOWNLOAD_CHUNK" },
        { FILE_DOWNLOAD_COMPLETE, "FILE_DOWNLOAD_COMPLETE" },
        { FILE_DELETE_REQUEST, "FILE_DELETE_REQUEST" },
        { FILE_DELETE_RESPONSE, "FILE_DELETE_RESPONSE" },

        { SUCCESS, "SUCCESS" },
        { ERROR, "ERROR" },
        { UNAUTHORIZED, "UNAUTHORIZED" }
    };

    // HashSet for O(1) validation checks
    private static readonly HashSet<int> ValidCommands = new(CommandNames.Keys);

    // Checks if the command code is valid
    public static bool IsValidCommandCode(int code) => ValidCommands.Contains(code);

    // Gets the name of a command
    public static string GetCommandName(int code) => CommandNames.TryGetValue(code, out var name) ? name : "UNKNOWN";

}