using System;

// Інтерфейс для абстрагування логування
public interface ILogger
{
    void Log(string message);
    void LogError(string message);
}