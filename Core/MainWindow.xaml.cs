using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Wpf;
using TB.Features;
using TB.Features.Navigation;
using TB.Features.Tabs;

namespace TB.Core;

public partial class MainWindow
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new TabService(), new NavigationViewModel());
        DataContext = ViewModel;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await BrowserView.EnsureCoreWebView2Async();
        if (BrowserView.CoreWebView2 != null && ViewModel.SelectedTab != null)
        {
            BrowserView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            BrowserView.CoreWebView2.Navigate(ViewModel.SelectedTab.Url);
            BrowserView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }
    }

    private void CoreWebView2_SourceChanged(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2SourceChangedEventArgs e) =>
        Omnibox.Text = BrowserView.CoreWebView2?.Source ?? string.Empty;

    private void CoreWebView2_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        if (ViewModel.SelectedTab != null && BrowserView.CoreWebView2 != null)
            ViewModel.SelectedTab.Title = string.IsNullOrEmpty(BrowserView.CoreWebView2.DocumentTitle) ? "Google" : BrowserView.CoreWebView2.DocumentTitle;
    }

    private async void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrowserView.CoreWebView2 == null || e.AddedItems.Count == 0) return;
        if (e.AddedItems[0] is TabViewModel tab)
        {
            await BrowserView.EnsureCoreWebView2Async();
            BrowserView.CoreWebView2.Navigate(tab.Url);
        }
    }

    private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMax_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    private void AddTab_Click(object sender, RoutedEventArgs e) => ViewModel.AddTab();
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TabViewModel tab) tab.Close();
    }
    private void GoBack_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoBack();
    private void GoForward_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoForward();
    private void Reload_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.Reload();
    private void Omnibox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox tb) return;
        var url = tb.Text?.Trim();
        if (string.IsNullOrEmpty(url)) return;
        url = url.Contains(".") && !url.StartsWith("http") ? $"https://{url}" : $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
        BrowserView.CoreWebView2?.Navigate(url);
        tb.Text = url;
    }
    private void OpenSettings_Click(object sender, RoutedEventArgs e) { }
}
