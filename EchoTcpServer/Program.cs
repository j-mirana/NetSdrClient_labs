using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTcpServer;

// -------------------------------
// EchoServer
// -------------------------------
public class EchoServer
{
    private readonly int _port;
    private readonly ITcpListener _listener;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public EchoServer(int port, ITcpListener listener, ILogger logger)
    {
        _port = port;
        _listener = listener;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        _listener.Start();
        _logger.Log($"Server started on port {_port}.");

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                _logger.Log("Client connected.");
                _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }

        _logger.Log("Server shutdown.");
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using NetworkStream stream = client.GetStream();

        try
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            while (!token.IsCancellationRequested &&
                   (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead, token);
                _logger.Log($"Echoed {bytesRead} bytes to the client.");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Error: {ex.Message}");
        }
        finally
        {
            client.Close();
            _logger.Log("Client disconnected.");
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _listener.Stop();
        _cancellationTokenSource.Dispose();
        _logger.Log("Server stopped.");
    }
}

// -------------------------------
// UdpTimedSender
// -------------------------------
public class UdpTimedSender : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly UdpClient _udpClient;
    private readonly ILogger _logger;
    private Timer? _timer;

    private ushort _counter = 0;

    public UdpTimedSender(string host, int port, ILogger logger)
    {
        _host = host;
        _port = port;
        _udpClient = new UdpClient();
        _logger = logger;
    }

    public void StartSending(int intervalMilliseconds)
    {
        if (_timer != null)
            throw new InvalidOperationException("Sender is already running.");

        _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
    }

    private void SendMessageCallback(object? state)
    {
        try
        {
            byte[] samples = new byte[1024];
            RandomNumberGenerator.Fill(samples);

            _counter++;

            byte[] msg = new byte[] { 0x04, 0x84 }
                .Concat(BitConverter.GetBytes(_counter))
                .Concat(samples)
                .ToArray();

            var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
            _udpClient.Send(msg, msg.Length, endpoint);

            _logger.Log($"Message sent to {_host}:{_port}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending message: {ex.Message}");
        }
    }

    public void StopSending()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        StopSending();
        _udpClient.Dispose();
    }
}
