//-----------------------------------------------------------------------------
// Copyright (c) 2025 FileLink Project. All rights reserved.
// Licensed under the MIT License.
//-----------------------------------------------------------------------------
//
// PacketSerializer.cs
//
// This class handles the binary serialization and deserialization of network packets,
// converting Packet objects to byte arrays and vice versa. The serializer maintains a specific
// byte order for all packet components:
//
// 1. Protocol Version (1 byte): Used for backward compatibility if the packet format changes
// 2. Command Code (4 bytes): Identifies the operation type (login, file list, etc.)
// 3. Packet ID (16 bytes): UUID that uniquely identifies each packet for tracking, debugging, and matching requests with responses
// 4. User ID (variable length): Identifies the authenticated user for authorization checks
// 5. Timestamp (8 bytes): Records when the packet was created for monitoring and timeouts
// 6. Metadata Count (4 bytes): Number of key-value pairs in the metadata dictionary
// 7. Metadata Key-Value Pairs: Variable length strings with prefixed length indicators
// 8. Payload Length (4 bytes): Size of the binary payload
// 9. Payload (variable): Command-specific data (e.g., file contents, JSON-serialized objects)
//
// Error handling is implemented by wrapping operations in try-catch blocks and throwing
// ProtocolExceptions with meaningful messages. The class validates the protocol version
// during deserialization to reject incompatible packet formats.
//-----------------------------------------------------------------------------

using System.Text;
using FileLink.Server.Core.Exceptions;

namespace FileLink.Server.Protocol;

// Handles the binary serialization and deserialization of packets yo
public class PacketSerializer
{
    // Protocol Versioning 
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
            if (packet.Payload != null)
            {
                writer.Write(packet.Payload.Length);
                writer.Write(packet.Payload);
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

    
    // Deserializes a byte array into a packet
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
                packet.Payload = reader.ReadBytes(payloadLength);
            }

            return packet;
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            throw new ProtocolException("Error deserializing packet", ex);
        }
    }
}