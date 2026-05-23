using System;
using System.Runtime.InteropServices;
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

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new TabService(), new NavigationViewModel());
        DataContext = ViewModel;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        SizeChanged += MainWindow_SizeChanged;
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

    private void CoreWebView2_SourceChanged(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2SourceChangedEventArgs e)
    {
        if (BrowserView.CoreWebView2?.Source != null)
        {
            Dispatcher.Invoke(() => Omnibox.Text = BrowserView.CoreWebView2.Source);
        }
    }

    private void CoreWebView2_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        if (ViewModel.SelectedTab != null && BrowserView.CoreWebView2 != null)
        {
            ViewModel.SelectedTab.Title = string.IsNullOrEmpty(BrowserView.CoreWebView2.DocumentTitle)
                ? ViewModel.SelectedTab.Url
                : BrowserView.CoreWebView2.DocumentTitle;
        }
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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMax_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void AddTab_Click(object sender, RoutedEventArgs e) => ViewModel.AddTab();

    private void GoBack_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoBack();
    private void GoForward_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoForward();
    private void Reload_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.Reload();

    private void Omnibox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox tb) return;
        var query = tb.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        var url = query.Contains(".") && !query.StartsWith("http")
            ? $"https://{query}"
            : $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
        BrowserView.CoreWebView2?.Navigate(url);
        tb.Text = url;
    }

    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn.DataContext is TabViewModel tab) { tab.Close(); return; }
            if (btn.TemplatedParent is ContentPresenter cp && cp.DataContext is TabViewModel tab2) { tab2.Close(); return; }
        }
        if (sender is TabItem ti && ti.DataContext is TabViewModel tab3) { tab3.Close(); }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e) { /* Phase 3 */ }
    private void MainWindow_Closed(object? sender, EventArgs e) => BrowserView?.Dispose();
    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) { /* Trigger layout update */ }
}
