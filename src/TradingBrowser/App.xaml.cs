using Microsoft.UI.Xaml;
using TradingBrowser.Services;
using Microsoft.Data.Sqlite; // FIX: Changed from 'SQLite' to 'Microsoft.Data.Sqlite'
using System;
using System.IO;
using System.Threading.Tasks;

namespace TradingBrowser;

public partial class App : Application
{
    // FIX: Changed to SqliteConnection (lowercase 'l')
    public static SqliteConnection? Db { get; private set; } 
    private Window? m_window;

    public App()
    {
        this.InitializeComponent();
        
        this.UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            LoggingService.Info("App startup initiated.");

            // --- PASTE YOUR EXACT ORIGINAL DB INITIALIZATION HERE ---
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TradingBrowser");
            if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
            
            string dbPath = Path.Combine(appDataPath, "data.db");
            
            // FIX: Using Microsoft.Data.Sqlite connection string format
            Db = new SqliteConnection($"Data Source={dbPath}");
            Db.Open(); // Microsoft.Data.Sqlite requires explicit Open()
            
            // If you had table creation logic, put it here using Db.CreateCommand()
            
            LoggingService.Info("Database schema initialized successfully.");
            // --------------------------------------------------------

            m_window = new MainWindow();
            m_window.Activate();
        }
        catch (Exception ex)
        {
            LoggingService.Error("Fatal error during app startup", ex);
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LoggingService.Error("UI Thread Unhandled Exception", e.Exception);
        e.Handled = true; 
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LoggingService.Error("AppDomain Fatal Exception", ex);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggingService.Error("Unobserved Background Task Exception", e.Exception);
        e.SetObserved(); 
    }
}
