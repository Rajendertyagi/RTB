using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel; // Fixes AppLifecycleEventArgs
using TB_Browser.Infrastructure;
using TB_Browser.Repositories;
using TB_Browser.Services;
using TB_Browser.ViewModels;

namespace TB_Browser;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public App()
    {
        InitializeComponent();
        LoggingService.Init();
        LoggingService.Info("TB-Browser starting...");
        ConfigureDI();
    }

    private void ConfigureDI()
    {
        var services = new ServiceCollection();
        services.AddSingleton<PathResolver>();
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<DbInitializer>();
        services.AddSingleton<BookmarkRepository>();
        services.AddSingleton<HistoryRepository>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<FaviconService>();
        services.AddSingleton<TabService>();
        services.AddSingleton<BookmarkService>();
        services.AddSingleton<HistoryService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<SearchService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<NavigationViewModel>();
        services.AddTransient<TabViewModel>();
        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            LoggingService.Info("App launched. Initializing environment...");
            var pathResolver = Services!.GetRequiredService<PathResolver>();
            pathResolver.EnsureDirectories();
            var dbInit = Services.GetRequiredService<DbInitializer>();
            dbInit.Initialize();
            var mainWindow = new MainWindow();
            mainWindow.Activate();
            LoggingService.Info("MainWindow activated. Startup complete.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Critical startup failure", ex);
            System.Environment.Exit(1);
        }
    }

    protected override void OnClosed(object sender, AppLifecycleEventArgs args)
    {
        try
        {
            var bookmarkService = Services?.GetService<BookmarkService>();
            var historyService = Services?.GetService<HistoryService>();
            if (bookmarkService != null) bookmarkService.FlushAsync().Wait();
            if (historyService != null) historyService.FlushAsync().Wait();
            LoggingService.Info("App closed cleanly.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Shutdown error", ex);
        }
        base.OnClosed(sender, args);
    }
}
