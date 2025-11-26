using NetSdrClientApp.Networking;
using System;

namespace NetSdrClientApp
{
    // Проста реалізація ILogger, що виводить повідомлення в консоль.
    public class ConsoleLogger : ILogger
    {
        public void Log(string message) => Console.WriteLine($"[INFO] {message}");
        public void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
    }
}