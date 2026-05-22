using System;
using System.IO;
using System.Text;

namespace TB_Browser.Core.Logging
{
    public static class Logger
    {
        private static readonly string _path;
        private static readonly object _lock = new();

        static Logger()
        {
            try
            {
                // ✅ Use path next to executable
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string logDir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(logDir);
                _path = Path.Combine(logDir, "app.log");
                
                Console.WriteLine($"Logger initialized: {_path}");
            }
            catch (Exception ex)
            {
                // Fallback to temp folder
                _path = Path.Combine(Path.GetTempPath(), "tb-browser.log");
                Console.WriteLine($"Logger fallback: {_path}. Error: {ex.Message}");
            }
        }

        public static void Info(string src, string msg) => Write("INFO", src, msg);
        public static void Warning(string src, string msg) => Write("WARN", src, msg);
        public static void Error(string src, string msg) => Write("ERR", src, msg);
        public static void Debug(string src, string msg) => Write("DBG", src, msg);

        private static void Write(string lvl, string src, string msg)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var line = $"[{timestamp}] [{lvl}] [{src}] {msg}";
            
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
                    // Force flush to disk immediately
                    using (var fs = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.AutoFlush = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LOG FAILED: {ex.Message}");
                }
            }
        }
    }
}
