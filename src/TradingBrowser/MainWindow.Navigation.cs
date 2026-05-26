using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using System;

namespace TradingBrowser;

public sealed partial class MainWindow
{
    private void UpdateOmniboxIcon()
    {
        string url = ViewModel.OmniboxText ?? "";
        bool isHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        bool isNewTab = string.IsNullOrWhiteSpace(url) || url == "https://www.google.com";
        
        OmniboxIcon.Glyph = (isHttps && !isNewTab) ? "\uE72E" : "\uE721";
    }

    private void Omnibox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) 
        { 
            ViewModel.NavigateOmniboxCommand.Execute(null); 
            e.Handled = true; 
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoBack) MainWebView.CoreWebView2.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoForward) MainWebView.CoreWebView2.GoForward(); }
    private void Reload_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized) MainWebView.CoreWebView2.Reload(); }
    private void Home_Click(object sender, RoutedEventArgs e) { ViewModel.GoHomeCommand.Execute(null); }
    
    // FIX 5: Native Settings Dialog instead of edge://settings
    private async void Settings_Click(object sender, RoutedEventArgs e) 
    { 
        var dialog = new ContentDialog
        {
            Title = "Settings",
            Content = "Native settings panel coming soon.\n\n(WebView2 blocks edge:// URIs for security reasons).",
            CloseButtonText = "Ok",
            XamlRoot = RootGrid.XamlRoot
        };
        await dialog.ShowAsync();
    }
    
    private void Downloads_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized) MainWebView.CoreWebView2.Navigate("about:downloads"); }

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
}
