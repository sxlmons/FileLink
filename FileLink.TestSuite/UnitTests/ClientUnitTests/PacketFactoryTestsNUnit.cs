using System.Text;
using FileLink.Client.Protocol;
using NUnit.Framework;
using System.Text.Json;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.UnitTests.ClientUnitTests;

[TestFixture]
public class PacketFactoryTestsNUnit
{
    private PacketFactory _packetFactory;

    [SetUp]
    public void Setup()
    {
        _packetFactory = new PacketFactory();
    }
    
        [Test]
        public void CreateAccountCreationRequest_ShouldSetCorrectCommandAndPayload()
        {
            var packet = _packetFactory.CreateAccountCreationRequest("user", "pass", "email@example.com");
            Assert.That(Commands.CommandCode.CREATE_ACCOUNT_REQUEST, Is.EqualTo(packet.CommandCode) );

            var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(packet.Payload);
            Assert.That(obj["Username"], Is.EqualTo("user"));
            Assert.That(obj["Password"], Is.EqualTo("pass"));
            Assert.That(obj["Email"], Is.EqualTo("email@example.com"));
        }

        [Test]
        public void CreateLoginRequest_ShouldSetCorrectCommandAndPayload()
        {
            var packet = _packetFactory.CreateLoginRequest("user", "pass");
            Assert.That(Commands.CommandCode.LOGIN_REQUEST, Is.EqualTo(packet.CommandCode));

            var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(packet.Payload);
            Assert.That(obj["Username"], Is.EqualTo("user"));
            Assert.That(obj["Password"], Is.EqualTo("pass"));
        }

        [Test]
        public void CreateLogoutRequest_ShouldSetCorrectCommandAndUserId()
        {
            var packet = _packetFactory.CreateLogoutRequest("user123");
            Assert.That(Commands.CommandCode.LOGOUT_REQUEST, Is.EqualTo(packet.CommandCode) );
            Assert.That("user123", Is.EqualTo(packet.UserId));
        }

        [Test]
        public void ExtractLoginResponse_ValidPayload_ShouldReturnParsedResult()
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { Success = true, Message = "Login OK" });
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.LOGIN_RESPONSE,
                Payload = payload,
                UserId = "uid"
            };

            var (success, message, userId) = _packetFactory.ExtractLoginResponse(packet);
            Assert.That(success, Is.True);
            Assert.That("Login OK", Is.EqualTo(message));
            Assert.That("uid", Is.EqualTo(userId));
        }

        [Test]
        public void ExtractAccountCreationResponse_ValidPayload_ShouldReturnParsedResult()
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { Success = true, Message = "Created", UserId = "u001" });
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.CREATE_ACCOUNT_RESPONSE,
                Payload = payload
            };

            var (success, message, userId) = _packetFactory.ExtractAccountCreationResponse(packet);
            Assert.That(success, Is.True);
            Assert.That("Created",Is.EqualTo(message));
            Assert.That("u001", Is.EqualTo(userId));
        }

        [Test]
        public void ExtractLogoutResponse_ValidPayload_ShouldReturnParsedResult()
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { Success = true, Message = "Logged out" });
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.LOGOUT_RESPONSE,
                Payload = payload
            };

            var (success, message) = _packetFactory.ExtractLogoutResponse(packet);
            Assert.That(success, Is.True);
            Assert.That("Logged out",Is.EqualTo(message));
        }

        [Test]
        public void CreateFileListRequest_ShouldSetUserIdAndCommand()
        {
            var packet = _packetFactory.CreateFileListRequest("user123");
            Assert.That(Commands.CommandCode.FILE_LIST_REQUEST, Is.EqualTo(packet.CommandCode));
            Assert.That("user123", Is.EqualTo(packet.UserId));
        }

        [Test]
        public void CreateFileUploadInitRequest_ShouldSetMetadataAndPayload()
        {
            var packet = _packetFactory.CreateFileUploadInitRequest("uid", "file.txt", 12345, "text/plain");

            Assert.That(packet.CommandCode, Is.EqualTo(Commands.CommandCode.FILE_UPLOAD_INIT_REQUEST));
            Assert.That(packet.UserId, Is.EqualTo("uid"));
            Assert.That(packet.Metadata["FileName"], Is.EqualTo("file.txt"));
            Assert.That(packet.Metadata["FileSize"], Is.EqualTo("12345"));
            Assert.That(packet.Metadata["ContentType"], Is.EqualTo("text/plain"));

            var obj = JsonSerializer.Deserialize<Dictionary<string, Object>>(packet.Payload);
            Assert.That(obj["FileName"].ToString(), Is.EqualTo("file.txt"));
            Assert.That(obj["FileSize"].ToString(), Is.EqualTo("12345"));
            Assert.That(obj["ContentType"].ToString(), Is.EqualTo("text/plain"));
        }

        [Test]
        public void CreateFileUploadChunkRequest_ShouldIncludeCorrectMetadata()
        {
            var data = Encoding.UTF8.GetBytes("ChunkData");
            var packet = _packetFactory.CreateFileUploadChunkRequest("uid", "fid", 3, true, data);

            Assert.That(Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST, Is.EqualTo(packet.CommandCode));
            Assert.That("fid", Is.EqualTo(packet.Metadata["FileId"]));
            Assert.That("3", Is.EqualTo(packet.Metadata["ChunkIndex"]));
            Assert.That("True", Is.EqualTo(packet.Metadata["IsLastChunk"]));
            Assert.That(data, Is.EqualTo(packet.Payload));
        }

        [Test]
        public void CreateFileUploadCompleteRequest_ShouldSetMetadata()
        {
            var packet = _packetFactory.CreateFileUploadCompleteRequest("uid", "fid");

            Assert.That(Commands.CommandCode.FILE_UPLOAD_COMPLETE_REQUEST, Is.EqualTo(packet.CommandCode));
            Assert.That("fid", Is.EqualTo(packet.Metadata["FileId"]));
        }

        [Test]
        public void CreateFileDownloadInitRequest_ShouldSetMetadata()
        {
            var packet = _packetFactory.CreateFileDownloadInitRequest("uid", "fid");

            Assert.That(Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST, Is.EqualTo(packet.CommandCode));
            Assert.That("fid", Is.EqualTo(packet.Metadata["FileId"]));
        }

        [Test]
        public void CreateFileDownloadChunkRequest_ShouldSetMetadata()
        {
            var packet = _packetFactory.CreateFileDownloadChunkRequest("uid", "fid", 2);

            Assert.That("2", Is.EqualTo(packet.Metadata["ChunkIndex"]));
            Assert.That("fid", Is.EqualTo(packet.Metadata["FileId"]));
        }

        [Test]
        public void CreateFileDownloadCompleteRequest_ShouldSetMetadata()
        {
            var packet = _packetFactory.CreateFileDownloadCompleteRequest("uid", "fid");

            Assert.That(Commands.CommandCode.FILE_DOWNLOAD_COMPLETE_REQUEST, Is.EqualTo(packet.CommandCode));
            Assert.That("fid", Is.EqualTo(packet.Metadata["FileId"]));
        }

        [Test]
        public void CreateFileDeleteRequest_ShouldSetMetadata()
        {
            var packet = _packetFactory.CreateFileDeleteRequest("uid", "fid");

            Assert.That(Commands.CommandCode.FILE_DELETE_REQUEST, Is.EqualTo(packet.CommandCode));
            Assert.That("fid", Is.EqualTo(packet.Metadata["FileId"]));
        }

        [Test]
        public void ExtractLoginResponse_WithWrongCommand_ShouldReturnError()
        {
            var packet = new Packet { CommandCode = Commands.CommandCode.FILE_LIST_REQUEST };
            var (success, message, _) = _packetFactory.ExtractLoginResponse(packet);

            Assert.That(success, Is.False);
            Assert.That("Invalid packet type", Is.EqualTo(message));
        }

        [Test]
        public void ExtractLogoutResponse_WithEmptyPayload_ShouldReturnError()
        {
            var packet = new Packet
            {
                CommandCode = Commands.CommandCode.LOGOUT_RESPONSE,
                Payload = Array.Empty<byte>()
            };

            var (success, message) = _packetFactory.ExtractLogoutResponse(packet);
            
            Assert.That(success, Is.False); 
            Assert.That("Empty response payload", Is.EqualTo(message));
        }
        
}
    