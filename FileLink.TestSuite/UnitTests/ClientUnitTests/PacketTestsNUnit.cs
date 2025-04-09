using System.Text;
using FileLink.Client.Protocol;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.UnitTests.ClientUnitTests;

[TestFixture]

public class PacketTestsNUnit
{

    [Test]
    public void DefaultConstructor_InitializesProperties()
    {
        var packet = new Packet();
        
        Assert.That(packet.PacketId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(packet.Timestamp, Is.Not.EqualTo(default(DateTime)));
        Assert.That(packet.UserId, Is.EqualTo(string.Empty));
        Assert.That(packet.Metadata, Is.Not.Null);
        Assert.That(packet.Payload, Is.Null);
        Assert.That(packet.EncryptedPayload, Is.Null);

    }

    [Test]
    public void ParameterizedConstructor_InitializesProperties()
    {
        var packet = new Packet(100);
        Assert.That(packet.CommandCode, Is.EqualTo(100));

    }

    [Test]
    public void Clone_ReturnsDeepCopy()
    {
        var original = new Packet(101)
        {
            UserId = "testUser",
            Metadata = new Dictionary<string, string> { {"key1", "value1"} },
            Payload = Encoding.UTF8.GetBytes("testPayload"),
            EncryptedPayload = Encoding.UTF8.GetBytes("testEncryptedPayload")
            
        };
        
        var cloned = original.Clone();
        
        Assert.That(cloned, Is.Not.SameAs(original));
        Assert.That(cloned.CommandCode, Is.EqualTo(original.CommandCode));
        Assert.That(cloned.UserId, Is.EqualTo(original.UserId));
        Assert.That(cloned.PacketId, Is.EqualTo(original.PacketId));
        Assert.That(cloned.Timestamp, Is.EqualTo(original.Timestamp));
        
        //Deep copy check
        
        Assert.That(cloned.Metadata, Is.Not.SameAs(original.Metadata));
        Assert.That(cloned.Metadata["key1"], Is.EqualTo("value1"));
        
        Assert.That(cloned.Payload, Is.Not.Null);
        Assert.That(cloned.Payload, Is.Not.SameAs(original.Payload));
        CollectionAssert.AreEqual(original.Payload, cloned.Payload);

        Assert.That(cloned.EncryptedPayload, Is.Null);
        
    }

    [Test]
    public void IsSuccess_With_TrueString_Returns_False()
    {
        var packet = new Packet();
        packet.Metadata["Success"] = "false";
        
        Assert.That(packet.IsSuccess(), Is.False);
        
    }

    [Test]
    public void IsSuccess_With_TrueString_Returns_True()
    {
        var packet = new Packet();
        packet.Metadata["Success"] = "true";
        
        Assert.That(packet.IsSuccess(), Is.True);
        
    }

    [Test]
    public void GetMessage_InMetadata_ReturnsMessage()
    {
        var packet = new Packet();
        String msg = "Bahooie";
        packet.Metadata["Message"] = msg;
        
        Assert.That(packet.GetMessage(), Is.EqualTo(msg));
        
    }

    [Test]
    public void GetMessage_NoMessage_Returns_EmptyMessage()
    {
        var packet = new Packet();
        
        Assert.That(packet.GetMessage(), Is.EqualTo(string.Empty));
        
    }

    [Test]
    public void ToString_ReturnsFormattedString()
    {
        var packet = new Packet(212)
        {
            UserId = "testUser",
            Metadata = {{"key1", "value1"}},
            Payload = Encoding.UTF8.GetBytes("testPayload"),
        };

        var output = packet.ToString();
        
        Assert.That(output, Is.Not.Null.Or.Empty);
        StringAssert.Contains(output, "Packet[Code=");
        StringAssert.Contains(output, "UserId=");
        StringAssert.Contains(output, "Timestamp=");
        StringAssert.Contains(output, "MetadataCount=");
        StringAssert.Contains(output, "PayloadSize=");
    }

    [Test]
    public void EncryptedPayload_AllowsGetAndSet()
    {
        var packet = new Packet();
        var encryptedData = Encoding.UTF8.GetBytes("testEncryptedPayload");
        
        packet.EncryptedPayload = encryptedData;
        
        Assert.That(packet.EncryptedPayload, Is.EqualTo(encryptedData));

    }

}