using FileLink.Client.Models;
using FileLink.Client.Protocol;
using FileLink.Client.Services;
using NUnit.Framework;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.UnitTests.ClientUnitTests;

[TestFixture]
public class AuthServicesTestsNUnit
{
    private TestNetworkService _networkService = null!;
    private AuthenticationService _authenticationService = null!;

    [SetUp]
    public void Setup()
    {
        _networkService = new TestNetworkService();
        _authenticationService = new AuthenticationService(_networkService);
        
    }
    
    /*
     * To run test, first run the server.
     * User credentials must be changed every run time, much like creating an account. 
     * Accounts cannot have the same username or email. 
     */
    
    [Test]
    public async Task CreateAccount_Async_Returns_Success()
    {
        _networkService.ResponseToReturn = new Packet
        {
            CommandCode = Commands.CommandCode.CREATE_ACCOUNT_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                
                Success = true,
                Message = "Account Created",
                UserId = "TestUser2"
                
            })
        };

        var result = await _networkService.CallCreateAccountAsync(_authenticationService, "user_test1", "pass", "email@test1.com");
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Account created successfully."));
        Assert.That(result.UserId, Is.Not.Null);
    }

    [Test]
    public async Task CreateAccount_Async_NullResponse_Returns_Error()
    {
        _networkService.ResponseToReturn = null;
        
        var result = await _networkService.CallCreateAccountAsync(_authenticationService, "user", "pass", "email@test1.com");
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("No response from server"));
        Assert.That(result.UserId, Is.Empty);
        
    }

    [Test]
    public async Task CreateAccount_Async_ThrowsException_Returns_Error()
    {
        _networkService.ThrowException = true;
        
        var result = await _networkService.CallCreateAccountAsync(_authenticationService, "user", "pass", "email@test1.com");
        
        Assert.That(result.Success, Is.False);
        StringAssert.StartsWith(result.Message,"Failed to create account");
        
    }

    [Test]
    public async Task LoginAsync_Success_SetsCurrentUser()
    {
        _networkService.ResponseToReturn = new Packet
        {
            CommandCode = Commands.CommandCode.LOGIN_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    Success = true,
                    Message = "Login Success",
                }),
            UserId = "TestUser2"
        };
        
        var result = await _networkService.CallLoginAsync(_authenticationService, "user_test1", "pass");
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Authentication successful."));
        Assert.That(_authenticationService.IsLoggedIn, Is.True);
        Assert.That(_authenticationService.CurrentUser, Is.Not.Null);
        Assert.That(_authenticationService.CurrentUser!.Id, Is.Not.Null);
        Assert.That(_authenticationService.CurrentUser.Username, Is.EqualTo("user_test1"));

    }

    [Test]
    public async Task LoginAsync_NullResponse_Fails()
    {
        _networkService.ResponseToReturn = null;
        
        var result = await _networkService.CallLoginAsync(_authenticationService, "user", "pass");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("No response from server"));
        Assert.That(_authenticationService.IsLoggedIn, Is.False);
        
    }

    [Test]
    public async Task LoginAsync_WrongCredentials_ThrowsException()
    {
        _networkService.ResponseToReturn = null;
        
        var result = await _networkService.CallLoginAsync(_authenticationService, "NotAUser", "pass");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("Invalid username or password."));
        Assert.That(_authenticationService.IsLoggedIn, Is.False);
    }

    [Test]
    public async Task LogoutAsync_NotLoggedIn_ReturnsError()
    {
        var result = await _authenticationService.LogoutAsync();
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("Not logged in"));
        
    }

    [Test]
    public async Task LogoutAsync_Success_ClearsUser_ResetsConnection()
    {
        _networkService.ResponseToReturn = new Packet
        {
            CommandCode = Commands.CommandCode.LOGIN_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Success = true,
                Message = "Login Success",
                UserId = "ddde3af3-b0b2-431c-a33b-f01ae8d3391b"
                
            })
        };
        
        await _networkService.CallLoginAsync(_authenticationService, "user_test1", "pass");
        
        _networkService.ResponseToReturn = new Packet
        {
            CommandCode = Commands.CommandCode.LOGOUT_RESPONSE,
            Payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                
                Success = true,
                Message = "Logout Success",
                
            })
        };
        
        var result = await _authenticationService.LogoutAsync();
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Logout successful."));
        Assert.That(_authenticationService.IsLoggedIn, Is.False);
        
    }
    
    // Helper class 
    
    private void SetPrivateCurrentUser(User user)
    {
        typeof(AuthenticationService)
            .GetField("_currentUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_authenticationService, user);
        
    }
    
}