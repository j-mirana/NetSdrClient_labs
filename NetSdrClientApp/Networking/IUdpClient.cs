namespace NetSdrClientApp.Interfaces // or whatever namespace the test expects
{

    public interface IUdpClient
    {
        event EventHandler<byte[]>? MessageReceived;

        Task StartListeningAsync();

        void StopListening();
        void Exit();
    }
}