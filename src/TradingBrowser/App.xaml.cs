using Microsoft.UI.Xaml;
using TradingBrowser.Services;
using System;

namespace TradingBrowser;

public partial class App : Application
{
    private Window? _window;
    public static DatabaseService? Db { get; private set; }

    public App() => this.InitializeComponent();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            LoggingService.Log("App startup initiated.");
            Db = new DatabaseService();
            
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            LoggingService.Error("Fatal error during app startup", ex);
            throw;
        }
    }
}
