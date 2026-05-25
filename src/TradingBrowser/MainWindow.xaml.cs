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

namespace TradingBrowser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    private bool _isWebViewInitialized;
    
    private readonly SessionService _sessionService;
    private readonly ShortcutService _shortcutService;
    
    private readonly string _shortcutsJs;
    private readonly string _tradingViewJs;

    public MainWindow()
    {
        this.InitializeComponent();
        RootGrid.DataContext = this; 
        
        if (this.Content is FrameworkElement content) content.RequestedTheme = ElementTheme.Dark;

        _sessionService = new SessionService(App.Db!);
        
        _shortcutService = new ShortcutService(
            ViewModel, 
            () => _isWebViewInitialized ? MainWebView.CoreWebView2 : null
        );

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
            string userDataFolder = Path.Combine(AppContext.BaseDirectory, "UserData", "Profile");
            Directory.CreateDirectory(userDataFolder);

            // Bypass CoreWebView2Environment.CreateAsync compiler bugs using environment variables
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
            
            MainWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            MainWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            MainWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            MainWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            
            if (!string.IsNullOrEmpty(_shortcutsJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_shortcutsJs);
            if (!string.IsNullOrEmpty(_tradingViewJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_tradingViewJs);

            _isWebViewInitialized = true;
            LoggingService.Log("WebView2 initialized successfully via Environment Variables.");

            // Session Restore Logic based on Settings
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
        }
        catch (Exception ex)
        {
            LoggingService.Error("WebView2 Init Error", ex);
        }
    }

    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        string? msg = args.TryGetWebMessageAsString();
        if (msg == null) return;

        if (msg.StartsWith("SHORTCUT:"))
        {
            _shortcutService.HandleWebViewMessage(msg);
        }
        else if (msg.StartsWith("LOG:"))
        {
            LoggingService.Log(msg, "WEBVIEW_JS");
        }
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

    // --- UI Click Handlers ---
    private void Back_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoBack) MainWebView.CoreWebView2.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoForward) MainWebView.CoreWebView2.GoForward(); }
    private void Reload_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized) MainWebView.CoreWebView2.Reload(); }
    private void Home_Click(object sender, RoutedEventArgs e) { ViewModel.GoHomeCommand.Execute(null); }
    private void CloseTab_Click(object sender, RoutedEventArgs e) { if (sender is FrameworkElement el && el.DataContext is TabViewModel tab) ViewModel.CloseTabCommand.Execute(tab); }
    private void NewTab_Click(object sender, RoutedEventArgs e) { ViewModel.AddTabCommand.Execute(null); }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var vm = new SettingsViewModel();
        
        var panel = new StackPanel { Spacing = 16, Width = 400 };
        
        panel.Children.Add(new TextBlock { Text = "Default Search Engine", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var comboBox = new ComboBox 
        { 
            ItemsSource = vm.SearchEngines, 
            SelectedIndex = vm.SelectedSearchEngineIndex,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        comboBox.SelectionChanged += (s, args) => vm.SelectedSearchEngineIndex = comboBox.SelectedIndex;
        panel.Children.Add(comboBox);

        var toggle = new ToggleSwitch 
        { 
            Header = "Restore last session on startup", 
            IsOn = vm.RestoreSessionOnStartup,
            Margin = new Thickness(0, 8, 0, 0)
        };
        toggle.Toggled += (s, args) => vm.RestoreSessionOnStartup = toggle.IsOn;
        panel.Children.Add(toggle);

        var clearBtn = new Button 
        { 
            Content = "Clear Browsing Data (Cache, Cookies, History)", 
            Margin = new Thickness(0, 16, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        clearBtn.Click += (s, args) => 
        {
            vm.ClearBrowsingDataCommand.Execute(null);
            clearBtn.Content = "Cleared! (Restart app to apply)";
            clearBtn.IsEnabled = false;
        };
        panel.Children.Add(clearBtn);

        var dialog = new ContentDialog
        {
            Title = "Settings",
            Content = panel,
            CloseButtonText = "Done",
            XamlRoot = this.Content.XamlRoot,
            RequestedTheme = ElementTheme.Dark
        };

        await dialog.ShowAsync();
    }

    // --- WebView State Sync ---
    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;
        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab) oldTab.Url = MainWebView.CoreWebView2.Source;
        
        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;
        if (MainWebView.CoreWebView2.Source != newTab.Url) MainWebView.CoreWebView2.Navigate(newTab.Url);
    }

    private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (ViewModel.SelectedTab != null) { ViewModel.OmniboxText = args.Uri; ViewModel.SelectedTab.Url = args.Uri; ViewModel.SelectedTab.IsLoading = true; }
    }

    private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (ViewModel.SelectedTab != null) { ViewModel.SelectedTab.IsLoading = false; if (!args.IsSuccess) LoggingService.Error($"Nav Failed: {args.WebErrorStatus}"); }
        BackButton.IsEnabled = sender.CanGoBack;
        ForwardButton.IsEnabled = sender.CanGoForward;
    }

    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        if (ViewModel.SelectedTab != null) ViewModel.SelectedTab.Title = sender.DocumentTitle;
    }

    private void Omnibox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) { ViewModel.NavigateOmniboxCommand.Execute(null); e.Handled = true; }
    }
}
