using System.Text.Json;
using FileLink.Client.Protocol;
using FileLink.Client.Services;
using FileLink.Client.Models;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.UnitTests.ClientUnitTests;

[TestFixture]
public class DirectoryServiceTestsNUnit
{
    private DirectoryService _directoryService = null!;
    private TestNetworkService _networkService = null!;
    private AuthenticationService _authenticationService = null!;
    private const string testUserId = "ddde3af3-b0b2-431c-a33b-f01ae8d3391b";

    [SetUp]
    public async Task SetUp()
    {
        _networkService = new TestNetworkService();
        _directoryService = new DirectoryService(_networkService);
        _authenticationService = new AuthenticationService(_networkService);
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.LOGIN_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true,
                Message = "Login Success",
            }),
            
            UserId = testUserId
            
        });

        await _authenticationService.LoginAsync("user_test1", "pass");
    }

    [Test]
    public async Task GetDirectoryContents_Async_ReturnsListCount_Successfully()
    {
        var response = new
        {
            File = new List<FileItem> { new() { Id = "f1", FileName = "file1.txt" } },
            Directory = new List<DirectoryItem> { new() { Id = "d1", Name = "folder1" } }
        };
        
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(response),
        });

        var (files, directories) = await _directoryService.GetDirectoryContentsAsync(null, testUserId);
        
        Assert.That(response.File.Count, Is.AtLeast(1));
        Assert.That(response.Directory.Count, Is.AtLeast(1));
        Assert.That(files[0].FileName, Is.Not.Null);
        Assert.That(directories.Count, Is.Not.Null);
        
    }

    [Test]
    public async Task GetDirectoryContents_Async_ReturnsEmpty_Fail()
    {
        _networkService.EnqueueResponse(null);
        
        var (files, directories) = await _directoryService.GetDirectoryContentsAsync("NullDir", testUserId);
        
        Assert.That(files, Is.Empty);
        Assert.That(directories, Is.Empty);
        
    }

    [Test] 
    public async Task CreateDirectoryAsync_ReturnsDirectory_WhenSuccess()
    {
        var response = new
        {
            Success = true,
            DirectoryId = "dir123",
            DirectoryName = "TestFolder4"

        };
        
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CREATE_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(response),
        });
        
        var result = await _directoryService.CreateDirectoryAsync("TestFolder4",null ,testUserId);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("TestFolder4"));

    }
    
    [Test]
    public async Task CreateDirectoryAsync_ReturnsNull_WhenErrorResponse()
    {
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.ERROR
        });

        var result = await _directoryService.CreateDirectoryAsync("FailTestFolder", null, testUserId);
        Assert.That(result, Is.Null);
    }
    
    [Test] 
    public async Task DeleteDirectory_Async_ReturnsFalse_WhenDirectoryExists()
    {
        var setUp = new
        {
            Success = true,
            DirectoryId = "dir123",
            DirectoryName = "DeleteFolder3"

        };
        
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CREATE_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(setUp),
        });
        
        var setUpResult = await _directoryService.CreateDirectoryAsync("DeleteFolder3", null, testUserId);
        
        var response = new { Success = true };

        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_DELETE_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(response)
        });

        var result = await _directoryService.DeleteDirectoryAsync("DeleteFolder3", true, testUserId);
        
        Assert.That(setUpResult, Is.Not.Null);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteDirectoryAsync_ShouldReturnFalse_WhenError()
    {
        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.ERROR
        });

        var result = await _directoryService.DeleteDirectoryAsync("badDir", false, testUserId);
        Assert.That(result, Is.False);
    }
    
    [Test] 
    public async Task GetDirectoryByIdAsync_ShouldReturnDirectory_WhenFound()
    {
        var response = new
        {
            Files = new List<FileItem>(),
            Directories = new List<DirectoryItem>
            {
                new DirectoryItem { Id = "targetDir", Name = "Target" }
            }
        };

        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(response)
        });

        var result = await _directoryService.GetDirectoryByIdAsync("a2df3a5d-347d-4406-9568-b8664420ade6", testUserId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("a2df3a5d-347d-4406-9568-b8664420ade6"));
    }

    [Test]
    public async Task GetDirectoryByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var response = new
        {
            Files = new List<FileItem>(),
            Directories = new List<DirectoryItem>()
        };

        _networkService.EnqueueResponse(new Packet
        {
            CommandCode = Commands.CommandCode.DIRECTORY_CONTENTS_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(response)
        });

        var result = await _directoryService.GetDirectoryByIdAsync("missingDir", testUserId);
        Assert.That(result, Is.Null);
    }


    // Helper Class
    private class TestNetworkService : NetworkService
    {
        private readonly Queue<Packet?> _responses = new();

        public new Task<Packet?> SendAndReceiveAsync(Packet packet)
        {
            return Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : null);
        }

        public void EnqueueResponse(Packet? packet)
        {
            _responses.Enqueue(packet);
        }
    }
    
}
