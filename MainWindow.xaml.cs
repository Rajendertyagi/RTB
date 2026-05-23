using System;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core; // ADDED: Fixes CS0246
using TB_Browser.Services;
using TB_Browser.ViewModels;

namespace TB_Browser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private AppWindow? _appWindow;
    private OverlappedPresenter? _presenter;

    public MainWindow()
    {
        ViewModel = App.Services!.GetRequiredService<MainViewModel>();
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTabView);
        _appWindow = this.AppWindow;
        _presenter = _appWindow?.Presenter as OverlappedPresenter;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await BrowserWebView.EnsureCoreWebView2Async();
        BrowserWebView.NavigationStarting += OnNavigationStarting;
        BrowserWebView.NavigationCompleted += OnNavigationCompleted;
        var settingsService = App.Services!.GetRequiredService<SettingsService>();
        await settingsService.LoadAsync();
        ViewModel.InitializeTabs();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs e)
    {
        App.Services?.GetService<BookmarkService>()?.FlushAsync().Wait();
        App.Services?.GetService<HistoryService>()?.FlushAsync().Wait();
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

    private void AppTabView_AddTabButtonClick(TabView sender, object args) => ViewModel.AddTab("https://www.google.com", "New Tab");
    private void AppTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab.DataContext is TabViewModel tab) tab.CloseCommand.Execute(null);
    }

    private void AddressBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var url = args.QueryText?.Trim();
        if (string.IsNullOrEmpty(url)) return;
        if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.Contains("."))
            url = $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
        else if (!url.StartsWith("http")) url = "https://" + url;
        if (BrowserWebView.CoreWebView2 != null) BrowserWebView.Navigate(url);
    }
}
