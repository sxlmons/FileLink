using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace FileLink.Shared;

public class FileTransfer
{

    private const int PacketSize = 1460; // Standard TCP Segment
    private const int HeaderSize = 10;
    private const int PayloadSize = 1400;
    private const int ReservedSize = 50;
    
    static void SendFile(NetworkStream stream, string filePath, RSA rsa)
    { // SendFile is called by both Client and Server 

        byte[] fileData = File.ReadAllBytes(filePath);
        byte[] encryptedData = EncryptPayload(fileData, rsa);
        
        byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
        byte[] header = new byte[HeaderSize];
        byte[] reserved = new byte[ReservedSize];
        
        BitConverter.GetBytes(fileNameBytes.Length).CopyTo(header, 0);
        BitConverter.GetBytes(encryptedData.Length).CopyTo(header, 4);
        
        stream.Write(header, 0, header.Length);
        stream.Write(reserved, 0, reserved.Length);
        stream.Write(encryptedData, 0, encryptedData.Length);
        
    }

    static void ReceiveFile(NetworkStream stream, string saveDirectory, RSA rsa) {
        
        byte[] header = new byte[HeaderSize];
        byte[] reserved = new byte[ReservedSize];
        
        stream.ReadExactly(header, 0, HeaderSize);
        stream.ReadExactly(reserved, 0, ReservedSize);
        
        int fileSize = BitConverter.ToInt32(header, 4);
        byte[] encryptedData = new byte[fileSize];

        int totalBytesRead = 0;
        while (totalBytesRead < fileSize) {
            
            int bytesRead = stream.Read(encryptedData, totalBytesRead, fileSize - totalBytesRead);
            
            if (bytesRead == 0) {
                break;
            }

            totalBytesRead += bytesRead;
        }
        
        byte[] decryptedData = DecryptPayload(encryptedData, rsa);

        string savePath = Path.Combine(saveDirectory, "received.dec");
        File.WriteAllBytes(savePath, decryptedData);

    }

    static void RequestFile(NetworkStream stream, string fileName) {
        
        byte[] request = Encoding.UTF8.GetBytes("GET: " + fileName);
        stream.Write(request, 0, request.Length);
        
    }

    static void HandleRequest(NetworkStream stream, string storagePath, RSA rsa)
    {

        byte[] buffer = new byte[256];
        int bytesRead = stream.Read(buffer, 0, 256);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        if (request.StartsWith("GET")) {
            
            string fileName = request.Substring(4);
            string filePath = Path.Combine(storagePath, fileName + ".enc");

            if (File.Exists(filePath)) {
                
                SendFile(stream, filePath, rsa);
                
            }
        }
    }

    private static byte[] EncryptPayload(byte[] data, RSA rsa) {

        using (Aes aes = Aes.Create()) {
            
            aes.GenerateKey();
            aes.GenerateIV();

            byte[] encryptedData;

            using (ICryptoTransform encryptor = aes.CreateEncryptor()) {
                
                encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
                
            }
            
            byte[] encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);
            byte[] finalData = new byte[encryptedKey.Length + aes.IV.Length + encryptedData.Length];

            encryptedKey.CopyTo(finalData, 0);
            aes.IV.CopyTo(finalData, encryptedKey.Length);
            encryptedData.CopyTo(finalData, encryptedKey.Length + aes.IV.Length);
            
            return finalData;
        }
        
    }

    private static byte[] DecryptPayload(byte[] data, RSA rsa) {

        using (Aes aes = Aes.Create())
        {
            byte[] encryptedKey = new byte[rsa.KeySize / 8];
            byte[] iv = new byte[16];
            
            Array.Copy(data, 0, encryptedKey, 0, encryptedKey.Length);
            Array.Copy(data, encryptedKey.Length, iv, 0, iv.Length);
            
            byte[] decryptedKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);
            aes.Key = decryptedKey;
            aes.IV = iv;

            using (ICryptoTransform decryptor = aes.CreateDecryptor()) {
                
                return decryptor.TransformFinalBlock(data, encryptedKey.Length + iv.Length, data.Length - encryptedKey.Length - iv.Length);
                
            }
            
        }
        
    }
    
}



    
