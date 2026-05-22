using System;
using System.IO;
using System.Text;

namespace TB_Browser.Core.Logging
{
    public static class Logger
    {
        private static readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log");
        private static readonly object _lock = new();
        static Logger() => Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        public static void Info(string src, string msg) => Write("INFO", src, msg);
        public static void Warning(string src, string msg) => Write("WARN", src, msg);
        public static void Error(string src, string msg) => Write("ERR", src, msg);
        public static void Debug(string src, string msg) => Write("DBG", src, msg);

        private static void Write(string lvl, string src, string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{lvl}] [{src}] {msg}";
            lock (_lock) File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
        }
    }
}
