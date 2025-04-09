using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileLink.Client.Protocol
{
   
    // Handles the binary serialization and deserialization of packets
    public class PacketSerializer
    {
        // Protocol versioning
        private const byte PROTOCOL_VERSION = 1;
        
        // Header Structure:
        // - Protocol Version (1 byte)
        // - Command Code (4 bytes)
        // - Packet ID (16 bytes)
        // - User ID Length (4 bytes)
        // - User ID (variable)
        // - Timestamp (8 bytes)
        // - Metadata Count (4 bytes)
        // - Metadata Key-Value Pairs (variable)
        // - Payload Length (4 bytes)
        // - Payload (variable)

       
        // Serializes a packet into a byte array
        public byte[] Serialize(Packet packet)
        {
            try
            {
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);

                // Write protocol version
                writer.Write(PROTOCOL_VERSION);

                // Write command code
                writer.Write(packet.CommandCode);

                // Write packet ID
                writer.Write(packet.PacketId.ToByteArray());

                // Write user ID
                byte[] userIdBytes = Encoding.UTF8.GetBytes(packet.UserId ?? string.Empty);
                writer.Write(userIdBytes.Length);
                writer.Write(userIdBytes);

                // Write timestamp (as ticks)
                writer.Write(packet.Timestamp.Ticks);

                // Write metadata
                writer.Write(packet.Metadata?.Count ?? 0);
                if (packet.Metadata != null)
                {
                    foreach (var kvp in packet.Metadata)
                    {
                        byte[] keyBytes = Encoding.UTF8.GetBytes(kvp.Key);
                        byte[] valueBytes = Encoding.UTF8.GetBytes(kvp.Value);

                        writer.Write(keyBytes.Length);
                        writer.Write(keyBytes);
                        writer.Write(valueBytes.Length);
                        writer.Write(valueBytes);
                    }
                }

                // Write payload
                if (packet.Payload != null &&  packet.Payload.Length > 0)
                {
                    // Add packet type differentiator 

                    if (packet.CommandCode == Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST) 
                    {
                        
                        packet.EncryptedPayload = EncryptPayload(packet.Payload); // Encrypting packet payload yo
                        writer.Write(packet.EncryptedPayload.Length);
                        writer.Write(packet.EncryptedPayload); // Writing the encrypted payload 
                        
                    }
                    else
                    {
                        writer.Write(packet.Payload.Length);
                        writer.Write(packet.Payload);
                        
                    }

                }
                else
                {
                    writer.Write(0);
                }

                return ms.ToArray();
            }
            catch (Exception ex)
            {
                throw new ProtocolException("Error serializing packet", ex);
            }
        }
        
        private static byte[] EncryptPayload(byte[] data) 
        { // Encrypting payload 

            using (Aes aes = Aes.Create()) { // Using AES encryption as a baseline 
            
                aes.GenerateKey(); // Generating key
                aes.GenerateIV();

                byte[] encryptedData;

                using (ICryptoTransform encryptor = aes.CreateEncryptor()) { // Calling encryptor to perform AES
                
                    encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
                }
                
                byte[] finalData = new byte[aes.Key.Length + aes.IV.Length + encryptedData.Length]; // creating the final data payload 

                Buffer.BlockCopy(aes.Key, 0, finalData, 0, aes.Key.Length);
                Buffer.BlockCopy(aes.IV, 0, finalData, aes.Key.Length, aes.IV.Length);
                Buffer.BlockCopy(encryptedData, 0, finalData, aes.Key.Length + aes.IV.Length, encryptedData.Length);
            
                return finalData;
            }
        
        }
        
        public Packet Deserialize(byte[] data)
        {
            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms);

                var packet = new Packet();

                // Read protocol version
                byte version = reader.ReadByte();
                if (version != PROTOCOL_VERSION)
                {
                    throw new ProtocolException($"Unsupported protocol version: {version}");
                }

                // Read command code
                packet.CommandCode = reader.ReadInt32();

                // Read packet ID
                byte[] packetIdBytes = reader.ReadBytes(16);
                packet.PacketId = new Guid(packetIdBytes);

                // Read user ID
                int userIdLength = reader.ReadInt32();
                byte[] userIdBytes = reader.ReadBytes(userIdLength);
                packet.UserId = Encoding.UTF8.GetString(userIdBytes);

                // Read timestamp
                long timestampTicks = reader.ReadInt64();
                packet.Timestamp = new DateTime(timestampTicks);

                // Read metadata
                int metadataCount = reader.ReadInt32();
                packet.Metadata = new Dictionary<string, string>(metadataCount);
                for (int i = 0; i < metadataCount; i++)
                {
                    int keyLength = reader.ReadInt32();
                    byte[] keyBytes = reader.ReadBytes(keyLength);
                    string key = Encoding.UTF8.GetString(keyBytes);

                    int valueLength = reader.ReadInt32();
                    byte[] valueBytes = reader.ReadBytes(valueLength);
                    string value = Encoding.UTF8.GetString(valueBytes);

                    packet.Metadata[key] = value;
                }

                // Read payload
                int payloadLength = reader.ReadInt32();
                if (payloadLength > 0)
                {
                    bool encryptedCommands = packet.CommandCode == Commands.CommandCode.FILE_DOWNLOAD_CHUNK_REQUEST;
                    
                    byte[] tempData = reader.ReadBytes(payloadLength);
                    
                    if (encryptedCommands) // Decrypting packet payload 
                    { 
                        packet.EncryptedPayload = tempData;
                        packet.Payload = DecryptPayload(tempData);
                        
                    } else
                    {
                        packet.Payload = tempData; // if the packet is never encrypted to begin with, it will be stored in packet.Payload
                    }
                    
                }

                return packet;
            }
            catch (Exception ex) when (!(ex is ProtocolException))
            {
                throw new ProtocolException("Error deserializing packet", ex);
            }
        }
        
        private static byte[] DecryptPayload(byte[] data) 
        { // Decrypting payload 

            using (Aes aes = Aes.Create()) { // Creating new AES instance 
                
                int aesKeySize = aes.KeySize / 8;
                int ivSize = aes.BlockSize / 8;
                
                byte[] key = new byte[aesKeySize];
                byte[] iv = new byte[ivSize];
                byte[] encryptedData = new byte[data.Length - aesKeySize - ivSize];
                
                Buffer.BlockCopy(data, 0, key, 0, aesKeySize);
                Buffer.BlockCopy(data, aesKeySize, iv, 0, ivSize);
                Buffer.BlockCopy(data, aesKeySize + ivSize, encryptedData, 0, encryptedData.Length);
                
                aes.Key = key;
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor()) { // Adjusting offsets to ensure integrity of packet 
                
                    return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                
                }
            
            }
        
        }
       
        // Serializes a packet including the length prefix
        public byte[] SerializeWithLengthPrefix(Packet packet)
        {
            var packetData = Serialize(packet);
            var result = new byte[4 + packetData.Length];
            
            // Write length prefix
            BitConverter.GetBytes(packetData.Length).CopyTo(result, 0);
            
            // Write packet data
            packetData.CopyTo(result, 4);
            
            return result;
        }

       
        // Reads a packet from a stream, including the length prefix
        public async Task<Packet> ReadPacketFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            // Read length prefix (4 bytes)
            byte[] lengthBuffer = new byte[4];
            int bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
            if (bytesRead < 4)
            {
                throw new ProtocolException("Failed to read packet length prefix");
            }

            int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (packetLength <= 0 || packetLength > 100 * 1024 * 1024) // Limit to 100MB for safety
            {
                throw new ProtocolException($"Invalid packet length: {packetLength}");
            }

            // Read packet data
            byte[] packetBuffer = new byte[packetLength];
            int totalBytesRead = 0;
            while (totalBytesRead < packetLength)
            {
                int bytesRemaining = packetLength - totalBytesRead;
                bytesRead = await stream.ReadAsync(packetBuffer, totalBytesRead, bytesRemaining, cancellationToken);
                
                if (bytesRead == 0)
                {
                    throw new ProtocolException("Connection closed while reading packet data");
                }
                
                totalBytesRead += bytesRead;
            }

            // Deserialize the packet
            return Deserialize(packetBuffer);
        }

       
        // Writes a packet to a stream, including the length prefix
        public async Task WritePacketToStreamAsync(Stream stream, Packet packet, CancellationToken cancellationToken = default)
        {
            var packetData = Serialize(packet);
            
            // Write length prefix
            await stream.WriteAsync(BitConverter.GetBytes(packetData.Length), 0, 4, cancellationToken);
            
            // Write packet data
            await stream.WriteAsync(packetData, 0, packetData.Length, cancellationToken);
            
            // Flush the stream
            await stream.FlushAsync(cancellationToken);
        }
    }

   
    // Exception thrown when there's an error in protocol serialization or deserialization
    public class ProtocolException : Exception
    {
        public ProtocolException(string message) : base(message) { }
        public ProtocolException(string message, Exception innerException) : base(message, innerException) { }
    }
}