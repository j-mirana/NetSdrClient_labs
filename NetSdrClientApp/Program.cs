using NetSdrClientApp;
using NetSdrClientApp.Networking;

// 1. Визначення констант для Магічних чисел (Magic Numbers)
const string DefaultTcpAddress = "127.0.0.1";
const int DefaultTcpPort = 5000;
const int DefaultUdpPort = 60000;
const long DefaultFrequency = 20000000;
const int DefaultGain = 1;


Console.WriteLine($@"Usage:
C - connect
D - disconnet
F - set frequency (Default: {DefaultFrequency} Hz, Gain: {DefaultGain})
S - Start/Stop IQ listener
Q - quit");

// Використання констант замість хардкоду
var tcpClient = new TcpClientWrapper(DefaultTcpAddress, DefaultTcpPort);
var udpClient = new UdpClientWrapper(DefaultUdpPort);

var netSdr = new NetSdrClient(tcpClient, udpClient);

while (true)
{
    var key = Console.ReadKey(intercept: true).Key;
    if (key == ConsoleKey.C)
    {
        await netSdr.ConnectAsync();
    }
    else if (key == ConsoleKey.D)
    {
        // Примітка: Метод Disconect буде перейменовано на Disconnect у NetSdrClient.cs
        netSdr.Disconnect();
    }
    else if (key == ConsoleKey.F)
    {
        // Використання констант для частоти та гейну
        await netSdr.ChangeFrequencyAsync(DefaultFrequency, DefaultGain);
    }
    else if (key == ConsoleKey.S)
    {
        if (netSdr.IQStarted)
        {
            await netSdr.StopIQAsync();
        }
        else
        {
            await netSdr.StartIQAsync();
        }
    }
    else if (key == ConsoleKey.Q)
    {
        break;
    }
}