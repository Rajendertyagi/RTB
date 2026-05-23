using System;
using System.IO;

namespace TB_Browser.Infrastructure;

public class PathResolver
{
    public string BaseDir { get; }
    public string DataDir { get; }
    public string LogsDir { get; }
    public string DatabasePath { get; } // ✅ ADDED: Matches DbConnectionFactory call

    public PathResolver()
    {
        string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TB-Browser");
        try
        {
            string testPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".write_test");
            File.Create(testPath).Dispose();
            File.Delete(testPath);
            BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        }
        catch { BaseDir = fallback; }

        DataDir = Path.Combine(BaseDir, "Data");
        LogsDir = Path.Combine(BaseDir, "Logs");
        DatabasePath = Path.Combine(DataDir, "tb-browser.db"); // ✅ Sets property
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(LogsDir);
    }
}
