using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Wpf;
using TB.Features;
using TB.Features.Tabs;

namespace TB.Core;

// ⚠️ No base class declaration - XAML defines <Window>, code-behind is partial
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
            BrowserView.CoreWebView2.Navigate(ViewModel.SelectedTab.Url);
            BrowserView.CoreWebView2.NavigationCompleted += (s, args) =>
                ViewModel.SelectedTab!.Title = string.IsNullOrEmpty(BrowserView.CoreWebView2.DocumentTitle)
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

    // Title bar drag
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // Window controls
    private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMax_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    // Navigation
    private void GoBack_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoBack();
    private void GoForward_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoForward();
    private void Reload_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.Reload();

    // Omnibox
    private void Omnibox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var query = Omnibox.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;
        var url = query.Contains(".") && !query.StartsWith("http") ? $"https://{query}" : $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
        BrowserView.CoreWebView2?.Navigate(url);
        Omnibox.Text = url;
    }

    // Tab close
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TabViewModel tab)
            tab.Close();
    }

    // Settings (stub for Phase 3)
    private void OpenSettings_Click(object sender, RoutedEventArgs e) { /* Phase 3 */ }
}
