using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using TradingBrowser.ViewModels;
using TradingBrowser.Services;
using TradingBrowser.Helpers;
using TradingBrowser.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using System.Collections.Generic;

namespace TradingBrowser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    
    // Exposed for XAML x:Bind to access ActiveDownloads
    public DownloadService DownloadManager => _downloadService; 

    private bool _isWebViewInitialized;
    
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
        
        _ = PreWarmWebViewEnvironmentAsync();
        _ = InitializeWebViewAsync();
    }

    private async Task PreWarmWebViewEnvironmentAsync()
    {
        try
        {
            string userDataFolder = Path.Combine(AppContext.BaseDirectory, "UserData", "Profile");
            Directory.CreateDirectory(userDataFolder);
            
            var options = new CoreWebView2EnvironmentOptions("--enable-features=msWebView2CodeCache --force-gpu-rasterization");
            await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            LoggingService.Log("WebView2 Environment pre-warmed successfully.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("WebView2 Pre-warm Error", ex);
        }
    }

    private void UpdateOmniboxIcon()
    {
        string url = ViewModel.OmniboxText ?? "";
        bool isHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        bool isNewTab = string.IsNullOrWhiteSpace(url) || url == "https://www.google.com";
        OmniboxIcon.Text = (isHttps && !isNewTab) ? "🔒" : "🔍";
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

    private async Task InitializeWebViewAsync()
    {
        try
        {
            await MainWebView.EnsureCoreWebView2Async();
            
            var settings = MainWebView.CoreWebView2.Settings;
            settings.IsStatusBarEnabled = false;
            settings.AreDefaultContextMenusEnabled = true;
            settings.IsGeneralAutofillEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsPinchZoomEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
            
            MainWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            MainWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            MainWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            MainWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            
            _downloadService.Initialize(MainWebView.CoreWebView2);
            _navService = new WebViewNavigationService(_downloadService, MainWebView.CoreWebView2, this);
            
            if (!string.IsNullOrEmpty(_shortcutsJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_shortcutsJs);
            if (!string.IsNullOrEmpty(_tradingViewJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_tradingViewJs);

            _isWebViewInitialized = true;
            LoggingService.Log("WebView2 initialized successfully.");

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
            
            UpdateOmniboxIcon();
        }
        catch (Exception ex)
        {
            LoggingService.Error("WebView2 Init Error", ex);
        }
    }

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

        if (ViewModel.SelectedTab != null)
        {
            bool isBookmarked = _hbService.IsBookmarked(ViewModel.SelectedTab.Url);
            BookmarkIcon.Glyph = isBookmarked ? "\uE735" : "\uE734"; 
        }
    }

    private void ToggleBookmark(string url, string title)
    {
        if (string.IsNullOrEmpty(url)) return;
        bool isBookmarked = _hbService.IsBookmarked(url);
        
        if (isBookmarked) { _hbService.RemoveBookmark(url); BookmarkIcon.Glyph = "\uE734"; }
        else { _hbService.AddBookmark(url, title); BookmarkIcon.Glyph = "\uE735"; }
        
        if (MainSplitView.IsPaneOpen) RefreshSidebar();
    }

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

    private void Bookmark_Click(object sender, RoutedEventArgs e) { if (ViewModel.SelectedTab != null) ToggleBookmark(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title); }
    private void Downloads_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized) MainWebView.CoreWebView2.Navigate("about:downloads"); }
    private void Settings_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized) MainWebView.CoreWebView2.Navigate("about:settings"); }
    private void Back_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoBack) MainWebView.CoreWebView2.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoForward) MainWebView.CoreWebView2.GoForward(); }
    private void Reload_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized) MainWebView.CoreWebView2.Reload(); }
    private void Home_Click(object sender, RoutedEventArgs e) { ViewModel.GoHomeCommand.Execute(null); }
    private void NewTab_Click(object sender, RoutedEventArgs e) { ViewModel.AddTabCommand.Execute(null); }

    private void Library_Click(object sender, RoutedEventArgs e)
    {
        RefreshSidebar(); 
        MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
    }

    private void Tab_ContextRequested(object sender, ContextRequestedEventArgs e)
    {
        if (sender is TabItemPresenter tabPresenter && tabPresenter.DataContext is TabViewModel tabVM)
        {
            var menu = new MenuFlyout();
            
            var closeItem = new MenuFlyoutItem { Text = "Close tab" };
            closeItem.Click += (s, args) => ViewModel.CloseTabCommand.Execute(tabVM);
            menu.Items.Add(closeItem);

            var closeOtherItem = new MenuFlyoutItem { Text = "Close other tabs" };
            closeOtherItem.Click += (s, args) => 
            {
                var tabsToClose = ViewModel.Tabs.Where(t => t != tabVM).ToList();
                foreach (var t in tabsToClose) ViewModel.CloseTabCommand.Execute(t);
            };
            menu.Items.Add(closeOtherItem);

            menu.SystemBackdrop = new DesktopAcrylicBackdrop();

            if (e.TryGetPosition(tabPresenter, out Windows.Foundation.Point point))
            {
                menu.ShowAt(tabPresenter, new FlyoutShowOptions { Position = point });
            }
            else
            {
                menu.ShowAt(tabPresenter);
            }
            e.Handled = true;
        }
    }

    private void Tab_MiddleClicked(object sender, PointerRoutedEventArgs e) 
    { 
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab) 
            ViewModel.CloseTabCommand.Execute(tab); 
    }
    
    private void Tab_CloseClicked(object sender, RoutedEventArgs e) 
    { 
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab) 
            ViewModel.CloseTabCommand.Execute(tab); 
    }

    private async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        string? msg = args.TryGetWebMessageAsString();
        if (msg == null) return;

        if (msg.StartsWith("SHORTCUT:")) 
            _shortcutService.HandleWebViewMessage(msg);
        else if (msg.StartsWith("LOG:")) 
            LoggingService.Log(msg, "WEBVIEW_JS");
        else if (_navService != null) 
            await _navService.HandleWebMessageAsync(msg);
    }

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

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;
        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab) oldTab.Url = MainWebView.CoreWebView2.Source;
        
        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;
        if (MainWebView.CoreWebView2.Source != newTab.Url) MainWebView.CoreWebView2.Navigate(newTab.Url);
        UpdateOmniboxIcon();
        
        bool isBookmarked = _hbService.IsBookmarked(newTab.Url);
        BookmarkIcon.Glyph = isBookmarked ? "\uE735" : "\uE734";
    }

    private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (_navService != null && _navService.HandleSpecialUri(args.Uri))
        {
            args.Cancel = true;
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
        
        ViewModel.UpdateNavigationState(sender.CanGoBack, sender.CanGoForward);

        if (args.IsSuccess && ViewModel.SelectedTab != null)
        {
            _hbService.AddHistory(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title);
            if (MainSplitView.IsPaneOpen) RefreshSidebar();
        }
    }

    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        if (ViewModel.SelectedTab != null) ViewModel.SelectedTab.Title = sender.DocumentTitle;
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
