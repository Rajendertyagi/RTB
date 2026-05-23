using System;
using System.IO;

namespace TB.Infrastructure;

public class PathResolver
{
    public string BaseDir { get; } = AppDomain.CurrentDomain.BaseDirectory;
    public string DataDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TB");
    public string DatabasePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TB", "tb.db");
    public void EnsureDirectories() => Directory.CreateDirectory(DataDir);
}
