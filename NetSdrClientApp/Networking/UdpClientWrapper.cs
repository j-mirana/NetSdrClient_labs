using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetSdrClientApp.Networking; // Додано для ILogger та IUdpClient

// Клас має знаходитися в просторі імен проекту NetSdrClientApp
namespace NetSdrClientApp.Networking
{
    // Клас повинен реалізовувати інтерфейс IUdpClient (який вже вказано)
    public class UdpClientWrapper : IUdpClient
    {
        private readonly ILogger _logger; // Додано залежність ILogger
        private readonly IPEndPoint _localEndPoint;
        private CancellationTokenSource? _cts;
        private UdpClient? _udpClient;

        public event EventHandler<byte[]>? MessageReceived;

        // Виправлення: ILogger додано до конструктора
        public UdpClientWrapper(int port, ILogger logger)
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
            _logger = logger;
        }

        public async Task StartListeningAsync()
        {
            _cts = new CancellationTokenSource();
            _logger.Log("Start listening for UDP messages..."); // Виправлено Console.WriteLine

            try
            {
                _udpClient = new UdpClient(_localEndPoint);
                while (!_cts.Token.IsCancellationRequested)
                {
                    // Використовуємо .ReceiveAsync з токеном
                    UdpReceiveResult result = await _udpClient.ReceiveAsync(_cts.Token);
                    MessageReceived?.Invoke(this, result.Buffer);

                    _logger.Log($"Received from {result.RemoteEndPoint}"); // Виправлено Console.WriteLine
                }
            }
            catch (OperationCanceledException ex)
            {
                // Do something...
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error receiving message: {ex.Message}"); // Виправлено Console.WriteLine
            }
        }

        public void StopListening()
        {
            try
            {
                _cts?.Cancel();
                _udpClient?.Close();
                _logger.Log("Stopped listening for UDP messages."); // Виправлено Console.WriteLine
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while stopping: {ex.Message}"); // Виправлено Console.WriteLine
            }
        }

        public void Exit()
        {
            // Усунення дублювання: виклик StopListening
            StopListening();
        }

        public override int GetHashCode()
        {
            // Цей метод не має прямого відношення до Code Smells Лаби 2, 
            // але залишається як частина оригінального коду.
            var payload = $"{nameof(UdpClientWrapper)}|{_localEndPoint.Address}|{_localEndPoint.Port}";

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));

            return BitConverter.ToInt32(hash, 0);
        }
    }
}