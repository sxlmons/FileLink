namespace FileLink.Server.Protocol;
public static class Commands 
{
// COMPLETE 
// Contains constants for all valid command codes used in the protocol.
// These codes are sent in packets to indicate the type of request or response.
    public static class CommandCode
    {
        // Auth Commands (100-199)
        public const int LOGIN_REQUEST = 100;
        public const int LOGIN_RESPONSE = 101;
        public const int LOGOUT_REQUEST = 102;
        public const int LOGOUT_RESPONSE = 103;
        public const int CREATE_ACCOUNT_REQUEST = 110;
        public const int CREATE_ACCOUNT_RESPONSE = 111;

        // File Commands (200-299)
        // File list commands 
        public const int FILE_LIST_REQUEST = 200;
        public const int FILE_LIST_RESPONSE = 201;

        // File upload commands 
        public const int FILE_UPLOAD_INIT_REQUEST = 210;
        public const int FILE_UPLOAD_INIT_RESPONSE = 211;
        public const int FILE_UPLOAD_CHUNK_REQUEST = 212;
        public const int FILE_UPLOAD_CHUNK_RESPONSE = 213;
        public const int FILE_UPLOAD_COMPLETE_REQUEST = 214;
        public const int FILE_UPLOAD_COMPLETE_RESPONSE = 215;

        // File download commands
        public const int FILE_DOWNLOAD_INIT_REQUEST = 220;
        public const int FILE_DOWNLOAD_INIT_RESPONSE = 221;
        public const int FILE_DOWNLOAD_CHUNK_REQUEST = 222;
        public const int FILE_DOWNLOAD_CHUNK_RESPONSE = 223;
        public const int FILE_DOWNLOAD_COMPLETE_REQUEST = 224;
        public const int FILE_DOWNLOAD_COMPLETE_RESPONSE = 225;
        
        // Directory Operations (240-251)
        public const int DIRECTORY_CREATE_REQUEST = 240;
        public const int DIRECTORY_CREATE_RESPONSE = 241;
        public const int DIRECTORY_LIST_REQUEST = 242;
        public const int DIRECTORY_LIST_RESPONSE = 243;
        public const int DIRECTORY_RENAME_REQUEST = 244;
        public const int DIRECTORY_RENAME_RESPONSE = 245;
        public const int DIRECTORY_DELETE_REQUEST = 246;
        public const int DIRECTORY_DELETE_RESPONSE = 247;
        public const int FILE_MOVE_REQUEST = 248;
        public const int FILE_MOVE_RESPONSE = 249;
        public const int DIRECTORY_CONTENTS_REQUEST = 250;
        public const int DIRECTORY_CONTENTS_RESPONSE = 251;

        // File delete commands
        public const int FILE_DELETE_REQUEST = 230;
        public const int FILE_DELETE_RESPONSE = 231;

        // Status responses (300-399)
        public const int SUCCESS = 300;
        public const int ERROR = 301;
        public const int UNAUTHORIZED = 302;

        // Dictionary for lookup
        public static string GetCommandName(int code)
        {
            return code switch
            {
                LOGIN_REQUEST => "LOGIN_REQUEST",
                LOGIN_RESPONSE => "LOGIN_RESPONSE",
                LOGOUT_REQUEST => "LOGOUT_REQUEST",
                LOGOUT_RESPONSE => "LOGOUT_RESPONSE",
                CREATE_ACCOUNT_REQUEST => "CREATE_ACCOUNT_REQUEST",
                CREATE_ACCOUNT_RESPONSE => "CREATE_ACCOUNT_RESPONSE",
                FILE_LIST_REQUEST => "FILE_LIST_REQUEST",
                FILE_LIST_RESPONSE => "FILE_LIST_RESPONSE",
                FILE_UPLOAD_INIT_REQUEST => "FILE_UPLOAD_INIT_REQUEST",
                FILE_UPLOAD_INIT_RESPONSE => "FILE_UPLOAD_INIT_RESPONSE",
                FILE_UPLOAD_CHUNK_REQUEST => "FILE_UPLOAD_CHUNK_REQUEST",
                FILE_UPLOAD_CHUNK_RESPONSE => "FILE_UPLOAD_CHUNK_RESPONSE",
                FILE_UPLOAD_COMPLETE_REQUEST => "FILE_UPLOAD_COMPLETE_REQUEST",
                FILE_UPLOAD_COMPLETE_RESPONSE => "FILE_UPLOAD_COMPLETE_RESPONSE",
                FILE_DOWNLOAD_INIT_REQUEST => "FILE_DOWNLOAD_INIT_REQUEST",
                FILE_DOWNLOAD_INIT_RESPONSE => "FILE_DOWNLOAD_INIT_RESPONSE",
                FILE_DOWNLOAD_CHUNK_REQUEST => "FILE_DOWNLOAD_CHUNK_REQUEST",
                FILE_DOWNLOAD_CHUNK_RESPONSE => "FILE_DOWNLOAD_CHUNK_RESPONSE",
                FILE_DOWNLOAD_COMPLETE_REQUEST => "FILE_DOWNLOAD_COMPLETE_REQUEST",
                FILE_DOWNLOAD_COMPLETE_RESPONSE => "FILE_DOWNLOAD_COMPLETE_RESPONSE",
                FILE_DELETE_REQUEST => "FILE_DELETE_REQUEST",
                FILE_DELETE_RESPONSE => "FILE_DELETE_RESPONSE",
                
                // Directory operation commands
                DIRECTORY_CREATE_REQUEST => "DIRECTORY_CREATE_REQUEST",
                DIRECTORY_CREATE_RESPONSE => "DIRECTORY_CREATE_RESPONSE",
                DIRECTORY_LIST_REQUEST => "DIRECTORY_LIST_REQUEST",
                DIRECTORY_LIST_RESPONSE => "DIRECTORY_LIST_RESPONSE",
                DIRECTORY_RENAME_REQUEST => "DIRECTORY_RENAME_REQUEST",
                DIRECTORY_RENAME_RESPONSE => "DIRECTORY_RENAME_RESPONSE",
                DIRECTORY_DELETE_REQUEST => "DIRECTORY_DELETE_REQUEST",
                DIRECTORY_DELETE_RESPONSE => "DIRECTORY_DELETE_RESPONSE",
                FILE_MOVE_REQUEST => "FILE_MOVE_REQUEST",
                FILE_MOVE_RESPONSE => "FILE_MOVE_RESPONSE",
                DIRECTORY_CONTENTS_REQUEST => "DIRECTORY_CONTENTS_REQUEST",
                DIRECTORY_CONTENTS_RESPONSE => "DIRECTORY_CONTENTS_RESPONSE",
                SUCCESS => "SUCCESS",
                ERROR => "ERROR",
                _ => $"UNKNOWN({code})"
            };
        }

        public static int GetResponseCommandCode(int requestCode)
        {
            return requestCode switch
            {
                LOGIN_REQUEST => LOGIN_RESPONSE,
                LOGOUT_REQUEST => LOGOUT_RESPONSE,
                CREATE_ACCOUNT_REQUEST => CREATE_ACCOUNT_RESPONSE,
                FILE_LIST_REQUEST => FILE_LIST_RESPONSE,
                FILE_UPLOAD_INIT_REQUEST => FILE_UPLOAD_INIT_RESPONSE,
                FILE_UPLOAD_CHUNK_REQUEST => FILE_UPLOAD_CHUNK_RESPONSE,
                FILE_UPLOAD_COMPLETE_REQUEST => FILE_UPLOAD_COMPLETE_RESPONSE,
                FILE_DOWNLOAD_INIT_REQUEST => FILE_DOWNLOAD_INIT_RESPONSE,
                FILE_DOWNLOAD_CHUNK_REQUEST => FILE_DOWNLOAD_CHUNK_RESPONSE,
                FILE_DOWNLOAD_COMPLETE_REQUEST => FILE_DOWNLOAD_COMPLETE_RESPONSE,
                FILE_DELETE_REQUEST => FILE_DELETE_RESPONSE,
                // Directory operation commands
                DIRECTORY_CREATE_REQUEST => DIRECTORY_CREATE_RESPONSE,
                DIRECTORY_LIST_REQUEST => DIRECTORY_LIST_RESPONSE,
                DIRECTORY_RENAME_REQUEST => DIRECTORY_RENAME_RESPONSE,
                DIRECTORY_DELETE_REQUEST => DIRECTORY_DELETE_RESPONSE,
                FILE_MOVE_REQUEST => FILE_MOVE_RESPONSE,
                DIRECTORY_CONTENTS_REQUEST => DIRECTORY_CONTENTS_RESPONSE,
                _ => ERROR
            };
        }
    }
}