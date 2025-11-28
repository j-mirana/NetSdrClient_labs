using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Interfaces;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    private NetSdrClient _client;
    private Mock<ITcpClient> _tcpMock;
    private Mock<IUdpClient> _udpMock;

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();

        _tcpMock.Setup(t => t.Connect())
            .Callback(() =>
            {
                _tcpMock.Setup(t => t.Connected).Returns(true);
            });

        _tcpMock.Setup(t => t.Disconnect())
            .Callback(() =>
            {
                _tcpMock.Setup(t => t.Connected).Returns(false);
            });

        _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
            .Callback<byte[]>(bytes =>
            {
                _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, bytes);
            })
            .Returns(Task.CompletedTask);

        _udpMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
    }

    // -------------------------------------------------------
    // 🟢 CONNECT
    // -------------------------------------------------------
    [Test]
    public async Task ConnectAsyncTest()
    {
        await _client.ConnectAsync();

        _tcpMock.Verify(t => t.Connect(), Times.Once);
        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    // -------------------------------------------------------
    // 🟢 DISCONNECT без підключення
    // -------------------------------------------------------
    [Test]
    public void DisconnectWithNoConnectionTest()
    {
        _client.Disconnect();

        _tcpMock.Verify(t => t.Disconnect(), Times.Once);
    }

    // -------------------------------------------------------
    // 🟢 DISCONNECT після підключення
    // -------------------------------------------------------
    [Test]
    public async Task DisconnectTest()
    {
        await _client.ConnectAsync();
        _tcpMock.Invocations.Clear();

        _client.Disconnect();

        _tcpMock.Verify(t => t.Disconnect(), Times.Once);
    }

    // -------------------------------------------------------
    // 🟢 CHANGE FREQUENCY
    // -------------------------------------------------------
    [Test]
    public async Task ChangeFrequencyAsyncTest()
    {
        await _client.ConnectAsync();
        _tcpMock.Invocations.Clear();

        await _client.ChangeFrequencyAsync(20000000, 1);

        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    // -------------------------------------------------------
    // 🟢 START IQ — без підключення
    // -------------------------------------------------------
    [Test]
    public async Task StartIQNoConnectionTest()
    {
        await _client.StartIQAsync();

        _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(t => t.Connected, Times.AtLeastOnce);
    }

    // -------------------------------------------------------
    // 🟢 START IQ — успішний
    // -------------------------------------------------------
    [Test]
    public async Task StartIQTest()
    {
        await _client.ConnectAsync();
        _udpMock.Invocations.Clear();

        await _client.StartIQAsync();

        _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    // -------------------------------------------------------
    // 🟢 STOP IQ
    // -------------------------------------------------------
    [Test]
    public async Task StopIQTest()
    {
        await _client.ConnectAsync();
        await _client.StartIQAsync();
        _udpMock.Invocations.Clear();

        await _client.StopIQAsync();

        _udpMock.Verify(u => u.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }
}
