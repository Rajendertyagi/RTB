using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
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

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        window.Activate();
    }
}
