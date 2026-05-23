using System;
using System.IO;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace TB;

public static class Program
{
    private static string LogPath => Path.Combine(AppContext.BaseDirectory, "TB_Startup.log");

    private static void Log(string msg, Exception? ex = null)
    {
        var text = $"[{DateTime.Now:O}] {msg}{(ex != null ? $"\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : "")}\n";
        File.AppendAllText(LogPath, text);
        Console.Error.Write(text);
    }

    [STAThread]
    public static void Main(string[] args)
    {
        Log("=== TB Startup Sequence ===");
        try
        {
            Log("1. Initializing Windows App SDK Bootstrap...");
            // 0x00010000 = v1.0+. Compatible with all 1.x/2.x runtimes.
            Bootstrap.Initialize(0x00010000);
            Log("   ✅ Bootstrap OK");

            Log("2. Initializing WinRT COM Wrappers...");
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Log("   ✅ COM Wrappers OK");

            Log("3. Starting Application...");
            Application.Start(p =>
            {
                var ctx = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(ctx);
                new App();
                Log("   ✅ App instantiated");
            });

            Log("=== Startup Successful ===");
        }
        catch (Exception ex)
        {
            Log("❌ FATAL ERROR DURING STARTUP", ex);
            Log("📌 Likely missing components on LTSC:");
            Log("   1. Windows App SDK Runtime → winget install Microsoft.WindowsAppRuntime.1.5");
            Log("   2. WebView2 Runtime       → winget install Microsoft.EdgeWebView2Runtime");
            Log("   3. VC++ Redistributable     → winget install Microsoft.VCRedist.2015+.x64");
            Environment.Exit(1);
        }
    }
}
