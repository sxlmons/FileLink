using System;

namespace FileLink.Client.Protocol
{
   
    // Contains definitions for all command codes used in the cloud file server protocol
  
    public static class Commands
    {
       
        // Contains static definitions for all command codes used in the protocol
      
        public static class CommandCode
        {
            // Authentication Commands (100-199)
           
            // Command code for login request from client
            public const int LOGIN_REQUEST = 100;

           
            // Command code for login response from server
            public const int LOGIN_RESPONSE = 101;

           
            // Command code for logout request from client
            public const int LOGOUT_REQUEST = 102;

           
            // Command code for logout response from server
            public const int LOGOUT_RESPONSE = 103;
            
           
            // Command code for account creation request from client
            public const int CREATE_ACCOUNT_REQUEST = 110;
            
           
            // Command code for account creation response from server
            public const int CREATE_ACCOUNT_RESPONSE = 111;

            // File Operations (200-299)
           
            // Command code for file list request from client
            public const int FILE_LIST_REQUEST = 200;

           
            // Command code for file list response from server
            public const int FILE_LIST_RESPONSE = 201;

           
            // Command code for file upload initialization request from client
            public const int FILE_UPLOAD_INIT_REQUEST = 210;

           
            // Command code for file upload initialization response from server
            public const int FILE_UPLOAD_INIT_RESPONSE = 211;

           
            // Command code for file upload chunk request from client
            public const int FILE_UPLOAD_CHUNK_REQUEST = 212;

           
            // Command code for file upload chunk response from server
            public const int FILE_UPLOAD_CHUNK_RESPONSE = 213;

           
            // Command code for file upload completion request from client
            public const int FILE_UPLOAD_COMPLETE_REQUEST = 214;

           
            // Command code for file upload completion response from server
            public const int FILE_UPLOAD_COMPLETE_RESPONSE = 215;

           
            // Command code for file download initialization request from client
            public const int FILE_DOWNLOAD_INIT_REQUEST = 220;

           
            // Command code for file download initialization response from server
            public const int FILE_DOWNLOAD_INIT_RESPONSE = 221;

           
            // Command code for file download chunk request from client
            public const int FILE_DOWNLOAD_CHUNK_REQUEST = 222;

           
            // Command code for file download chunk response from server
            public const int FILE_DOWNLOAD_CHUNK_RESPONSE = 223;

           
            // Command code for file download completion request from client
            public const int FILE_DOWNLOAD_COMPLETE_REQUEST = 224;

           
            // Command code for file download completion response from server
            public const int FILE_DOWNLOAD_COMPLETE_RESPONSE = 225;

           
            // Command code for file deletion request from client
            public const int FILE_DELETE_REQUEST = 230;

           
            // Command code for file deletion response from server
            public const int FILE_DELETE_RESPONSE = 231;

            // Status Responses (300-399)
           
            // Command code for general success response
            public const int SUCCESS = 300;

           
            // Command code for general error response
            public const int ERROR = 301;

           
            // Gets the string name of a command code for logging and debugging
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
                    SUCCESS => "SUCCESS",
                    ERROR => "ERROR",
                    _ => $"UNKNOWN({code})"
                };
            }

           
            /// Gets the corresponding response command code for a request command code
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
                    _ => ERROR
                };
            }
        }
    }
}