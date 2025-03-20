namespace FileLink.Server.Protocol;

// Represents a packet of data in the FileLink protocol.
// This class will form a basis of communication between client and server.

// TO DO: Add AES encryption (Stephan)
public class Packet
{
    public int CommandCode { get; set; }
    public Guid PacketId { get; set; }
    public string UserId  { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    
    // Gets/Sets the metadata dictionary for the packet
    // Contains additional information needed for processing the packet 
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    
    // Gets/Sets binary payload data of the packet
    // The content amd interpretation of this data depends on the command code
    public byte[]? Payload { get; set; }

    // Initializes a new instance of the Packet class with default values: ID and Timestamp
    public Packet()
    {
        PacketId = Guid.NewGuid();
        Timestamp = DateTime.Now;
    }

    // Initializes a new instance of the Packet class, but with the specified command code
    public Packet(int commandCode) : this()
    {
        CommandCode = commandCode;
    }

    // This creates a deep copy of the packet
    public Packet Clone()
    {
        var clone = new Packet()
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
            clone.Payload = new byte [this.Payload.Length];
            Array.Copy (this.Payload, clone.Payload, this.Payload.Length);
        }
        
        return clone;
    }
    
    // Creates response for the packet request. 
    // Sets the command code to the response code that corresponds to the request code.
    public Packet CreateResponse(int responseCommandCode)
    {
        var response = new Packet()
        {
            CommandCode = responseCommandCode,
            PacketId = Guid.NewGuid(),
            UserId = this.UserId,
            Timestamp = DateTime.Now,
        };
        
        // Add the original request packet ID to the metadata
        response.Metadata["RequestPacketId"] = this.PacketId.ToString();
        
        return response;
    }
    
    
    
}






















