using System;
using System.Collections.Generic;

namespace FileLink.Client.Protocol
{
   
    // Represents a packet of data in the cloud file server protocol.
    // Forms the basic unit of communication between client and server
    public class Packet
    {
       
        // Gets or sets the command code that identifies the purpose of this packet
        public int CommandCode { get; set; }

       
        // Gets or sets the unique identifier for this packet.
        // Used for tracking packets throughout the system and matching requests with responses
        public Guid PacketId { get; set; }

       
        // Gets or sets the user ID associated with this packet
        // Empty for unauthenticated packets
        public string UserId { get; set; } = string.Empty;

       
        // Gets or sets the timestamp when this packet was created
        public DateTime Timestamp { get; set; }

       
        // Gets or sets the metadata dictionary for the packet.
        // Contains additional information needed for processing the packet
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

       
        // Gets or sets the binary payload data of the packet.
        // The content and interpretation of this data depends on the command code
        public byte[]? Payload { get; set; }

       
        // Initializes a new instance of the Packet class with default values
        public Packet()
        {
            PacketId = Guid.NewGuid();
            Timestamp = DateTime.Now;
        }

       
        // Initializes a new instance of the Packet class with the specified command code
        public Packet(int commandCode) : this()
        {
            CommandCode = commandCode;
        }

       
        // Creates a deep copy of this packet
        public Packet Clone()
        {
            var clone = new Packet
            {
                CommandCode = this.CommandCode,
                PacketId = this.PacketId,
                UserId = this.UserId,
                Timestamp = this.Timestamp
            };

            // Deep copy metadata
            foreach (var entry in this.Metadata)
            {
                clone.Metadata[entry.Key] = entry.Value;
            }

            // Deep copy payload
            if (this.Payload != null)
            {
                clone.Payload = new byte[this.Payload.Length];
                Array.Copy(this.Payload, clone.Payload, this.Payload.Length);
            }

            return clone;
        }

       
        // Gets a value indicating whether the packet represents a successful operation
        public bool IsSuccess()
        {
            if (Metadata.TryGetValue("Success", out string? successStr))
            {
                return bool.TryParse(successStr, out bool success) && success;
            }
            return false;
        }

       
        // Gets the message associated with this packet, if available in the payload
        public string GetMessage()
        {
            if (Metadata.TryGetValue("Message", out string? message))
            {
                return message;
            }
            return string.Empty;
        }

       
        // Returns a string representation of this packet for debugging
        public override string ToString()
        {
            return $"Packet[Code={Commands.CommandCode.GetCommandName(CommandCode)}, ID={PacketId}, " +
                   $"UserId={UserId}, Timestamp={Timestamp}, " +
                   $"MetadataCount={Metadata.Count}, " +
                   $"PayloadSize={(Payload != null ? Payload.Length : 0)}]";
        }
    }
}