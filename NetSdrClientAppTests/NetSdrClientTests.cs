using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
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

            _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                    .Returns(Task.CompletedTask);

            // Default: TCP disconnected
            _tcpMock.Setup(t => t.Connected).Returns(false);

            // When Connect() is invoked → connection becomes active
            _tcpMock.Setup(t => t.Connect()).Callback(() =>
            {
                _tcpMock.Setup(t => t.Connected).Returns(true);
            });

            // When Disconnect() is invoked → connection becomes inactive
            _tcpMock.Setup(t => t.Disconnect()).Callback(() =>
            {
                _tcpMock.Setup(t => t.Connected).Returns(false);
            });

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object, _loggerMock.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            if (_client.IQStarted)
                await _client.StopIQAsync();
        }

        // ----------------------------------------------
        // CONNECTION
        // ----------------------------------------------

        [Test]
        public async Task ConnectAsync_SendsInitialRequests()
        {
            await _client.ConnectAsync();

            _tcpMock.Verify(t => t.Connect(), Times.Once);
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public void Disconnect_WhenNotConnected_LogsWarning()
        {
            _client.Disconnect();

            Assert.Multiple(() =>
            {
                _tcpMock.Verify(t => t.Disconnect(), Times.Once);
                _loggerMock.Verify(l => l.Log("No active connection to disconnect."), Times.Once);
            });
        }

        [Test]
        public async Task Disconnect_WhenConnected_LogsAndDisconnects()
        {
            await _client.ConnectAsync();

            _client.Disconnect();

            Assert.Multiple(() =>
            {
                _tcpMock.Verify(t => t.Disconnect(), Times.Once);
                _loggerMock.Verify(l => l.Log("Disconnected."), Times.Once);
            });
        }

        // ----------------------------------------------
        // FREQUENCY CHANGE
        // ----------------------------------------------

        [Test]
        public async Task ChangeFrequencyAsync_SendsMessage_WhenConnected()
        {
            await _client.ConnectAsync();
            _tcpMock.Invocations.Clear();

            await _client.ChangeFrequencyAsync(20_000_000, 1);

            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Test]
        public async Task ChangeFrequencyAsync_WhenNotConnected_ReturnsNullAndLogs()
        {
            var result = await _client.ChangeFrequencyAsync(20_000_000, 1);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Null);
                _loggerMock.Verify(l => l.Log("No active connection. TCP request aborted."), Times.Once);
                _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            });
        }

        // ----------------------------------------------
        // IQ START / STOP
        // ----------------------------------------------

        [Test]
        public async Task StartIQAsync_WhenDisconnected_LogsWarning()
        {
            await _client.StartIQAsync();

            Assert.Multiple(() =>
            {
                _loggerMock.Verify(l => l.Log("No active connection. Cannot start IQ."), Times.Once);
                _udpMock.Verify(u => u.StartListeningAsync(), Times.Never);
            });
        }

        [Test]
        public async Task StartIQAsync_WhenConnected_StartsUdpAndSetsFlag()
        {
            await _client.ConnectAsync();

            await _client.StartIQAsync();

            Assert.Multiple(() =>
            {
                _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
                Assert.That(_client.IQStarted, Is.True);
            });
        }

        [Test]
        public async Task StopIQAsync_WhenDisconnected_LogsWarning()
        {
            await _client.StopIQAsync();

            Assert.Multiple(() =>
            {
                _loggerMock.Verify(l => l.Log("No active connection. Cannot stop IQ."), Times.Once);
                _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            });
        }

        [Test]
        public async Task StopIQAsync_WhenConnected_StopsUdpAndClearsFlag()
        {
            await _client.ConnectAsync();

            await _client.StopIQAsync();

            Assert.Multiple(() =>
            {
                _udpMock.Verify(u => u.StopListening(), Times.Once);
                Assert.That(_client.IQStarted, Is.False);
            });
        }

        // ----------------------------------------------
        // TCP + UDP EVENTS
        // ----------------------------------------------

        [Test]
        public void TcpMessageReceived_WhenNoAwaiter_LogsUnsolicitedResponse()
        {
            byte[] msg = { 0x01, 0x02, 0x03, 0x04 };

            _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, msg);

            _loggerMock.Verify(
                l => l.Log(It.Is<string>(s => s.StartsWith("Response recieved:"))),
                Times.Once);
        }

        [Test]
        public void UdpMessageReceived_LogsSamples()
        {
            byte[] raw = { 0x04, 0x84, 0x00, 0x01, 0x11, 0x22, 0x33, 0x44 };

            _udpMock.Raise(u => u.MessageReceived += null, _udpMock.Object, raw);

            _loggerMock.Verify(
                l => l.Log(It.Is<string>(s => s.StartsWith("Samples recieved:"))),
                Times.Once);
        }
    }
}
