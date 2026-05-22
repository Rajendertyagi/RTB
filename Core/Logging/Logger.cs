using System;
using System.IO;
using System.Text;

namespace TB_Browser.Core.Logging
{
    public static class Logger
    {
        private static readonly string _path;
        private static readonly object _lock = new();

        // Static constructor runs once on first access
        static Logger()
        {
            try
            {
                // 1. Use a safe, writable folder (Local App Data)
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDir = Path.Combine(appData, "TB-Browser", "logs");
                
                // 2. Ensure folder exists
                Directory.CreateDirectory(logDir);
                
                // 3. Set final log file path
                _path = Path.Combine(logDir, "app.log");
                
                Console.WriteLine($"Logger initialized at: {_path}");
            }
            catch (Exception ex)
            {
                // Fallback in case of permission issues
                _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                Console.WriteLine($"Logger fallback path: {_path}. Error: {ex.Message}");
            }
        }

        public static void Info(string src, string msg) => Write("INFO", src, msg);
        public static void Warning(string src, string msg) => Write("WARN", src, msg);
        public static void Error(string src, string msg) => Write("ERR", src, msg);
        public static void Debug(string src, string msg) => Write("DBG", src, msg);

        private static void Write(string lvl, string src, string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{lvl}] [{src}] {msg}";
            
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // If writing to file fails, print to console (Debug mode) or silent ignore (Release)
                    System.Diagnostics.Debug.WriteLine($"Log Write Failed: {ex.Message}");
                }
            }
        }
    }
}
