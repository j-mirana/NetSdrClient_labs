using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    public class NetSdrClientTests
    {
        private NetSdrClient _client;
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;
        private Mock<ILogger> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();
            _loggerMock = new Mock<ILogger>();

            // Default TCP is disconnected
            _tcpMock.Setup(t => t.Connected).Returns(false);

            // When Connect() is called → set Connected = true
            _tcpMock.Setup(t => t.Connect()).Callback(() =>
            {
                _tcpMock.Setup(t => t.Connected).Returns(true);
            });

            // Auto-response for every SendMessageAsync → avoids hangs
            _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                    .Callback(() =>
                    {
                        // Instant fake response
                        _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, new byte[] { 0xAA });
                    })
                    .Returns(Task.CompletedTask);

            // UDP stubs
            _udpMock.Setup(u => u.StartListeningAsync()).Returns(Task.CompletedTask);
            _udpMock.Setup(u => u.StopListening());

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task ConnectAsync_SendsThreeStartupMessages()
        {
            await _client.ConnectAsync();

            _tcpMock.Verify(t => t.Connect(), Times.Once);
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public void Disconnect_WhenNotConnected_LogsInfo()
        {
            _client.Disconnect();

            _tcpMock.Verify(t => t.Disconnect(), Times.Once);
            _loggerMock.Verify(l => l.Log("No active connection to disconnect."), Times.Once);
        }

        [Test]
        public async Task Disconnect_WhenConnected_LogsDisconnected()
        {
            await _client.ConnectAsync();

            _client.Disconnect();

            _loggerMock.Verify(l => l.Log("Disconnected."), Times.Once);
        }

        [Test]
        public async Task ChangeFrequencyAsync_SendsMessage()
        {
            await _client.ConnectAsync();
            _tcpMock.Invocations.Clear();

            await _client.ChangeFrequencyAsync(20_000_000, 1);

            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Test]
        public async Task ChangeFrequencyAsync_NoConnection_ReturnsNullAndLogs()
        {
            var result = await _client.ChangeFrequencyAsync(20_000_000, 1);

            Assert.IsNull(result);
            _loggerMock.Verify(l => l.Log("No active connection. TCP request aborted."), Times.Once);
        }

        [Test]
        public async Task StartIQAsync_NoConnection_DoesNotStart()
        {
            await _client.StartIQAsync();

            _udpMock.Verify(u => u.StartListeningAsync(), Times.Never);
            _loggerMock.Verify(l => l.Log("No active connection. Cannot start IQ."), Times.Once);
        }

        [Test]
        public async Task StartIQAsync_Valid()
        {
            await _client.ConnectAsync();
            await _client.StartIQAsync();

            _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
            Assert.IsTrue(_client.IQStarted);
        }

        [Test]
        public async Task StopIQAsync_NoConnection_LogsWarning()
        {
            await _client.StopIQAsync();

            _loggerMock.Verify(l => l.Log("No active connection. Cannot stop IQ."), Times.Once);
        }

        [Test]
        public async Task StopIQAsync_Valid()
        {
            await _client.ConnectAsync();
            await _client.StopIQAsync();

            _udpMock.Verify(u => u.StopListening(), Times.Once);
            Assert.IsFalse(_client.IQStarted);
        }

        [Test]
        public void TcpMessageReceived_WhenNoAwaiter_LogsResponse()
        {
            byte[] msg = { 0x01, 0x02, 0x03 };

            _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, msg);

            _loggerMock.Verify(l => l.Log(It.Is<string>(s => s.StartsWith("Response recieved:"))),
                Times.Once);
        }

        [Test]
        public void UdpMessageReceived_LogsSamples()
        {
            byte[] udpData = { 0x04, 0x84, 0x00, 0x01, 0x11, 0x22, 0x33, 0x44 };

            _udpMock.Raise(u => u.MessageReceived += null, _udpMock.Object, udpData);

            _loggerMock.Verify(l => l.Log(It.Is<string>(s => s.StartsWith("Samples recieved:"))),
                Times.Once);
        }
    }
}
