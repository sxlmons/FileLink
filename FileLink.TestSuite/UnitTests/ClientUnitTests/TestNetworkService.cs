using FileLink.Client.Protocol;
using FileLink.Client.Services;

namespace FileLink.TestSuite.UnitTests.ClientUnitTests;

public class TestNetworkService: NetworkService
{
    public Packet? ResponseToReturn { get; set; }
    public bool ThrowException { get; set; } = false;
    public bool ResetCalled { get; private set; } = false;

    public new Task<Packet?> SendAndReceiveAsync(Packet packet)
    {
        if (ThrowException)
        {
            throw new Exception("Test newtork failed");
        }

        return Task.FromResult(ResponseToReturn);
    }

    public new void ResetConnection()
    {
        ResetCalled = true;
    }
    
    // Helper methods
    public async Task<(bool Success, string Message, string UserId)> CallCreateAccountAsync(AuthenticationService authService, string username, string password, string email = "")
    {
        return await authService.CreateAccountAsync(username, password, email);
    }

    public async Task<(bool Success, string Message)> CallLoginAsync(AuthenticationService authService, string username, string password)
    {
        return await authService.LoginAsync(username, password);
    }

}