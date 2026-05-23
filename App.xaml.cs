using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
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
            var pathResolver = Services!.GetRequiredService<PathResolver>();
            pathResolver.EnsureDirectories();
            Services.GetRequiredService<DbInitializer>().Initialize();
            new MainWindow().Activate();
        }
        catch (Exception ex)
        {
            LoggingService.Error("Critical startup failure", ex);
            Environment.Exit(1);
        }
    }

    // FIXED: Changed AppLifecycleEventArgs to WindowEventArgs
    protected override void OnClosed(object sender, WindowEventArgs e)
    {
        try
        {
            Services?.GetService<BookmarkService>()?.FlushAsync().Wait();
            Services?.GetService<HistoryService>()?.FlushAsync().Wait();
        }
        catch (Exception ex) { LoggingService.Error("Shutdown error", ex); }
        base.OnClosed(sender, e);
    }
}
