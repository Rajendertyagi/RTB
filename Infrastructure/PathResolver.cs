using System;
using System.IO;

namespace TB_Browser.Infrastructure;

public class PathResolver
{
    public string BaseDir { get; }
    public string DataDir { get; }  // ✅ ADDED: Resolves CS1061
    public string LogsDir { get; }
    public string DbPath { get; }

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
        catch
        {
            BaseDir = fallback;
        }

        DataDir = Path.Combine(BaseDir, "Data");
        LogsDir = Path.Combine(BaseDir, "Logs");
        DbPath = Path.Combine(DataDir, "tb-browser.db");
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(LogsDir);
    }
}
