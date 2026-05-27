using System;
using System.IO;
using System.Threading.Tasks;

namespace TradingBrowser.Services;

public static class LoggingService
{
    private static readonly object _lock = new();
    private static readonly string _logFilePath;

    static LoggingService()
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TradingBrowser",
            "logs"
        );
        
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"tradingbrowser_{DateTime.Now:yyyy-MM-dd}.log");
    }

    public static void Info(string message) => WriteLog("INFO", message);
    public static void Warning(string message) => WriteLog("WARN", message);
    
    public static void Error(string message, Exception? ex = null)
    {
        string fullMsg = ex != null ? $"{message} {ex}" : message;
        WriteLog("ERROR", fullMsg);
    }

    private static void WriteLog(string level, string message)
    {
        Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logFilePath, entry);
                }
                catch { /* Never crash the app because logging failed */ }
            }
        });
    }
}
