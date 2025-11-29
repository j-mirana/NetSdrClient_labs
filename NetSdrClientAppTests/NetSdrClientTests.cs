using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using System.Threading.Tasks;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;
    Mock<ILogger> _loggerMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        // Setup SendMessageAsync to return a completed task without raising MessageReceived, 
        // which avoids unexpected side effects in async tests.
        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

        _updMock = new Mock<IUdpClient>();
        _loggerMock = new Mock<ILogger>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public async Task TearDown()
    {
        // FIX: Гарантуємо, що UDP-клієнт зупиниться, якщо був запущений, щоб уникнути блокування тестів.
        if (_client.IQStarted)
        {
            await _client.StopIQAsync();
        }
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    // FIX: Changed from async Task to void to fix SonarCloud "async method lacks await" smell
    public void DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconnect();

        //assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        // COVERAGE: Verify logger call
        _loggerMock.Verify(l => l.Log("No active connection to disconnect."), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await _client.ConnectAsync();

        //act
        _client.Disconnect();

        //assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        // COVERAGE: Verify logger call
        _loggerMock.Verify(l => l.Log("Disconnected."), Times.Once);
    }

    // Новий тест для Лаби 3: Покриття ChangeFrequencyAsync
    [Test]
    public async Task ChangeFrequencyAsyncTest()
    {
        // Arrange
        await _client.ConnectAsync();
        _tcpMock.Invocations.Clear();

        long frequency = 20000000;
        int channel = 1;

        // Act
        await _client.ChangeFrequencyAsync(frequency, channel);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    // COVERAGE: New test for ChangeFrequencyAsync when disconnected (covers guard clause and logging)
    [Test]
    public async Task ChangeFrequencyAsyncNoConnectionTest()
    {
        // Arrange (no connection by default)
        long frequency = 20000000;
        int channel = 1;

        // Act
        var result = await _client.ChangeFrequencyAsync(frequency, channel);

        // Assert
        Assert.That(result, Is.Null);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _loggerMock.Verify(l => l.Log("No active connection. TCP request aborted."), Times.Once);
    }


    [Test]
    public async Task StartIQNoConnectionTest()
    {
        //act
        await _client.StartIQAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
        // COVERAGE: Verify logger call
        _loggerMock.Verify(l => l.Log("No active connection. Cannot start IQ."), Times.Once);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await _client.ConnectAsync();

        //act
        await _client.StartIQAsync();

        //assert
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    // COVERAGE: New test for StopIQAsync when disconnected (covers guard clause and logging)
    public async Task StopIQNoConnectionTest()
    {
        // Act
        await _client.StopIQAsync();

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
        _loggerMock.Verify(l => l.Log("No active connection. Cannot stop IQ."), Times.Once);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await _client.ConnectAsync();

        //act
        await _client.StopIQAsync();

        //assert
        _updMock.Verify(udp => udp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    // COVERAGE: New test for logging unsolicited/late TCP messages (covers 'if (responseTaskSource != null)' block when null)
    [Test]
    public void TcpMessageReceived_LogsResponse_WhenResponseTaskSourceIsNull()
    {
        // Arrange
        byte[] message = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act: Simulate an unsolicited message
        _tcpMock.Raise(c => c.MessageReceived += null, _tcpMock.Object, message);

        // Assert
        _loggerMock.Verify(l => l.Log(It.Is<string>(s => s.StartsWith("Response recieved:"))), Times.Once);
    }

    // COVERAGE: New test for logging UDP messages
    [Test]
    public void UdpMessageReceived_LogsSamples()
    {
        // Arrange: Header + Sample Data (4 bytes header + 4 bytes body)
        byte[] rawMessage = new byte[] { 0x04, 0x84, 0x00, 0x01, 0x11, 0x22, 0x33, 0x44 };

        // Act: Simulate receiving a UDP message
        _updMock.Raise(c => c.MessageReceived += null, _updMock.Object, rawMessage);

        // Assert
        _loggerMock.Verify(l => l.Log(It.Is<string>(s => s.StartsWith("Samples recieved:"))), Times.Once);
    }
}