using System.IO;

namespace TradingBrowser.Helpers;

public static class PathHelper
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string UserDataFolder => Path.Combine(BaseDirectory, "UserData");
    public static string LogsFolder => Path.Combine(BaseDirectory, "logs");
    public static string DatabasePath => Path.Combine(UserDataFolder, "browser.db");
}
