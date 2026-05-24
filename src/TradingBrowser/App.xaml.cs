using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using TB.Services;
using TB.ViewModels;

namespace TB;

public partial class App : Application
{
    public static IServiceProvider Services { get; } = new ServiceCollection()
        .AddSingleton<WebViewService>()
        .AddSingleton<TabStateManager>()
        .AddSingleton<NavigationViewModel>()
        .AddSingleton<MainViewModel>()
        .BuildServiceProvider();

    public App()
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "TB_Startup.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:O}] Bootstrap initializing...\n");
            
            // 0x00010000 = v1.0.0.0+. Compatible with SDK 2.x runtime.
            Bootstrap.Initialize(0x00010000);
            File.AppendAllText(logPath, $"[{DateTime.Now:O}] Bootstrap OK\n");
        }
        catch (Exception ex)
        {
            var msg = $"FATAL: {ex.Message}\n" +
                      "Missing: Windows App SDK Runtime or WebView2 Runtime.\n" +
                      "Fix: winget install Microsoft.WindowsAppRuntime.1.5 Microsoft.EdgeWebView2Runtime";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "TB_Error.log"), msg);
            throw;
        }

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        window.Activate();
    }
}
