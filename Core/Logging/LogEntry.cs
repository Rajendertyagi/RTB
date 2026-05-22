using System;

namespace TB_Browser.Core.Logging
{
    public class LogEntry
    {
        public DateTime Timestamp { get; init; }
        public LogLevel Level { get; init; }
        public string Source { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public Exception? Exception { get; init; }

        public override string ToString()
        {
            var emoji = Level switch
            {
                LogLevel.Debug => "🔍",
                LogLevel.Info => "ℹ️",
                LogLevel.Warning => "⚠️",
                LogLevel.Error => "❌",
                LogLevel.Critical => "🔥",
                _ => "•"
            };
            var ex = Exception != null ? $"\n  Exception: {Exception.Message}" : "";
            return $"[{Timestamp:HH:mm:ss.fff}] {emoji} [{Level}] [{Source}] {Message}{ex}";
        }
    }
}
