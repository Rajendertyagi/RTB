using System;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using TB_Browser.Services;
using TB_Browser.ViewModels;

namespace TB_Browser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private AppWindow? _appWindow;
    private OverlappedPresenter? _presenter;
    private TabViewModel? _previousTab;

    public MainWindow()
    {
        ViewModel = App.Services!.GetRequiredService<MainViewModel>();
        InitializeComponent();

        // Setup Custom Title Bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTabView);

        _appWindow = this.AppWindow;
        _presenter = _appWindow?.Presenter as OverlappedPresenter;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 1. Initialize WebView2
        await BrowserWebView.EnsureCoreWebView2Async();
        
        // 2. Wire Events
        BrowserWebView.NavigationStarting += OnNavigationStarting;
        BrowserWebView.NavigationCompleted += OnNavigationCompleted;
        BrowserWebView.CoreWebView2InitializationCompleted += OnCoreWebView2Init;
        BrowserWebView.KeyDown += OnWebViewKeyDown; // For F12 DevTools

        // 3. Load Settings & Theme
        var settingsService = App.Services!.GetRequiredService<SettingsService>();
        await settingsService.LoadAsync();

        // 4. Initialize Tabs
        ViewModel.InitializeTabs();
        SetupTabSuspension();
    }

    private void OnCoreWebView2Init(CoreWebView2 sender, CoreWebView2InitializationCompletedEventArgs args)
    {
        // Apply privacy settings
        var settings = App.Services!.GetRequiredService<SettingsService>().Settings;
        if (settings.BlockThirdPartyCookies)
        {
            sender.CookieManager.DeleteAllCookies(); // Clear existing
            // Note: WebView2 doesn't have a direct "block 3rd party" toggle via API yet.
            // This is typically handled via WebResourceRequested interception in production.
        }
    }

    private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (ViewModel.SelectedTab != null)
            ViewModel.SelectedTab.IsBusy = true;
    }

    private void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (ViewModel.SelectedTab == null) return;

        ViewModel.SelectedTab.IsBusy = false;
        ViewModel.SelectedTab.Url = sender.Source;
        ViewModel.SelectedTab.Title = sender.DocumentTitle;

        // Log history & queue flush trigger
        var historyService = App.Services!.GetRequiredService<HistoryService>();
        historyService.QueueVisit(sender.Source, sender.DocumentTitle);
        _ = historyService.FlushAsync(); // Fire & forget

        // Update address bar
        ViewModel.NavigationViewModel.AddressBarText = sender.Source;
    }

    private void OnWebViewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.F12 && BrowserWebView.CoreWebView2 != null)
        {
            BrowserWebView.CoreWebView2.OpenDevToolsWindow();
        }
    }

    private void SetupTabSuspension()
    {
        AppTabView.SelectionChanged += async (s, e) =>
        {
            if (_previousTab != null && _previousTab.IsSuspended && BrowserWebView.CoreWebView2 != null)
            {
                // Note: WebView2 suspension is per-control. If sharing one WebView2 across tabs,
                // we just navigate. If using multiple WebView2 controls per tab, we'd call TrySuspendAsync.
                // For single-WebView2 architecture, we rely on navigation state.
            }
            _previousTab = ViewModel.SelectedTab;
        };
    }

    // --- TabView Events ---
    private void AppTabView_AddTabButtonClick(TabView sender, object args)
    {
        ViewModel.AddTab("https://www.google.com", "New Tab");
    }

    private void AppTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab.DataContext is TabViewModel tab)
            tab.CloseCommand.Execute(null);
    }

    // --- Navigation Toolbar Events ---
    private void NavBtn_Click(object sender, RoutedEventArgs e)
    {
        if (BrowserWebView.CoreWebView2 == null) return;
        var tag = (sender as Button)?.Tag?.ToString();
        
        switch (tag)
        {
            case "Back": if (BrowserWebView.CanGoBack) BrowserWebView.GoBack(); break;
            case "Forward": if (BrowserWebView.CanGoForward) BrowserWebView.GoForward(); break;
            case "Refresh": BrowserWebView.Reload(); break;
        }
    }

    private void AddressBar_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            _ = ViewModel.NavigationViewModel.LoadSuggestionsCommand.ExecuteAsync(sender.Text);
        }
    }

    private void AddressBar_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        ViewModel.NavigationViewModel.AddressBarText = args.SelectedItem?.ToString() ?? string.Empty;
    }

    private void AddressBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var url = args.QueryText?.Trim();
        if (string.IsNullOrEmpty(url)) return;

        // Basic URL detection
        if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.Contains("."))
            url = $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
        else if (!url.StartsWith("http"))
            url = "https://" + url;

        if (BrowserWebView.CoreWebView2 != null)
            BrowserWebView.Navigate(url);
    }

    // --- Window Controls ---
    private void WindowBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_presenter == null) return;
        var tag = (sender as Button)?.Tag?.ToString();
        
        switch (tag)
        {
            case "Minimize": _presenter.Minimize(); break;
            case "Maximize": 
                if (_presenter.State == OverlappedPresenterState.Maximized) _presenter.Restore();
                else _presenter.Maximize(); 
                break;
            case "Close": Close(); break;
        }
    }
}
