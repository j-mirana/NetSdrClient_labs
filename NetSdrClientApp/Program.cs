using NetSdrClientApp;
using NetSdrClientApp.Networking;
using System;

// 1. Визначення констант для Магічних чисел (Magic Numbers)
const string DefaultTcpAddress = "127.0.0.1";
const int DefaultTcpPort = 5000;
const int DefaultUdpPort = 60000;
const long DefaultFrequency = 20000000;
const int DefaultGain = 1;

// Створення єдиного екземпляру логера
var logger = new ConsoleLogger();


Console.WriteLine($@"Usage:
C - connect
D - disconnect
F - set frequency (Default: {DefaultFrequency} Hz, Gain: {DefaultGain})
S - Start/Stop IQ listener
Q - quit");

// ВИПРАВЛЕННЯ: Передача об'єкта 'logger' у конструктори
var tcpClient = new TcpClientWrapper(DefaultTcpAddress, DefaultTcpPort, logger);
var udpClient = new UdpClientWrapper(DefaultUdpPort, logger);

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
        // Метод Disconnect вже перейменовано у NetSdrClient.cs
        netSdr.Disconnect();
    }
    else if (key == ConsoleKey.F)
    {
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