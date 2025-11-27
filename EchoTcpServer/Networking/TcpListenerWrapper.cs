using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

// Реалізація ITcpListener, що використовує реальний TcpListener
public class TcpListenerWrapper : ITcpListener
{
    private readonly TcpListener _listener;

    public TcpListenerWrapper(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start() => _listener.Start();
    public void Stop() => _listener.Stop();
    public Task<TcpClient> AcceptTcpClientAsync() => _listener.AcceptTcpClientAsync();
}