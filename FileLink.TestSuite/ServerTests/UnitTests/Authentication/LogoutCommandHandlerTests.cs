using System;
using System.Threading.Tasks;
using FileLink.Server.Authentication;
using FileLink.Server.Commands;
using FileLink.Server.Commands.Auth;
using FileLink.Server.Network;
using FileLink.Server.Protocol;
using FileLink.Server.Services.Logging;
using FileLink.Server.Server;
using FileLink.Server.SessionState;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.ServerTests.UnitTests.Authentication
{
    [TestFixture]
    public class LogoutCommandHandlerTests
    {
        // Couldn't quite figure out the mocking functionality so I was only able to test the CanHandle methods
        private Mock<IAuthenticationService> _mockAuthService;
        private Mock<LogService> _mockLogService;
        private Mock<ClientSession> _mockClientSession;
        private LogoutCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockLogService = new Mock<LogService>();
            _mockClientSession = new Mock<ClientSession>();
            
            _handler = new LogoutCommandHandler(_mockAuthService.Object, _mockLogService.Object);
        }

        [Test]
        public void CanHandle_WithLoginRequest_ReturnsTrue()
        {
            // Assert
            Assert.That(_handler.CanHandle(Commands.CommandCode.LOGOUT_REQUEST), Is.True);
        }

        [Test]
        public void CanHandle_WithOtherRequest_ReturnsFalse()
        {
            // Assert
            Assert.That(_handler.CanHandle(Commands.CommandCode.LOGIN_REQUEST), Is.False);
        }
    }
}