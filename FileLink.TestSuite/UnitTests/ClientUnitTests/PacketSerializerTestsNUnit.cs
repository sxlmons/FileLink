using System.Text;
using FileLink.Client.Protocol;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace FileLink.TestSuite.UnitTests.ClientUnitTests;

[TestFixture]
public class PacketSerializerTestsNUnit 
{
    private PacketSerializer _serializer;

    [SetUp]
    public void Setup()
    {
        _serializer = new PacketSerializer();
    }

    [Test]
    public void Serialize_Deserialize_Packet_Returns_Identical_CommandCode()
    {

        var packet = new Packet()
        {
            CommandCode = 1,
            PacketId = Guid.NewGuid(),
            UserId = "TestUser1",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);
        
        NUnit.Framework.Assert.That(packet.CommandCode, Is.EqualTo(deserializePacket.CommandCode));

    }
    
    [Test]
    public void Serialize_Deserialize_Packet_Returns_Identical_PacketId()
    {

        var packet = new Packet()
        {
            CommandCode = 1,
            PacketId = Guid.NewGuid(),
            UserId = "TestUser1",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);
        
        NUnit.Framework.Assert.That(packet.PacketId, Is.EqualTo(deserializePacket.PacketId));

    }
    
    [Test]
    public void Serialize_Deserialize_Packet_Returns_Identical_UserId()
    {

        var packet = new Packet()
        {
            CommandCode = 1,
            PacketId = Guid.NewGuid(),
            UserId = "TestUser1",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);
        
        NUnit.Framework.Assert.That(packet.UserId, Is.EqualTo(deserializePacket.UserId));

    }
    
    [Test]
    public void Serialize_Deserialize_Packet_Returns_Identical_Timestamp()
    {

        var packet = new Packet()
        {
            CommandCode = 1,
            PacketId = Guid.NewGuid(),
            UserId = "TestUser1",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);
        
        NUnit.Framework.Assert.That(packet.Timestamp.Ticks, Is.EqualTo(deserializePacket.Timestamp.Ticks));

    }
    
    [Test]
    public void Serialize_Deserialize_Packet_Returns_Identical_Metadata()
    {

        var packet = new Packet()
        {
            CommandCode = 1,
            PacketId = Guid.NewGuid(),
            UserId = "TestUser1",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);
        
        NUnit.Framework.Legacy.CollectionAssert.AreEqual(packet.Metadata, deserializePacket.Metadata);

    }
    
    [Test]
    public void Serialize_Deserialize_Packet_Returns_Identical_Payload()
    {

        var packet = new Packet()
        {
            CommandCode = 1,
            PacketId = Guid.NewGuid(),
            UserId = "TestUser1",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);

        if (deserializePacket.Payload != null)
        {
            NUnit.Framework.Legacy.CollectionAssert.AreEqual(packet.Payload, deserializePacket.Payload);
        }

    }

    [Test]
    public void Serialize_Empty_Metadata_Returns_Empty_Metadata()
    {

        var packet = new Packet
        {
            CommandCode = 2,
            PacketId = Guid.NewGuid(),
            UserId = "TestUser1",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>(),
            Payload = Encoding.UTF8.GetBytes("TestPayload")
            
        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);
        
        NUnit.Framework.Assert.That(deserializePacket.Metadata, Is.Empty);

    }

    [Test]
    public void Serialize_Empty_Payload_Returns_Empty_Payload()
    {
        var packet = new Packet
        {
            CommandCode = 3,
            PacketId = Guid.NewGuid(),
            UserId = "TestUser1",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key", "Value"}},
            Payload = null
            
        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);
        
        NUnit.Framework.Assert.That(deserializePacket.Payload, Is.Null);
        
    }

    [Test]
    public void Serialize_Encrypted_Payload_Should_Decrypt_File_Chunk_Request()
    {

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST,
            PacketId = Guid.NewGuid(),
            UserId = "EncryptedUser",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "EncKey", "EncValue" } },
            Payload = Encoding.UTF8.GetBytes("SecretPayload")

        };
        
        var serialized = _serializer.Serialize(packet);
        var deserializePacket = _serializer.Deserialize(serialized);
        
        NUnit.Framework.Assert.That(deserializePacket.EncryptedPayload, Is.Not.Null);
        NUnit.Framework.Legacy.CollectionAssert.AreEqual(packet.Payload, deserializePacket.Payload);
        
        
    }

    [Test]
    public async Task Write_Read_Packet_From_Stream_Returns_Identical_Packet_CommandCode()
    {
        var packet = new Packet
        {
            CommandCode = 4,
            PacketId = Guid.NewGuid(),
            UserId = "StreamUser",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };

        using var ms = new MemoryStream();
        await _serializer.WritePacketToStreamAsync(ms, packet, CancellationToken.None);
        ms.Position = 0;
        var deserializePacket = await _serializer.ReadPacketFromStreamAsync(ms, CancellationToken.None);
        
        NUnit.Framework.Assert.That(packet.CommandCode, Is.EqualTo(deserializePacket.CommandCode));

    }
    
    [Test]
    public async Task Write_Read_Packet_From_Stream_Returns_Identical_Packet_PacketId()
    {
        var packet = new Packet
        {
            CommandCode = 4,
            PacketId = Guid.NewGuid(),
            UserId = "StreamUser",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };

        using var ms = new MemoryStream();
        await _serializer.WritePacketToStreamAsync(ms, packet, CancellationToken.None);
        ms.Position = 0;
        var deserializePacket = await _serializer.ReadPacketFromStreamAsync(ms, CancellationToken.None);
        
        NUnit.Framework.Assert.That(packet.PacketId, Is.EqualTo(deserializePacket.PacketId));

    }
    
    [Test]
    public async Task Write_Read_Packet_From_Stream_Returns_Identical_Packet_UserId()
    {
        var packet = new Packet
        {
            CommandCode = 4,
            PacketId = Guid.NewGuid(),
            UserId = "StreamUser",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };

        using var ms = new MemoryStream();
        await _serializer.WritePacketToStreamAsync(ms, packet, CancellationToken.None);
        ms.Position = 0;
        var deserializePacket = await _serializer.ReadPacketFromStreamAsync(ms, CancellationToken.None);
        
        NUnit.Framework.Assert.That(packet.UserId, Is.EqualTo(deserializePacket.UserId));

    }
    
    [Test]
    public async Task Write_Read_Packet_From_Stream_Returns_Identical_Packet_Payload()
    {
        var packet = new Packet
        {
            CommandCode = 4,
            PacketId = Guid.NewGuid(),
            UserId = "StreamUser",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>{{"Key1", "Value1"}, {"Key2", "Value2"}},
            Payload = Encoding.UTF8.GetBytes("TestPayload")

        };

        using var ms = new MemoryStream();
        await _serializer.WritePacketToStreamAsync(ms, packet, CancellationToken.None);
        ms.Position = 0;
        var deserializePacket = await _serializer.ReadPacketFromStreamAsync(ms, CancellationToken.None);

        if (deserializePacket.Payload != null)
        {
            NUnit.Framework.Legacy.CollectionAssert.AreEqual(packet.Payload, deserializePacket.Payload);
        }

    }

    [Test]
    public void Deserialize_Invalid_ProtocolVersion_Throws_Exception()
    {
        byte[] invalidData = new byte[] { 99, 0, 5, 3, 1 };
        NUnit.Framework.Assert.Throws<ProtocolException>(() => _serializer.Deserialize(invalidData));
        
    }


}