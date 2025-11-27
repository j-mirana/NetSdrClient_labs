using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Клас EchoServer виділено та зроблено тестованим
public class EchoServer
{
    private readonly int _port;
    private readonly ITcpListener _listener; // Впровадження залежності
    private readonly ILogger _logger;       // Впровадження залежності
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
                // Приймаємо клієнта
                TcpClient client = await _listener.AcceptTcpClientAsync();
                _logger.Log("Client connected.");

                // Обробка клієнта у фоновому потоці
                _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
            }
            catch (ObjectDisposedException)
            {
                // Слухач закрито (Stop() було викликано)
                break;
            }
        }

        _logger.Log("Server shutdown.");
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (NetworkStream stream = client.GetStream())
        {
            try
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    // Echo back the received message
                    await stream.WriteAsync(buffer, 0, bytesRead, token);
                    _logger.Log($"Echoed {bytesRead} bytes to the client."); // Використовуємо логер
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _logger.LogError($"Error: {ex.Message}"); // Використовуємо логер
            }
            finally
            {
                client.Close();
                _logger.Log("Client disconnected."); // Використовуємо логер
            }
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _listener.Stop();
        _cancellationTokenSource.Dispose();
        _logger.Log("Server stopped."); // Використовуємо логер
    }
}


// Також рефакторимо UdpTimedSender для використання ILogger
public class UdpTimedSender : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly UdpClient _udpClient;
    private readonly ILogger _logger; // Додано логер
    private Timer? _timer;

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

    ushort i = 0;

    private void SendMessageCallback(object? state)
    {
        try
        {
            //dummy data
            Random rnd = new Random();
            byte[] samples = new byte[1024];
            rnd.NextBytes(samples);
            i++;

            byte[] msg = (new byte[] { 0x04, 0x84 }).Concat(BitConverter.GetBytes(i)).Concat(samples).ToArray();
            var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

            _udpClient.Send(msg, msg.Length, endpoint);
            _logger.Log($"Message sent to {_host}:{_port} "); // Використовуємо логер
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending message: {ex.Message}"); // Використовуємо логер
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