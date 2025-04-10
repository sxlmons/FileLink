using System.Text;
using FileLink.Client.Protocol;
using FileLink.Client.Services;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.UnitTests.ClientUnitTests;


[TestFixture]
public class NetworkServiceTestsNUnit
{
    private NetworkService _networkService;

    [SetUp]
    public void Setup()
    {
        _networkService = new NetworkService();
        _networkService.SetServer("localhost", 9000);
        
    }

    [Test]
    public async Task ConnectAsync_ReturnsTrue_WhenServerAvailable()
    {
        bool result = await _networkService.ConnectAsync();
        
        Assert.That(result, Is.True);
        Assert.That(_networkService.IsConnected, Is.True);
        
    }

    [Test]
    public async Task ConnectAsync_ReturnsFalse_WhenServerUnavailable()
    {
        _networkService.SetServer("localhost", 10000);
        
        bool result = await _networkService.ConnectAsync();
        
        Assert.That(result, Is.False);
        Assert.That(_networkService.IsConnected, Is.False);
        
    }

    [Test]
    public async Task IsConnected_IsTrue_AfterSuccessfulConnect()
    {
        await _networkService.ConnectAsync();
        Assert.That(_networkService.IsConnected, Is.True);
        
    }

    [Test]
    public async Task Disconnect_SetsIsConnectedFalse()
    {
        _networkService.Disconnect();
        Assert.That(_networkService.IsConnected, Is.False);
        
    }

    [Test]
    public void ResetConnection_DisposesClient()
    {
        _networkService.ResetConnection();
        Assert.That(_networkService.IsConnected, Is.False);
        
    }

    [Test]
    public async Task SendReceive_Async_ReturnsResponse_UponConnection()
    {
        var connected = await _networkService.ConnectAsync();

        var packet = new Packet
        {
            CommandCode = Commands.CommandCode.FILE_LIST_RESPONSE,
            UserId = "TestUser",
            Metadata = new Dictionary<string, string> {{"key1", "value1"}},
            Payload = Encoding.UTF8.GetBytes("Test Ping")
            
        };

        var response = await _networkService.SendAndReceiveAsync(packet);
        
        Assert.That(response, Is.Not.Null);
        Console.WriteLine("Response: " + response);

    }
    
    [Test]
    public async Task SendAndReceiveAsync_ReturnsNull_WhenServerNotAvailable()
    {
        _networkService.SetServer("localhost", 9999);
        var packet = new Packet { CommandCode = 100 };

        var response = await _networkService.SendAndReceiveAsync(packet);

        Assert.That(response, Is.Null);
    }

}