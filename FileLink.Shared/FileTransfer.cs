using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace FileLink.Shared;

public class FileTransfer
{
    
    private const int HeaderSize = 10; // Header size
    private const int PayloadSize = 1400; // Payload size
    private const int ReservedSize = 50; // Reserved data size
    // PacketSize = HeaderSize + PayloadSize + ReservedSize;
    
    static void SendFile(NetworkStream stream, string filePath, RSA rsa)
    { // SendFile is called by Client

        byte[] fileData = File.ReadAllBytes(filePath); // Reading all bytes from selected file
        byte[] encryptedData = EncryptPayload(fileData, rsa); // Calling encryptor to chunk file

        byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
        byte[] header = new byte[HeaderSize]; // Creating Header byte
        byte[] reserved = new byte[ReservedSize]; // Creating Reserved byte

        BitConverter.GetBytes(fileNameBytes.Length).CopyTo(header, 0); // Copying header information to byte position in array
        BitConverter.GetBytes(encryptedData.Length).CopyTo(header, 4);
        
        stream.Write(header, 0, header.Length); // Writing header and reserved bytes to tcp stream 
        stream.Write(reserved, 0, reserved.Length);

        int offset = 0;

        while (offset < encryptedData.Length) { // Ensuring full payload is sent in 1460 byte packets 
            
            int chunkSize = Math.Min(PayloadSize, encryptedData.Length - offset);
            byte[] payload = new byte[chunkSize];
            Array.Copy(encryptedData, offset, payload, 0, chunkSize);
            stream.Write(payload, 0, chunkSize);
            offset += chunkSize;
            
        }
        
    }

    static void SendFileServer(NetworkStream stream, string filePath) 
    { // SendFileServer is called by Server
       
        byte[] fileData = File.ReadAllBytes(filePath); // Reading all bytes from selected file
        
        byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
        byte[] header = new byte[HeaderSize]; // Creating Header byte
        byte[] reserved = new byte[ReservedSize]; // Creating Reserved byte
        
        BitConverter.GetBytes(fileNameBytes.Length).CopyTo(header, 0);
        BitConverter.GetBytes(fileData.Length).CopyTo(header, 4);
        
        stream.Write(header, 0, header.Length); // Writing header and reserved bytes to tcp stream 
        stream.Write(reserved, 0, reserved.Length);
        
        int offset = 0;

        while (offset < fileData.Length) { // Ensuring full payload is sent in 1460 byte packets 
            
            int chunkSize = Math.Min(PayloadSize, fileData.Length - offset);
            byte[] payload = new byte[chunkSize];
            Array.Copy(fileData, offset, payload, 0, chunkSize);
            stream.Write(payload, 0, chunkSize);
            offset += chunkSize;
            
        }
        
    }

    static void ReceiveFile(NetworkStream stream, string saveDirectory, RSA rsa) 
    { // ReceiveFile is called by Client
        
        byte[] header = new byte[HeaderSize]; // Creating Header byte
        byte[] reserved = new byte[ReservedSize]; // Creating Reserved byte
        
        stream.ReadExactly(header, 0, HeaderSize); // Reading both header and reserved information from stream
        stream.ReadExactly(reserved, 0, ReservedSize);
        
        int fileSize = BitConverter.ToInt32(header, 4);
        byte[] encryptedData = new byte[fileSize]; // Creating the encrypted data byte

        int totalBytesRead = 0;
        while (totalBytesRead < fileSize) { // Ensuring entire file is received correctly
            
            int bytesRead = stream.Read(encryptedData, totalBytesRead, fileSize - totalBytesRead); // Reading from stream
            
            if (bytesRead == 0) {
                break;
            }

            totalBytesRead += bytesRead;
        }
        
        byte[] decryptedData = DecryptPayload(encryptedData, rsa); // Calling decryptor function 

        string savePath = Path.Combine(saveDirectory, "received.dec"); // Saving to a file 
        File.WriteAllBytes(savePath, decryptedData);

    }

    static void ReceiveFileServer(NetworkStream stream, string saveDirectory)
    { // ReceiveFileServer is called by Server
        
        byte[] header = new byte[HeaderSize]; // Creating Header byte
        byte[] reserved = new byte[ReservedSize]; // Creating Reserved byte
        
        stream.ReadExactly(header, 0, HeaderSize); // Reading both header and reserved information from stream
        stream.ReadExactly(reserved, 0, ReservedSize);
        
        int fileSize = BitConverter.ToInt32(header, 4);
        byte[] data = new byte[fileSize];
        
        int totalBytesRead = 0;
        while (totalBytesRead < fileSize) { // Ensuring entire file is received correctly
            
            int  bytesRead = stream.Read(data, totalBytesRead, fileSize - totalBytesRead);

            if (bytesRead == 0) {
                break;
            }
            
            totalBytesRead += bytesRead;
            
        }
        
        string savePath = Path.Combine(saveDirectory, "received.dec"); // Saving to a file
        File.WriteAllBytes(savePath, data);
        
    }

    static void RequestFile(NetworkStream stream, string fileName) 
    { // Allows Client to ask for a file from the server 
        
        byte[] request = Encoding.UTF8.GetBytes("GET: " + fileName);
        stream.Write(request, 0, request.Length);
        
    }

    static void HandleRequest(NetworkStream stream, string storagePath, RSA rsa)
    { // Checks for incoming requests and sends files back 

        byte[] buffer = new byte[256]; // Buffer for request information 
        int bytesRead = stream.Read(buffer, 0, 256);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        if (request.StartsWith("GET_SERVER:")) { // Server sends a file request for Client
            
            string fileName = request.Substring(11);
            string filePath = Path.Combine(storagePath, fileName + ".enc");

            if (File.Exists(filePath)) {
                
                SendFile(stream, filePath, rsa);
                
            }
            
        } else if (request.StartsWith("GET_CLIENT:")) { // Client sends a file request for Server 
            
            string fileName = request.Substring(11);
            string filePath = Path.Combine(storagePath, fileName + ".dec");

            if (File.Exists(filePath)) {
                
                SendFileServer(stream, filePath);
                
            }

        }
        
    }

    private static byte[] EncryptPayload(byte[] data, RSA rsa) 
    { // Encrypting payload 

        using (Aes aes = Aes.Create()) { // Using AES encryption as a baseline 
            
            aes.GenerateKey(); // Generating key
            aes.GenerateIV();

            byte[] encryptedData;

            using (ICryptoTransform encryptor = aes.CreateEncryptor()) { // Calling encryptor to perform AES
                
                encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
                
            }
            
            byte[] encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1); // Adding padding
            byte[] finalData = new byte[encryptedKey.Length + aes.IV.Length + encryptedData.Length]; // creating the final data payload 

            encryptedKey.CopyTo(finalData, 0); // Placing everything in finalData and returning to SendFile() 
            aes.IV.CopyTo(finalData, encryptedKey.Length);
            encryptedData.CopyTo(finalData, encryptedKey.Length + aes.IV.Length);
            
            return finalData;
        }
        
    }

    private static byte[] DecryptPayload(byte[] data, RSA rsa) 
    { // Decrypting payload 

        using (Aes aes = Aes.Create()) { // Creating new AES instance 
            
            byte[] encryptedKey = new byte[rsa.KeySize / 8];
            byte[] iv = new byte[16];
            
            Array.Copy(data, 0, encryptedKey, 0, encryptedKey.Length);
            Array.Copy(data, encryptedKey.Length, iv, 0, iv.Length);
            
            byte[] decryptedKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1); // Removing padding and using key to decrypt 
            aes.Key = decryptedKey;
            aes.IV = iv;

            using (ICryptoTransform decryptor = aes.CreateDecryptor()) { // Adjusting offsets to ensure integrity of packet 
                
                return decryptor.TransformFinalBlock(data, encryptedKey.Length + iv.Length, data.Length - encryptedKey.Length - iv.Length);
                
            }
            
        }
        
    }
    
}



    
