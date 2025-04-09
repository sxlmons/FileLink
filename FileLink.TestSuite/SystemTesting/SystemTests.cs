using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using FileLink.Client.Connection;
using FileLink.Client.FileOperations;
using FileLink.Client.Protocol;
using FileLink.Client.Services;
using FileLink.Client.Session;

namespace FileLink.TestSuite.SystemTesting
{
    [TestFixture]
    public class SystemTests
    {
        private Process _serverProcess;
        private CloudServerConnection _connection;
        private AuthenticationManager _authManager;
        private FileManager _fileManager;

        private readonly string _serverExePath =
            @"C:\\Users\\Chris\\RiderProjects\\Project-IV-Section-2-Group-1\\FileLink.Server\\bin\\Debug\\net9.0\\FileLink.Server.exe";

        private static string ComputeSHA256(byte[] data)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _serverExePath,
                    Arguments = "",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!_serverProcess.Start())
                throw new Exception("Failed to start the server process.");

            await Task.Delay(2000);

            _connection = new CloudServerConnection("localhost", 9000);
            await _connection.ConnectAsync();

            InjectEncryptionKeys();

            _authManager = new AuthenticationManager(_connection);
            _fileManager = new FileManager(_connection, _authManager);

            var login = await _authManager.LoginAsync("admin", "admin");
            Assert.That(login.Success, Is.True, "Login should succeed.");
        }

        private void InjectEncryptionKeys()
        {
            var serializerField = typeof(CloudServerConnection)
                .GetField("_packetSerializer", BindingFlags.NonPublic | BindingFlags.Instance);
            var serializer = serializerField?.GetValue(_connection);

            if (serializer != null)
            {
                var aesKeyField = serializer.GetType().GetField("_aesKey", BindingFlags.NonPublic | BindingFlags.Instance);
                var aesIVField = serializer.GetType().GetField("_aesIV", BindingFlags.NonPublic | BindingFlags.Instance);

                if (aesKeyField != null && aesIVField != null)
                {
                    aesKeyField.SetValue(serializer, Encoding.UTF8.GetBytes("ThisIsA32ByteLongEncryptionKey!1234"));
                    aesIVField.SetValue(serializer, Encoding.UTF8.GetBytes("ThisIsA16ByteIV!"));
                }
            }
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            if (_authManager.IsAuthenticated)
                await _authManager.LogoutAsync();

            await _connection.DisconnectAsync();

            if (!_serverProcess.HasExited)
            {
                _serverProcess.Kill();
                await _serverProcess.WaitForExitAsync();
            }
        }

        [Test]
        public async Task Test_UploadDownloadFile_Success()
        {
            string content = "This is test file content";
            string tempPath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempPath, content);

            var uploaded = await _fileManager.UploadFileAsync(tempPath, null, null, CancellationToken.None);
            Assert.That(uploaded, Is.Not.Null);
            Assert.That(string.IsNullOrEmpty(uploaded.Id), Is.False);

            string downloadPath = Path.Combine(Path.GetTempPath(), "downloaded_" + Path.GetFileName(tempPath));
            var downloaded = await _fileManager.DownloadFileAsync(uploaded.Id, downloadPath, null, CancellationToken.None);

            Assert.That(downloaded, Is.Not.Null);
            Assert.That(File.Exists(downloadPath), Is.True);

            string result = await File.ReadAllTextAsync(downloadPath);
            string expectedHash = ComputeSHA256(Encoding.UTF8.GetBytes(content));
            string actualHash = ComputeSHA256(Encoding.UTF8.GetBytes(result));

            Console.WriteLine($"Expected SHA: {expectedHash}");
            Console.WriteLine($"Actual SHA:   {actualHash}");

            Assert.That(result, Is.EqualTo(content), "Downloaded file content should match.");

            File.Delete(tempPath);
            File.Delete(downloadPath);
        }

        [Test]
        public void Test_DynamicPayload_RoundTrip()
        {
            string originalText = "Hello from dynamic payload!";
            byte[] payload = Encoding.UTF8.GetBytes(originalText);

            var packet = new Packet(Commands.CommandCode.FILE_DOWNLOAD_INIT_REQUEST)
            {
                UserId = "testUser",
                Payload = payload
            };

            var serializer = new PacketSerializer();
            typeof(PacketSerializer).GetField("_aesKey", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(serializer, Encoding.UTF8.GetBytes("ThisIsA32ByteLongEncryptionKey!1234"));
            typeof(PacketSerializer).GetField("_aesIV", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(serializer, Encoding.UTF8.GetBytes("ThisIsA16ByteIV!"));

            byte[] bytes = serializer.Serialize(packet);
            var deserialized = serializer.Deserialize(bytes);

            string result = Encoding.UTF8.GetString(deserialized.Payload);
            Assert.That(result, Is.EqualTo(originalText));
        }

        [Test]
        public void Test_ByteArrayPayload_RoundTrip()
        {
            byte[] original = new byte[256];
            new Random(42).NextBytes(original);

            var packet = new Packet(Commands.CommandCode.FILE_UPLOAD_CHUNK_REQUEST)
            {
                UserId = "testUser",
                Payload = original
            };
            packet.Metadata["FileId"] = "test";
            packet.Metadata["ChunkIndex"] = "0";
            packet.Metadata["IsLastChunk"] = "true";

            var serializer = new PacketSerializer();
            typeof(PacketSerializer).GetField("_aesKey", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(serializer, Encoding.UTF8.GetBytes("ThisIsA32ByteLongEncryptionKey!1234"));
            typeof(PacketSerializer).GetField("_aesIV", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(serializer, Encoding.UTF8.GetBytes("ThisIsA16ByteIV!"));

            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] serialized = serializer.Serialize(packet);
            var deserialized = serializer.Deserialize(serialized);

            string originalHash = ComputeSHA256(original);
            string deserializedHash = ComputeSHA256(deserialized.Payload);

            Console.WriteLine($"Original SHA:     {originalHash}");
            Console.WriteLine($"Deserialized SHA: {deserializedHash}");

            Assert.That(deserializedHash, Is.EqualTo(originalHash));
        }
    }
}