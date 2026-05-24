using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using TradingBrowser.ViewModels;
using Windows.UI;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TradingBrowser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    private bool _isWebViewInitialized;

    public MainWindow()
    {
        this.InitializeComponent();
        
        // Set DataContext for ElementName bindings in DataTemplates
        RootGrid.DataContext = this; 
        
        this.Content.RequestedTheme = ElementTheme.Dark;
        SetupTitleBar();
        
        // Initialize WebView2 on launch
        _ = InitializeWebViewAsync();
    }

    private void SetupTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var appWindow = this.AppWindow;
        appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        appWindow.TitleBar.ButtonForegroundColor = Colors.White;
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            // Portability: Store user data relative to the executable
            string userDataFolder = Path.Combine(AppContext.BaseDirectory, "UserData", "Profile");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await MainWebView.EnsureCoreWebView2Async(env);
            
            MainWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            MainWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            
            // Hook CoreWebView2 specific events
            MainWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            
            _isWebViewInitialized = true;

            // Load the initial tab
            if (ViewModel.SelectedTab != null && ViewModel.SelectedTab.Url != "about:blank")
            {
                MainWebView.CoreWebView2.Navigate(ViewModel.SelectedTab.Url);
            }
        }
        catch (Exception ex)
        {
            // TODO: Route to LoggingService in Phase 4
            System.Diagnostics.Debug.WriteLine($"WebView2 Init Error: {ex.Message}");
        }
    }

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;

        // 1. Save state of the OLD tab
        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab)
        {
            oldTab.Url = MainWebView.CoreWebView2.Source.AbsoluteUri;
            oldTab.CanGoBack = MainWebView.CoreWebView2.CanGoBack;
            oldTab.CanGoForward = MainWebView.CoreWebView2.CanGoForward;
        }

        // 2. Load state of the NEW tab
        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;
        
        // Only navigate if the URL is different to prevent redundant loads
        if (MainWebView.CoreWebView2.Source.AbsoluteUri != newTab.Url)
        {
            MainWebView.CoreWebView2.Navigate(newTab.Url);
        }
    }

    private void MainWebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
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
   
