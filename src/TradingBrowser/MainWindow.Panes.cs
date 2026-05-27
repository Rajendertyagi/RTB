using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using TradingBrowser.Services;
using TradingBrowser.ViewModels;

namespace TradingBrowser;

public sealed partial class MainWindow
{
    private TabViewModel? _primaryTab;
    private TabViewModel? _secondaryTab;

    // Wire up ViewModel tiling events in constructor or SetupEventHooks
    private void SetupTilingEngine()
    {
        ViewModel.TilingLayoutChanged += ApplyTilingLayout;
        ViewModel.TilingTabsChanged += SyncTiledWebViews;
    }

    private void SyncTiledWebViews(ICollection<TabViewModel> tabs)
    {
        if (tabs.Count >= 2)
        {
            _primaryTab = tabs.First();
            _secondaryTab = tabs.Skip(1).First();
            
            if (_isWebViewInitialized)
            {
                MainWebView.CoreWebView2.Navigate(_primaryTab.Url);
                SecondaryWebView.CoreWebView2.Navigate(_secondaryTab.Url);
            }
        }
    }

    private async void ApplyTilingLayout(TilingLayout layout)
    {
        TilingHeader.Visibility = layout == TilingLayout.None ? Visibility.Collapsed : Visibility.Visible;

        if (layout == TilingLayout.None)
        {
            TilingDivider.Visibility = Visibility.Collapsed;
            SecondaryWebView.Visibility = Visibility.Collapsed;
            ResetGridToSingle();
            return;
        }

        SecondaryWebView.Visibility = Visibility.Visible;
        TilingDivider.Visibility = Visibility.Visible;

        await SecondaryWebView.EnsureCoreWebView2Async();
        SecondaryWebView.CoreWebView2.DocumentTitleChanged += (s, e) => UpdateTabTitle(_secondaryTab, SecondaryWebView);
        SecondaryWebView.CoreWebView2.NavigationStarting += (s, e) => UpdateTabUrl(_secondaryTab, e.Uri);

        MainWebView.CoreWebView2.DocumentTitleChanged += (s, e) => UpdateTabTitle(_primaryTab, MainWebView);
        MainWebView.CoreWebView2.NavigationStarting += (s, e) => UpdateTabUrl(_primaryTab, e.Uri);

        switch (layout)
        {
            case TilingLayout.Horizontal:
                ConfigureHorizontalLayout();
                break;
            case TilingLayout.Vertical:
                ConfigureVerticalLayout();
                break;
            case TilingLayout.Grid:
                ConfigureGridLayout();
                break;
        }
    }

    private void ConfigureHorizontalLayout()
    {
        ResetGridToDualColumn();
        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        Grid.SetRow(SecondaryWebView, 0); Grid.SetColumn(SecondaryWebView, 1);
        Grid.SetRow(TilingDivider, 0); Grid.SetColumn(TilingDivider, 1);
        TilingDivider.Height = double.NaN; TilingDivider.Width = 4;
    }

    private void ConfigureVerticalLayout()
    {
        ResetGridToDualRow();
        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        Grid.SetRow(SecondaryWebView, 1); Grid.SetColumn(SecondaryWebView, 0);
        Grid.SetRow(TilingDivider, 1); Grid.SetColumn(TilingDivider, 0);
        TilingDivider.Width = double.NaN; TilingDivider.Height = 4;
    }

    private void ConfigureGridLayout()
    {
        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        Grid.SetRow(SecondaryWebView, 1); Grid.SetColumn(SecondaryWebView, 1); // Diagonal grid for better visibility
        TilingDivider.Visibility = Visibility.Collapsed; // Grid uses natural star sizing; divider hidden for simplicity
    }

    private void ResetGridToSingle()
    {
        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        SecondaryWebView.Visibility = Visibility.Collapsed;
    }

    private void ResetGridToDualColumn()
    {
        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }

    private void ResetGridToDualRow()
    {
        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }

    private void TilingDivider_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (ViewModel.CurrentTilingLayout == TilingLayout.Horizontal)
        {
            double newCol0Width = TilingHost.ColumnDefinitions[0].ActualWidth + e.HorizontalChange;
            double totalWidth = TilingHost.ActualWidth;
            if (newCol0Width > 150 && (totalWidth - newCol0Width) > 150)
            {
                TilingHost.ColumnDefinitions[0].Width = new GridLength(newCol0Width, GridUnitType.Pixel);
                TilingHost.ColumnDefinitions[1].Width = new GridLength(totalWidth - newCol0Width, GridUnitType.Pixel);
            }
        }
        else if (ViewModel.CurrentTilingLayout == TilingLayout.Vertical)
        {
            double newRow0Height = TilingHost.RowDefinitions[0].ActualHeight + e.VerticalChange;
            double totalHeight = TilingHost.ActualHeight;
            if (newRow0Height > 150 && (totalHeight - newRow0Height) > 150)
            {
                TilingHost.RowDefinitions[0].Height = new GridLength(newRow0Height, GridUnitType.Pixel);
                TilingHost.RowDefinitions[1].Height = new GridLength(totalHeight - newRow0Height, GridUnitType.Pixel);
            }
        }
    }

    private void UpdateTabTitle(TabViewModel? tab, WebView2 wv) => tab?.Title = wv.CoreWebView2.DocumentTitle;
    private void UpdateTabUrl(TabViewModel? tab, string url) => tab?.Url = url;

    // Header Button Handlers
    private void SwitchToHorizontal_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchTilingLayoutCommand.Execute(TilingLayout.Horizontal);
    private void SwitchToVertical_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchTilingLayoutCommand.Execute(TilingLayout.Vertical);
    private void SwitchToGrid_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchTilingLayoutCommand.Execute(TilingLayout.Grid);
    private void Untile_Click(object sender, RoutedEventArgs e) => ViewModel.UntileTabsCommand.Execute(null);
}
