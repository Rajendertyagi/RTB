using System;
using System.IO;
using System.Reflection;

namespace TB_Browser.Infrastructure;

public class PathResolver
{
    public string DataDirectory { get; private set; } = string.Empty;
    public string LogDirectory { get; private set; } = string.Empty;
    public string DatabasePath { get; private set; } = string.Empty;

    public void EnsureDirectories()
    {
        // Strategy: Try BaseDirectory/Data first
        var baseDir = AppContext.BaseDirectory;
        var targetDataDir = Path.Combine(baseDir, "Data");
        var targetLogDir = Path.Combine(baseDir, "logs");

        // Check write permission
        try
        {
            if (!Directory.Exists(targetDataDir))
                Directory.CreateDirectory(targetDataDir);
            
            // Test write access
            var testFile = Path.Combine(targetDataDir, ".test");
            File.WriteAllText(testFile, "");
            File.Delete(testFile);

            DataDirectory = targetDataDir;
            LogDirectory = targetLogDir;
            Directory.CreateDirectory(LogDirectory); // Ensure log dir exists
        }
        catch (UnauthorizedAccessException)
        {
            // Fallback to LocalAppData
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(localAppData, "TB-Browser");
            
            DataDirectory = Path.Combine(appFolder, "Data");
            LogDirectory = Path.Combine(appFolder, "logs");
            
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(LogDirectory);
        }

        DatabasePath = Path.Combine(DataDirectory, "tb-browser.db");
    }
}
