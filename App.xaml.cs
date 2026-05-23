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
        InitializeComponent();
        // ✅ Required for unpackaged WinUI 3. Must run before any XAML parses.
        Bootstrap.Initialize(0x00010000);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        window.Activate();
    }
}
