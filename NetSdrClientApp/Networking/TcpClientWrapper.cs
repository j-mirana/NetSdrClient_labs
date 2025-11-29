using System;
using System.Collections.Generic;
using System.IO;
// FIX: Removed duplicated 'using System;'
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient
    {
        private readonly ILogger _logger; // Додано ILogger
        private string _host;
        private int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts = null; // Фікс CS8618

        public bool Connected => _tcpClient != null && _tcpClient.Connected && _stream != null;

        public event EventHandler<byte[]>? MessageReceived;

        // ILogger додано до конструктора
        public TcpClientWrapper(string host, int port, ILogger logger)
        {
            _host = host;
            _port = port;
            _logger = logger;
        }

        public void Connect()
        {
            if (Connected)
            {
                _logger.Log($"Already connected to {_host}:{_port}"); // Виправлено Console.WriteLine
                return;
            }

            _tcpClient = new TcpClient();

            try
            {
                _cts = new CancellationTokenSource();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                _logger.Log($"Connected to {_host}:{_port}"); // Виправлено Console.WriteLine
                _ = StartListeningAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to connect: {ex.Message}"); // Виправлено Console.WriteLine
            }
        }

        public void Disconnect()
        {
            if (Connected)
            {
                _cts?.Cancel();
                _stream?.Close();
                _tcpClient?.Close();

                _cts = null;
                _tcpClient = null;
                _stream = null;
                _logger.Log("Disconnected."); // Виправлено Console.WriteLine
            }
            else
            {
                _logger.Log("No active connection to disconnect."); // Виправлено Console.WriteLine
            }
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (Connected && _stream != null && _stream.CanWrite)
            {
                _logger.Log($"Message sent: " + data.Select(b => Convert.ToString(b, toBase: 16)).Aggregate((l, r) => $"{l} {r}")); // Виправлено Console.WriteLine
                await _stream.WriteAsync(data, 0, data.Length);
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        public async Task SendMessageAsync(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            if (Connected && _stream != null && _stream.CanWrite)
            {
                _logger.Log($"Message sent: " + data.Select(b => Convert.ToString(b, toBase: 16)).Aggregate((l, r) => $"{l} {r}")); // Виправлено Console.WriteLine
                await _stream.WriteAsync(data, 0, data.Length);
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        private async Task StartListeningAsync()
        {
            if (Connected && _stream != null && _stream.CanRead)
            {
                try
                {
                    _logger.Log($"Starting listening for incomming messages."); // Виправлено Console.WriteLine

                    while (!_cts!.Token.IsCancellationRequested) // ! використовується, оскільки _cts ініціалізується в Connect()
                    {
                        byte[] buffer = new byte[8194];

                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                        if (bytesRead > 0)
                        {
                            MessageReceived?.Invoke(this, buffer.AsSpan(0, bytesRead).ToArray());
                        }
                    }
                }
                catch (OperationCanceledException) // Виправлення: CS0168 - видалено невикористану змінну ex
                {
                    // Виправлення: Порожній блок catch
                    // Це виключення є очікуваним і виникає, коли _cts.Cancel() викликається 
                    // для зупинки циклу прослуховування. Воно ігнорується, оскільки
                    // є частиною нормального процесу відключення.
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in listening loop: {ex.Message}"); // Виправлено Console.WriteLine
                }
                finally
                {
                    _logger.Log("Listener stopped."); // Виправлено Console.WriteLine
                }
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }
    }
}