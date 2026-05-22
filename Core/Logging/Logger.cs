using System;
using System.IO;
using System.Text;

namespace TB_Browser.Core.Logging
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public static class Logger
    {
        private static readonly string _path;
        private static readonly object _lock = new();
        // ✅ Debug enabled by default
        private static LogLevel _minLevel = LogLevel.Debug; 

        static Logger()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string logDir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(logDir);
                _path = Path.Combine(logDir, "app.log");
                
                // Read log level from config file if it exists
                var configPath = Path.Combine(baseDir, "loglevel.txt");
                if (File.Exists(configPath))
                {
                    var levelStr = File.ReadAllText(configPath).Trim().ToUpper();
                    if (Enum.TryParse<LogLevel>(levelStr, out var level))
                        _minLevel = level;
                }
                
                Console.WriteLine($"Logger initialized: {_path} (Level: {_minLevel})");
            }
            catch (Exception ex)
            {
                _path = Path.Combine(Path.GetTempPath(), "tb-browser.log");
                Console.WriteLine($"Logger fallback: {_path}. Error: {ex.Message}");
            }
        }

        public static void SetLogLevel(LogLevel level) => _minLevel = level;
        public static LogLevel GetLogLevel() => _minLevel;

        public static void Info(string src, string msg) => Write(LogLevel.Info, "INFO", src, msg);
        public static void Warning(string src, string msg) => Write(LogLevel.Warning, "WARN", src, msg);
        public static void Error(string src, string msg) => Write(LogLevel.Error, "ERR", src, msg);
        public static void Debug(string src, string msg) => Write(LogLevel.Debug, "DBG", src, msg);

        private static void Write(LogLevel level, string levelStr, string src, string msg)
        {
            // Filter logs based on min level
            if (level < _minLevel) return; 
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var line = $"[{timestamp}] [{levelStr}] [{src}] {msg}";
            
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LOG FAILED: {ex.Message}");
                }
            }
        }
    }
}
