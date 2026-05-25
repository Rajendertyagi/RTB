using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using TradingBrowser.ViewModels;
using TradingBrowser.Services;
using TradingBrowser.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using System.Collections.Generic;
using System.Linq;

namespace TradingBrowser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    private bool _isWebViewInitialized;
    
    // Core Services
    private readonly SessionService _sessionService;
    private readonly ShortcutService _shortcutService;
    private readonly HistoryBookmarkService _hbService;
    private readonly DownloadService _downloadService; // NEW: Manages file downloads & history

    // JavaScript Injectors
    private readonly string _shortcutsJs;
    private readonly string _tradingViewJs;

    public MainWindow()
    {
        this.InitializeComponent();
        RootGrid.DataContext = this; 
        
        // Enforce Dark Theme on the root content
        if (this.Content is FrameworkElement content) content.RequestedTheme = ElementTheme.Dark;

        // Initialize all backend services
        _sessionService = new SessionService(App.Db!);
        _hbService = new HistoryBookmarkService(App.Db!);
        _downloadService = new DownloadService(App.Db!);
        
        // Initialize Shortcut Service with WebView access delegate
        _shortcutService = new ShortcutService(
            ViewModel, 
            () => _isWebViewInitialized ? MainWebView.CoreWebView2 : null
        );

        // Hook up the Bookmark shortcut event (Ctrl+D)
        _shortcutService.BookmarkRequested += () => {
            if (ViewModel.SelectedTab != null)
            {
                ToggleBookmark(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title);
            }
        };

        // Load JavaScript Injectors from disk
        string shortcutsPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "shortcuts.js");
        _shortcutsJs = File.Exists(shortcutsPath) ? File.ReadAllText(shortcutsPath) : "";

        string tvJsPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "tradingview-tweaks.js");
        _tradingViewJs = File.Exists(tvJsPath) ? File.ReadAllText(tvJsPath) : "";

        SetupTitleBar();
        SetupEventHooks();
        _ = InitializeWebViewAsync();
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
        // Route raw UI events to the ShortcutService
        RootGrid.PointerPressed += (s, e) => _shortcutService.HandlePointerPressed(e);
        RootGrid.KeyDown += (s, e) => _shortcutService.HandleUiKeyDown(e);
        
        ViewModel.NavigationRequested += url => { if (_isWebViewInitialized) MainWebView.CoreWebView2.Navigate(url); };
        ViewModel.FocusOmniboxRequested += () => { Omnibox.Focus(FocusState.Programmatic); Omnibox.SelectAll(); };
        ViewModel.ToggleFullscreenRequested += ToggleFullscreen;
        ViewModel.OpenDevToolsRequested += () => { if (_isWebViewInitialized) MainWebView.CoreWebView2.OpenDevToolsWindow(); };

        // Save session to SQLite when the window is closing
        this.AppWindow.Closing += (s, e) => {
            if (ViewModel.SelectedTab != null)
                _sessionService.SaveSession(ViewModel.Tabs, ViewModel.SelectedTab.Id.ToString());
        };
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            string userDataFolder = Path.Combine(AppContext.BaseDirectory, "UserData", "Profile");
            Directory.CreateDirectory(userDataFolder);

            // Bypass C# compiler overload bugs using official WebView2 environment variables
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-features=msWebView2CodeCache --force-gpu-rasterization");
            Environment.SetEnvironmentVariable("WEBVIEW2_LANGUAGE", "en-US");

            await MainWebView.EnsureCoreWebView2Async();
            
            var settings = MainWebView.CoreWebView2.Settings;
            settings.IsStatusBarEnabled = false;
            settings.AreDefaultContextMenusEnabled = true;
            settings.IsGeneralAutofillEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsPinchZoomEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
            
            // Attach CoreWebView2 event handlers
            MainWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            MainWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            MainWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            MainWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            
            // NEW: Hook Download Manager to intercept and log file downloads
            _downloadService.Initialize(MainWebView.CoreWebView2);
            
            // Inject JavaScript files for shortcuts and TradingView tweaks
            if (!string.IsNullOrEmpty(_shortcutsJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_shortcutsJs);
            if (!string.IsNullOrEmpty(_tradingViewJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_tradingViewJs);

            _isWebViewInitialized = true;
            LoggingService.Log("WebView2 initialized successfully via Environment Variables.");

            // Session Restore Logic based on User Settings
            bool shouldRestore = SettingsService.Get("RestoreSession", "true") == "true";
            if (shouldRestore)
            {
                var restoredTabs = _sessionService.LoadSession(out string? activeId);
                ViewModel.InitializeSession(restoredTabs, activeId);
            }
            else
            {
                ViewModel.InitializeSession(new List<TabViewModel>(), null);
            }
            
            // Refresh Sidebar with data from DB on startup
            RefreshSidebar();
        }
        catch (Exception ex)
        {
            LoggingService.Error("WebView2 Init Error", ex);
        }
    }

    /// <summary>
    /// Refreshes the Sidebar ListViews with the latest data from the SQLite database.
    /// </summary>
    private void RefreshSidebar()
    {
        var b = _hbService.GetBookmarks();
        var h = _hbService.GetHistory();
        
        var bookmarkList = new List<ViewModels.BookmarkItem>();
        foreach(var item in b) bookmarkList.Add(new ViewModels.BookmarkItem { Url = item.Url, Title = item.Title });
        BookmarkListView.ItemsSource = bookmarkList;

        var historyList = new List<ViewModels.HistoryItem>();
        foreach(var item in h) historyList.Add(new ViewModels.HistoryItem { Url = item.Url, Title = item.Title, VisitTime = item.Time });
        HistoryListView.ItemsSource = historyList;

        // Update Star icon state for current tab
        if (ViewModel.SelectedTab != null)
        {
            bool isBookmarked = _hbService.IsBookmarked(ViewModel.SelectedTab.Url);
            BookmarkButton.Content = isBookmarked ? "★" : "☆";
        }
    }

    /// <summary>
    /// Toggles the bookmark status of the current URL.
    /// </summary>
    private void ToggleBookmark(string url, string title)
    {
        if (string.IsNullOrEmpty(url)) return;
        
        bool isBookmarked = _hbService.IsBookmarked(url);
        
        if (isBookmarked)
        {
            _hbService.RemoveBookmark(url);
            BookmarkButton.Content = "☆";
            LoggingService.Log($"Removed bookmark: {title}");
        }
        else
        {
            _hbService.AddBookmark(url, title);
            BookmarkButton.Content = "★";
            LoggingService.Log($"Added bookmark: {title}");
        }
        
        RefreshSidebar();
    }

    // --- Event Handlers for ListViews ---
    private void BookmarkListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BookmarkListView.SelectedItem is ViewModels.BookmarkItem item)
        {
            ViewModel.NavigateToUrlCommand.Execute(item.Url);
            MainSplitView.IsPaneOpen = false; 
            BookmarkListView.SelectedItem = null;
        }
    }

    private void HistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryListView.SelectedItem is ViewModels.HistoryItem item)
        {
            ViewModel.NavigateToUrlCommand.Execute(item.Url);
            MainSplitView.IsPaneOpen = false;
            HistoryListView.SelectedItem = null;
        }
    }

    // --- UI Click Handlers ---
    private void Bookmark_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTab != null)
        {
            ToggleBookmark(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title);
        }
    }
    
    // NEW: Opens the Downloads History page
    private void Downloads_Click(object sender, RoutedEventArgs e)
    {
        if (_isWebViewInitialized)
        {
            MainWebView.CoreWebView2.Navigate("about:downloads");
        }
    }
    
    private void Back_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoBack) MainWebView.CoreWebView2.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoForward) MainWebView.CoreWebView2.GoForward(); }
    private void Reload_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized) MainWebView.CoreWebView2.Reload(); }
    private void Home_Click(object sender, RoutedEventArgs e) { ViewModel.GoHomeCommand.Execute(null); }
    private void CloseTab_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement el && el.DataContext is TabViewModel tab) ViewModel.CloseTabCommand.Execute(tab); }
    private void NewTab_Click(object sender, RoutedEventArgs e) { ViewModel.AddTabCommand.Execute(null); }

    // --- WebView Message Router ---
    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        string? msg = args.TryGetWebMessageAsString();
        if (msg == null) return;

        // Route Shortcut messages
        if (msg.StartsWith("SHORTCUT:"))
        {
            _shortcutService.HandleWebViewMessage(msg);
        }
        // Route JS Error logs
        else if (msg.StartsWith("LOG:"))
        {
            LoggingService.Log(msg, "WEBVIEW_JS");
        }
        // NEW: Handle Download Page Interactions
        else if (msg.StartsWith("REMOVE_DOWNLOAD:"))
        {
            int id = int.Parse(msg.Replace("REMOVE_DOWNLOAD:", ""));
            using var conn = App.Db!.GetConnection(); conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Downloads WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            LoadDownloadsPage(); // Refresh UI after deletion
        }
        else if (msg == "CLEAR_ALL_DOWNLOADS")
        {
            using var conn = App.Db!.GetConnection(); conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Downloads;";
            cmd.ExecuteNonQuery();
            LoadDownloadsPage();
        }
    }

    // --- Fullscreen Toggle ---
    private void ToggleFullscreen()
    {
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            if (presenter.State == OverlappedPresenterState.Maximized && !ExtendsContentIntoTitleBar)
            {
                presenter.Restore();
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }
            else
            {
                ExtendsContentIntoTitleBar = false;
                SetTitleBar(null);
                presenter.SetBorderAndTitleBar(false, false);
                presenter.Maximize();
            }
        }
    }

    // --- WebView State Sync ---
    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;
        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab) oldTab.Url = MainWebView.CoreWebView2.Source;
        
        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;
        if (MainWebView.CoreWebView2.Source != newTab.Url) MainWebView.CoreWebView2.Navigate(newTab.Url);
        
        bool isBookmarked = _hbService.IsBookmarked(newTab.Url);
        BookmarkButton.Content = isBookmarked ? "★" : "☆";
    }

    private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        // NEW: Intercept internal 'about:downloads' URI and render custom HTML instead
        if (args.Uri == "about:downloads")
        {
            args.Cancel = true;
            LoadDownloadsPage();
            return;
        }

        if (ViewModel.SelectedTab != null) 
        { 
            ViewModel.OmniboxText = args.Uri; 
            ViewModel.SelectedTab.Url = args.Uri; 
            ViewModel.SelectedTab.IsLoading = true; 
        }
    }

    private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (ViewModel.SelectedTab != null) 
        { 
            ViewModel.SelectedTab.IsLoading = false; 
            if (!args.IsSuccess) LoggingService.Error($"Nav Failed: {args.WebErrorStatus}");
        }
        
        BackButton.IsEnabled = sender.CanGoBack;
        ForwardButton.IsEnabled = sender.CanGoForward;

        // Add to History if navigation was successful
        if (args.IsSuccess && ViewModel.SelectedTab != null)
        {
            _hbService.AddHistory(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title);
            RefreshSidebar();
        }
    }

    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        if (ViewModel.SelectedTab != null) 
        {
            ViewModel.SelectedTab.Title = sender.DocumentTitle;
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

    // --- NEW: Download History Page Generator ---
    /// <summary>
    /// Generates and injects a dark-themed HTML page showing download history from SQLite.
    /// </summary>
    private void LoadDownloadsPage()
    {
        var records = _downloadService.GetHistory();
        var grouped = records.GroupBy(r => r.StartTime.ToString("MMM dd, yyyy"));
        
        string itemsHtml = "";
        foreach (var group in grouped)
        {
            itemsHtml += $"<div class='date-header'>{group.Key}</div>";
            foreach (var item in group)
            {
                // Determine status color based on state
                string statusColor = item.State == "Completed" ? "#4CAF50" : (item.State == "Failed" ? "#F44336" : "#FFC107");
                itemsHtml += $@"
                <div class='download-item'>
                    <div class='icon'>📄</div>
                    <div class='info'>
                        <div class='name'>{System.Net.WebUtility.HtmlEncode(item.FileName)}</div>
                        <div class='status' style='color:{statusColor}'>{item.State}</div>
                    </div>
                    <div class='actions'>
                        <button onclick=""copyLink('{System.Net.WebUtility.HtmlEncode(item.SourceUrl)}')"">🔗</button>
                        <button onclick=""removeDownload({item.Id})"">️</button>
                    </div>
                </div>";
            }
        }

        if (string.IsNullOrEmpty(itemsHtml))
        {
            itemsHtml = "<div class='empty-state'>No downloads found.</div>";
        }

        // Full HTML/CSS template matching the requested screenshot style
        string html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ background-color: #202124; color: #e8eaed; font-family: 'Segoe UI', sans-serif; margin: 0; padding: 20px; }}
                .header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; border-bottom: 1px solid #3c4043; padding-bottom: 10px; }}
                .header h2 {{ margin: 0; font-weight: 500; }}
                .clear-btn {{ background: #303134; border: 1px solid #5f6368; color: #8ab4f8; padding: 5px 15px; border-radius: 4px; cursor: pointer; }}
                .clear-btn:hover {{ background: #3c4043; }}
                .date-header {{ color: #9aa0a6; font-size: 12px; font-weight: bold; margin-top: 20px; margin-bottom: 10px; padding-left: 10px; }}
                .download-item {{ display: flex; align-items: center; background: #303134; padding: 10px; margin-bottom: 8px; border-radius: 4px; }}
                .icon {{ font-size: 24px; margin-right: 15px; }}
                .info {{ flex-grow: 1; }}
                .name {{ font-size: 14px; margin-bottom: 4px; }}
                .status {{ font-size: 12px; }}
                .actions button {{ background: none; border: none; color: #9aa0a6; font-size: 16px; cursor: pointer; padding: 0 5px; }}
                .actions button:hover {{ color: #e8eaed; }}
                .empty-state {{ text-align: center; color: #9aa0a6; margin-top: 50px; }}
            </style>
        </head>
        <body>
            <div class='header'>
                <h2>Downloads</h2>
                <button class='clear-btn' onclick=""clearAll()"">Clear all</button>
            </div>
            {itemsHtml}
            <script>
                function copyLink(url) {{ window.chrome.webview.postMessage('COPY_LINK:' + url); }}
                function removeDownload(id) {{ window.chrome.webview.postMessage('REMOVE_DOWNLOAD:' + id); }}
                function clearAll() {{ window.chrome.webview.postMessage('CLEAR_ALL_DOWNLOADS'); }}
            </script>
        </body>
        </html>";

        MainWebView.NavigateToString(html);
    }
}
