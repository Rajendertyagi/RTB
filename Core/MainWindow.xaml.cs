using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using TB.Features;
using TB.Features.Navigation;
using TB.Features.Tabs;

namespace TB.Core;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private AppWindow? _appWindow;
    private OverlappedPresenter? _presenter;
    private WebView2? _webView;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new TabService(), new NavigationViewModel());
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar); // ✅ Native drag handled automatically
        _appWindow = this.AppWindow;
        _presenter = _appWindow?.Presenter as OverlappedPresenter;
        _ = InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        _webView = new WebView2();
        WebViewContainer.Children.Add(_webView);
        await _webView.EnsureCoreWebView2Async();
        var core = _webView.CoreWebView2;
        if (core == null) return;

        core.NavigationStarting += (s, e) => ViewModel.OnNavigationStarting();
        core.NavigationCompleted += (s, e) => ViewModel.OnNavigationCompleted(s.Source, s.DocumentTitle);
        core.NewWindowRequested += (s, e) => { e.Handled = true; ViewModel.OpenInNewTab(e.Uri); };
    }

    private void BtnMin_Click(object sender, RoutedEventArgs e) => _presenter?.Minimize();
    private void BtnMax_Click(object sender, RoutedEventArgs e)
    {
        if (_presenter == null) return;
        if (_presenter.State == OverlappedPresenterState.Maximized) _presenter.Restore();
        else _presenter.Maximize();
    }
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void GoBack_Click(object sender, RoutedEventArgs e) => _webView?.CoreWebView2?.GoBack();
    private void GoForward_Click(object sender, RoutedEventArgs e) => _webView?.CoreWebView2?.GoForward();
    private void Reload_Click(object sender, RoutedEventArgs e) => _webView?.CoreWebView2?.Reload();
    private void OpenSettings_Click(object sender, RoutedEventArgs e) { /* Phase 3 */ }

    private void Omnibox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.QueryText?.Trim();
        if (string.IsNullOrEmpty(query)) return;
        var url = query.Contains(".") && !query.StartsWith("http") ? $"https://{query}" : $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
        _webView?.CoreWebView2?.Navigate(url);
    }

    private void TabStrip_AddTabButtonClick(Microsoft.UI.Xaml.Controls.TabView sender, object args) => ViewModel.AddTab();
    private void TabStrip_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab.DataContext is TabViewModel tab) tab.Close();
    }
}
