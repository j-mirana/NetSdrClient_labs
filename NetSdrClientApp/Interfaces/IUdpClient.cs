namespace NetSdrClientApp.Interfaces
{
    public interface IUdpClient
    {
        Task StartListeningAsync();
        void StopListening();
        event EventHandler<byte[]> DatagramReceived;
    }
}

    }
}