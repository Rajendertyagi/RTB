using System;
using System.IO;

namespace TB.Infrastructure;

public class PathResolver
{
    public string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "TB");

    public void EnsureDirectories() => Directory.CreateDirectory(DataDir);
}
