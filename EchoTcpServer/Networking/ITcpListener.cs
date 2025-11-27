using System.Net.Sockets;
using System.Threading.Tasks;

// Інтерфейс для абстрагування TcpListener
public interface ITcpListener
{
    void Start();
    void Stop();
    Task<TcpClient> AcceptTcpClientAsync();
}