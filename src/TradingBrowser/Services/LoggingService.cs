using System;
using System.IO;
using TradingBrowser.Helpers;

namespace TradingBrowser.Services;

public static class LoggingService
{
    private static readonly object _lock = new();
    private static string LogFile => Path.Combine(PathHelper.LogsFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");

    static LoggingService()
    {
        Directory.CreateDirectory(PathHelper.LogsFolder);
    }

    public static void Log(string message, string level = "INFO")
    {
        lock (_lock)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFile, logEntry);
            }
            catch { /* Fail silently to prevent app crashes */ }
        }
    }

    public static void Error(string message, Exception? ex = null)
    {
        Log($"{message} {ex?.ToString()}", "ERROR");
    }
}
