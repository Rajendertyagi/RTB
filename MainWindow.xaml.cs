using System;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core; // Fixes CoreWebView2InitializationCompletedEventArgs
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
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTabView);
        _appWindow = this.AppWindow;
        _presenter = _appWindow?.Presenter as OverlappedPresenter;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await BrowserWebView.EnsureCoreWebView2Async();
        BrowserWebView.NavigationStarting += OnNavigationStarting;
        BrowserWebView.NavigationCompleted += OnNavigationCompleted;
        BrowserWebView.CoreWebView2InitializationCompleted += OnCoreWebView2Init;
        BrowserWebView.KeyDown += OnWebViewKeyDown;
        var settingsService = App.Services!.GetRequiredService<SettingsService>();
        await settingsService.LoadAsync();
        ViewModel.InitializeTabs();
        SetupTabSuspension();
    }

    private void OnCoreWebView2Init(CoreWebView2 sender, CoreWebView2InitializationCompletedEventArgs args)
    {
        var settings = App.Services!.GetRequiredService<SettingsService>().Settings;
        if (settings.BlockThirdPartyCookies)
            sender.CookieManager.DeleteAllCookies();
    }

    private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (ViewModel.SelectedTab != null) ViewModel.SelectedTab.IsBusy = true;
    }

    private void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (ViewModel.SelectedTab == null) return;
        ViewModel.SelectedTab.IsBusy = false;
        ViewModel.SelectedTab.Url = sender.Source;
        ViewModel.SelectedTab.Title = sender.DocumentTitle;
        var historyService = App.Services!.GetRequiredService<HistoryService>();
        historyService.QueueVisit(sender.Source, sender.DocumentTitle);
        _ = historyService.FlushAsync();
        ViewModel.NavigationViewModel.AddressBarText = sender.Source;
    }

    private void OnWebViewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.F12 && BrowserWebView.CoreWebView2 != null)
            BrowserWebView.CoreWebView2.OpenDevToolsWindow();
    }

    private void SetupTabSuspension()
    {
        AppTabView.SelectionChanged += (s, e) => _previousTab = ViewModel.SelectedTab;
    }

    private void AppTabView_AddTabButtonClick(TabView sender, object args)
    {
        ViewModel.AddTab("https://www.google.com", "New Tab");
    }

    private void AppTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab.DataContext is TabViewModel tab)
            tab.CloseCommand.Execute(null);
    }

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
            _ = ViewModel.NavigationViewModel.LoadSuggestionsCommand.ExecuteAsync(sender.Text);
    }

    private void AddressBar_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        ViewModel.NavigationViewModel.AddressBarText = args.SelectedItem?.ToString() ?? string.Empty;
    }

    private void AddressBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var url = args.QueryText?.Trim();
        if (string.IsNullOrEmpty(url)) return;
        if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.Contains("."))
            url = $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
        else if (!url.StartsWith("http"))
            url = "https://" + url;
        if (BrowserWebView.CoreWebView2 != null) BrowserWebView.Navigate(url);
    }

    private void WindowBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_presenter == null) return;
        var tag = (sender as Button)?.Tag?.ToString();
        switch (tag)
        {
            case "Minimize": _presenter.Minimize(); break;
            case "Maximize": if (_presenter.State == OverlappedPresenterState.Maximized) _presenter.Restore(); else _presenter.Maximize(); break;
            case "Close": Close(); break;
        }
    }
}
