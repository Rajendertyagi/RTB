using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using TradingBrowser.ViewModels;
using TradingBrowser.Services;
using TradingBrowser.Helpers;
using System;
using System.IO;
using Microsoft.UI.Windowing;

namespace TradingBrowser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    public DownloadService DownloadManager => _downloadService; 

    private bool _isWebViewInitialized;
    private bool _isSplitPaneActive;
    
    private readonly SessionService _sessionService;
    private readonly ShortcutService _shortcutService;
    private readonly HistoryBookmarkService _hbService;
    private readonly DownloadService _downloadService;
    private WebViewNavigationService? _navService; 
    
    private readonly string _shortcutsJs;
    private readonly string _tradingViewJs;

    public MainWindow()
    {
        this.InitializeComponent();
        RootGrid.DataContext = this; 
        
        if (this.Content is FrameworkElement content) 
            content.RequestedTheme = ElementTheme.Dark;

        _sessionService = new SessionService(App.Db!);
        _hbService = new HistoryBookmarkService(App.Db!);
        _downloadService = new DownloadService(App.Db!);
        
        _shortcutService = new ShortcutService(
            ViewModel, 
            () => _isWebViewInitialized ? MainWebView.CoreWebView2 : null
        );

        _shortcutService.BookmarkRequested += () => {
            if (ViewModel.SelectedTab != null)
                ToggleBookmark(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title);
        };

        ViewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(MainViewModel.SelectedTab) || e.PropertyName == nameof(MainViewModel.OmniboxText))
                UpdateOmniboxIcon();
        };

        string shortcutsPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "shortcuts.js");
        _shortcutsJs = File.Exists(shortcutsPath) ? File.ReadAllText(shortcutsPath) : "";

        string tvJsPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "tradingview-tweaks.js");
        _tradingViewJs = File.Exists(tvJsPath) ? File.ReadAllText(tvJsPath) : "";

        SetupTitleBar();
        SetupEventHooks();
        SetupOmniboxAnimations(); 
        
        RootGrid.ActualThemeChanged += RootGrid_ActualThemeChanged;
        SetupAdaptiveTabScaling();
        SetupTilingEngine();

        // ✅ FIX: Restore session BEFORE initializing WebView
        RestoreLastSession();
        
        _ = InitializeWebViewAsync();
    }

    // ✅ NEW METHOD: Restore last session
    private void RestoreLastSession()
    {
        try
        {
            LoggingService.Info("[Session] Attempting to restore last session...");
            string? activeTabId;
            var restoredTabs = _sessionService.LoadSession(out activeTabId);
            
            if (restoredTabs != null && restoredTabs.Count > 0)
            {
                ViewModel.InitializeSession(restoredTabs, activeTabId);
                LoggingService.Info($"[Session] Restored {restoredTabs.Count} tabs. Active: {activeTabId ?? "none"}");
            }
            else
            {
                LoggingService.Info("[Session] No session found. Creating fresh tab.");
                ViewModel.InitializeSession(new(), null);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error("[Session] Restore failed", ex);
            ViewModel.InitializeSession(new(), null);
        }
    }

    private void SetupTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        var appWindow = this.AppWindow;
        appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
    }

    private void SetupEventHooks()
    {
        RootGrid.PointerPressed += (s, e) => _shortcutService.HandlePointerPressed(e);
        RootGrid.KeyDown += (s, e) => _shortcutService.HandleUiKeyDown(e);
        
        ViewModel.NavigationRequested += url => { if (_isWebViewInitialized) MainWebView.CoreWebView2.Navigate(url); };
        ViewModel.FocusOmniboxRequested += () => { Omnibox.Focus(FocusState.Programmatic); Omnibox.SelectAll(); };
        ViewModel.ToggleFullscreenRequested += ToggleFullscreen;
        ViewModel.OpenDevToolsRequested += () => { if (_isWebViewInitialized) MainWebView.CoreWebView2.OpenDevToolsWindow(); };

        this.AppWindow.Closing += (s, e) => {
            if (ViewModel.SelectedTab != null)
                _sessionService.SaveSession(ViewModel.Tabs, ViewModel.SelectedTab.Id.ToString());
        };
    }

    // ✅ FIX: Back button now calls GoBack()
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoBack)
        {
            LoggingService.Info("[Nav] Back button clicked");
            MainWebView.CoreWebView2.GoBack();
        }
    }

    // ✅ FIX: Forward button now calls GoForward()
    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoForward)
        {
            LoggingService.Info("[Nav] Forward button clicked");
            MainWebView.CoreWebView2.GoForward();
        }
    }

    private void Reload_Click(object sender, RoutedEventArgs e) 
    { 
        if (_isWebViewInitialized) MainWebView.CoreWebView2.Reload(); 
    }

    private void Omnibox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.NavigateOmniboxCommand.Execute(null);
            Omnibox.ClearSelection();
        }
    }

    private void UpdateOmniboxIcon() 
    { 
        if (ViewModel.SelectedTab != null && _hbService.IsBookmarked(ViewModel.SelectedTab.Url))
        {
            BookmarkIcon.Glyph = "\uE735"; // Filled Star
        }
        else
        {
            BookmarkIcon.Glyph = "\uE734"; // Outline Star
        }
    }

    // NOTE: The following methods are REMOVED to prevent CS0111 "already defined" errors.
    // They exist in your other partial files (like MainWindow.Panes.cs or MainWindow.WebView.cs):
    // - Home_Click, Settings_Click, SplitPane_Click, ToggleFullscreen, ToggleBookmark, Bookmark_Click
    // - SetupOmniboxAnimations, RootGrid_ActualThemeChanged, InitializeWebViewAsync
}
