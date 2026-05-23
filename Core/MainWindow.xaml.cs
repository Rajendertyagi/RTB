using System;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using TB.Features.Navigation;
using TB.Features.Tabs;

namespace TB.Core;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private AppWindow? _appWindow;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new TabService(), new NavigationViewModel());
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
        _appWindow = this.AppWindow;
        _ = InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        await BrowserView.EnsureCoreWebView2Async();
        var core = BrowserView.CoreWebView2;
        if (core == null) return;

        core.NavigationStarting += (s, e) => ViewModel.OnNavigationStarting();
        core.NavigationCompleted += (s, e) => ViewModel.OnNavigationCompleted(s.Source, s.DocumentTitle);
        core.NewWindowRequested += (s, e) => { e.Handled = true; ViewModel.OpenInNewTab(e.Uri); };
    }

    private void TitleBar_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint((UIElement)sender);
        if (point.Properties.IsLeftButtonPressed)
        {
            InputNonClientPointerSource.GetForWindowId(this.AppWindow.Id).StartDraggingPointerDrag();
        }
    }

    private void BtnMin_Click(object sender, RoutedEventArgs e) => _appWindow?.Minimize();

    private void BtnMax_Click(object sender, RoutedEventArgs e)
    {
        if (_appWindow?.Presenter is OverlappedPresenter op)
        {
            if (op.IsMaximized) op.Restore();
            else op.Maximize();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void GoBack_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoBack();
    private void GoForward_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoForward();
    private void Reload_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.Reload();
    private void OpenSettings_Click(object sender, RoutedEventArgs e) { /* Phase 3 */ }

    private void Omnibox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.QueryText?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        var url = query;
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = url.Contains(".") ? $"https://{url}" : $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
        }
        BrowserView.CoreWebView2?.Navigate(url);
    }

    private void TabStrip_AddTabButtonClick(Microsoft.UI.Xaml.Controls.TabView sender, object args) => ViewModel.AddTab();

    private void TabStrip_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
    {
        // Simplified: cast DataContext and call Close() directly
        if (args.Tab.DataContext is TabViewModel tab)
        {
            tab.Close();
        }
    }
}
