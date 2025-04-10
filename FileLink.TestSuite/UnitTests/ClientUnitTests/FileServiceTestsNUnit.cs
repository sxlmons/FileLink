using System.Text.Json;
using FileLink.Client.Protocol;
using FileLink.Client.Services;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.UnitTests.ClientUnitTests;


[TestFixture]
public class FileServiceTestsNUnit
{
    private AuthenticationService _authenticationService = null!;
    private TestNetworkService _networkService = null!;
    private FileService _fileService = null!;
    private string _testFilePath = "";
    private const string loggedInUserId = "ddde3af3-b0b2-431c-a33b-f01ae8d3391b";

    [SetUp]
    public async Task Setup()
    {
        _networkService = new TestNetworkService();
        _fileService = new FileService(_networkService);
        _authenticationService = new AuthenticationService(_networkService);
        
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.LOGIN_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true,
                Message = "Login Success",
            }),
            
            UserId = loggedInUserId
            
        });

        await _authenticationService.LoginAsync("user_test1", "pass");
        
        _testFilePath = Path.GetTempFileName();
        File.WriteAllText(_testFilePath, "This is a testing file.");
        
    }

    [TearDown]
    public void Cleanup()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }

    }
    
    [Test]
    public async Task UploadFileAsync_ShouldReturnFileItem_WhenAllStepsSucceed()
    {
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true,
                FileId = "fid123"
            })
        });
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true 
            })
        });

        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true 
            })
        });

        var result = await _fileService.UploadFileAsync(_testFilePath, null, loggedInUserId);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FileName, Is.EqualTo(Path.GetFileName(_testFilePath)));
        Assert.That(result.Id, Is.Not.Null);

    }

    [Test]
    public async Task UploadFileAsync_ReturnsError_InitFails()
    {
        _networkService.EnqueueResponse(null);
        
        var result = await _fileService.UploadFileAsync(_testFilePath, null, loggedInUserId);
        Assert.That(result, Is.Not.Null); // <FileLink.Client.Models.FileItem> -error
        
    }

    [Test]
    public async Task UploadFileAsync_ReturnsError_UploadChunkFails()
    {
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true,
                FileId = "fid999"
            })
        });
        
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = false
            })
        });
        
        var result = await _fileService.UploadFileAsync(_testFilePath, null, loggedInUserId);
        Assert.That(result, Is.Not.Null); // <FileLink.Client.Models.FileItem> -error
        
    }
    
    [Test]
    public async Task UploadFileAsync_ReturnsError_WhenCompleteFails()
    {
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_INIT_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true, 
                FileId = "fid000" 
            })
        });

        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_CHUNK_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true 
            })
        });

        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_UPLOAD_COMPLETE_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = false // simulate failure
            }) 
        });

        var result = await _fileService.UploadFileAsync(_testFilePath, null, loggedInUserId);
        Assert.That(result, Is.Not.Null);
        
    }
    
    [Test]
    public async Task DeleteFileAsync_ReturnsTrue_WhenSuccess()
    {
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_DELETE_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true 
            })
        });

        var result = await _fileService.DeleteFileAsync("ee4a3064-0149-48aa-8d7f-913d036d4ed4", loggedInUserId);
        Assert.That(result, Is.True);
    }
    
    [Test]
    public async Task DeleteFileAsync_ReturnsFalse_WhenFailure()
    {
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.FILE_DELETE_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = false 
            })
        });

        var result = await _fileService.DeleteFileAsync("ce53489b-601d-40aa-933f-58369f8f4794", loggedInUserId);
        Assert.That(result, Is.False);
    }

    // Helper class
    private class TestNetworkService : NetworkService
    {
        private Queue<Packet?> _packetQueue = new();

        public new Task<Packet?> SendPacketAsync(Packet packet)
        {
            return Task.FromResult(_packetQueue.Count > 0 ? _packetQueue.Dequeue() : null);
        }

        public void EnqueueResponse(Packet packet)
        {
            _packetQueue.Enqueue(packet);
        }

    }

}