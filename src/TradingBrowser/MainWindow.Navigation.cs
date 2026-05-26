using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

    // EDGE UI: Omnibox Focus & Hover Animations
    private void SetupOmniboxAnimations()
    {
        Omnibox.GotFocus += (s, e) => {
            // FIX: Use Application.Current.Resources for global WinUI 3 theme brushes
            OmniboxBorder.Background = (Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"];
            OmniboxBorder.BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            OmniboxBorder.BorderThickness = new Thickness(1.5);
        };
        
        Omnibox.LostFocus += (s, e) => {
            OmniboxBorder.Background = (Brush)Application.Current.Resources["ControlFillColorDefaultBrush"];
            OmniboxBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            OmniboxBorder.BorderThickness = new Thickness(0);
        };
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
    
    private async void Settings_Click(object sender, RoutedEventArgs e) 
    { 
        var dialog = new ContentDialog
        {
            Title = "Settings",
            Content = "Native settings panel coming soon.",
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
