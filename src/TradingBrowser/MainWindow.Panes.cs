using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using TradingBrowser.Services; // Required for LoggingService

namespace TradingBrowser;

public sealed partial class MainWindow
{
    private async void SplitPane()
    {
        if (_isSplitPaneActive) return;
        _isSplitPaneActive = true;

        LeftPaneColumn.Width = new GridLength(1, GridUnitType.Star);
        RightPaneColumn.Width = new GridLength(1, GridUnitType.Star);
        PaneDivider.Visibility = Visibility.Visible;
        RightPaneHost.Visibility = Visibility.Visible;

        try
        {
            await SecondaryWebView.EnsureCoreWebView2Async();
            SecondaryWebView.CoreWebView2.Navigate("https://www.tradingview.com");
            LoggingService.Log("Secondary WebView initialized (Split Pane).");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Secondary WebView Init Error", ex);
        }
    }

    private void CollapsePane()
    {
        if (!_isSplitPaneActive) return;
        _isSplitPaneActive = false;

        RightPaneColumn.Width = new GridLength(0);
        PaneDivider.Visibility = Visibility.Collapsed;
        RightPaneHost.Visibility = Visibility.Collapsed;
    }

    private void SplitPane_Click(object sender, RoutedEventArgs e) => SplitPane();
    
    // FIX: Handler for the new Close button in the split pane header
    private void CollapsePane_Click(object sender, RoutedEventArgs e) => CollapsePane();

    private void PaneDivider_DragDelta(object sender, DragDeltaEventArgs e)
    {
        double newLeftWidth = LeftPaneColumn.Width.Value + e.HorizontalChange;
        double totalWidth = WebViewHost.ActualWidth - 4; 
        
        if (newLeftWidth > 200 && (totalWidth - newLeftWidth) > 200)
        {
            LeftPaneColumn.Width = new GridLength(newLeftWidth, GridUnitType.Pixel);
            RightPaneColumn.Width = new GridLength(totalWidth - newLeftWidth, GridUnitType.Pixel);
        }
    }
}
