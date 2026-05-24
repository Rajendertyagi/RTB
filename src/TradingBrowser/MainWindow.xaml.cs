using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using TradingBrowser.ViewModels;
using TradingBrowser.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TradingBrowser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    private bool _isWebViewInitialized;

    public MainWindow()
    {
        this.InitializeComponent();
        
        RootGrid.DataContext = this; 
        
        if (this.Content is FrameworkElement content)
        {
            content.RequestedTheme = ElementTheme.Dark;
        }

        SetupTitleBar();
        
        // Hook into ViewModel navigation requests (Fixes Issue 2)
        ViewModel.NavigationRequested += url => {
            if (_isWebViewInitialized && MainWebView.CoreWebView2 != null) {
                MainWebView.CoreWebView2.Navigate(url);
            }
        };

        _ = InitializeWebViewAsync();
    }

    private void SetupTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar); // Now covers the whole top row

        var appWindow = this.AppWindow;
        appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            string userDataFolder = Path.Combine(AppContext.BaseDirectory, "UserData", "Profile");
            Directory.CreateDirectory(userDataFolder);

            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);

            await MainWebView.EnsureCoreWebView2Async();
            
            MainWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            MainWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            
            MainWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            // Hook Navigation Completed to log network errors (Fixes Issue 5)
            MainWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            
            _isWebViewInitialized = true;
            LoggingService.Log("WebView2 initialized successfully.");

            if (ViewModel.SelectedTab != null && ViewModel.SelectedTab.Url != "about:blank")
            {
                MainWebView.CoreWebView2.Navigate(ViewModel.SelectedTab.Url);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error("WebView2 Init Error", ex);
            
            // Auto-run bootstrapper if WebView2 runtime is missing
            string bootstrapper = Path.Combine(AppContext.BaseDirectory, "WebView2Bootstrapper.exe");
            if (File.Exists(bootstrapper))
            {
                LoggingService.Log("WebView2 runtime missing. Launching bootstrapper...");
                Process.Start(new ProcessStartInfo(bootstrapper) { UseShellExecute = true });
            }
        }
    }

    private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (ViewModel.SelectedTab != null)
        {
            ViewModel.SelectedTab.IsLoading = false;
            if (!args.IsSuccess)
            {
                // Logs errors like DNS failure, connection refused, etc.
                LoggingService.Error($"Navigation failed for {sender.Source}: {args.WebErrorStatus}");
            }
        }
    }

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;

        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab)
        {
            oldTab.Url = MainWebView.CoreWebView2.Source; 
            oldTab.CanGoBack = MainWebView.CoreWebView2.CanGoBack;
            oldTab.CanGoForward = MainWebView.CoreWebView2.CanGoForward;
        }

        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;
        
        if (MainWebView.CoreWebView2.Source != newTab.Url)
        {
            MainWebView.CoreWebView2.Navigate(newTab.Url);
        }
    }

    private void MainWebView_NavigationStarting(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (ViewModel.SelectedTab != null)
        {
            ViewModel.OmniboxText = args.Uri;
            ViewModel.SelectedTab.Url = args.Uri;
            ViewModel.SelectedTab.IsLoading = true;
        }
    }

    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        if (ViewModel.SelectedTab != null)
        {
            ViewModel.SelectedTab.Title = sender.DocumentTitle;
            ViewModel.SelectedTab.IsLoading = false;
        }
    }

    private void Omnibox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.NavigateOmniboxCommand.Execute(null);
            e.Handled = true;
        }
    }
}
