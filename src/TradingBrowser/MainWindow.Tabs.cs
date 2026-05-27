using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TradingBrowser.ViewModels;
using TradingBrowser.Controls;
using TradingBrowser.Services;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;

namespace TradingBrowser;

public sealed partial class MainWindow
{
    public void SetupAdaptiveTabScaling()
    {
        TabListView.SizeChanged += (_, _) => RecalculateTabWidths();
        ViewModel.Tabs.CollectionChanged += (_, _) => RecalculateTabWidths();
    }

    private void RecalculateTabWidths()
    {
        if (TabListView.ActualWidth <= 0 || ViewModel.Tabs.Count == 0) return;

        double availableWidth = TabListView.ActualWidth - 44;
        int tabCount = ViewModel.Tabs.Count;
        double targetWidth = availableWidth / tabCount;
        double finalWidth = System.Math.Max(72, System.Math.Min(240, targetWidth));

        foreach (var item in TabListView.Items)
        {
            if (TabListView.ContainerFromItem(item) is ListViewItem container)
            {
                container.Width = finalWidth;
                container.MinWidth = 72;
                container.MaxWidth = 240;
            }
        }
    }

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoggingService.Info($"TabListView_SelectionChanged fired. WebViewInitialized: {_isWebViewInitialized}, SelectedTab: {ViewModel.SelectedTab?.Title ?? "null"}");

        foreach (var item in TabListView.Items)
        {
            if (TabListView.ContainerFromItem(item) is ListViewItem container && container.Content is TabViewModel vm)
            {
                if (container.ContentTemplateRoot is TabItemPresenter presenter)
                {
                    presenter.IsActive = (vm == ViewModel.SelectedTab);
                }
            }
        }

        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) 
        {
            LoggingService.Warning("TabListView_SelectionChanged ABORTED: WebView not initialized or no tab selected.");
            return; 
        }
        if (TabListView.SelectedItems.Count > 1) return;

        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab) oldTab.Url = MainWebView.CoreWebView2.Source;
        
        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;
        
        if (MainWebView.CoreWebView2.Source != newTab.Url) MainWebView.CoreWebView2.Navigate(newTab.Url);
        UpdateOmniboxIcon();
        
        bool isBookmarked = _hbService.IsBookmarked(newTab.Url);
        BookmarkIcon.Glyph = isBookmarked ? "\uE735" : "\uE734";
    }

    private void Tab_ContextRequested(object sender, ContextRequestedEventArgs e)
    {
        var selectedTabs = TabListView.SelectedItems.Cast<TabViewModel>().ToList();
        TabItemPresenter? tabPresenter = sender as TabItemPresenter;
        
        if (tabPresenter?.DataContext is TabViewModel tabVM)
        {
            if (!selectedTabs.Contains(tabVM)) selectedTabs = new List<TabViewModel> { tabVM };
        }

        var menu = new MenuFlyout();
        var closeItem = new MenuFlyoutItem { Text = "Close tab" };
        closeItem.Click += (s, args) => ViewModel.CloseTabCommand.Execute(selectedTabs.LastOrDefault());
        menu.Items.Add(closeItem);

        var closeOtherItem = new MenuFlyoutItem { Text = "Close other tabs" };
        closeOtherItem.Click += (s, args) => 
        {
            foreach (var t in ViewModel.Tabs.Where(t => !selectedTabs.Contains(t))) ViewModel.CloseTabCommand.Execute(t);
        };
        menu.Items.Add(closeOtherItem);

        if (selectedTabs.Count >= 2)
        {
            var tileItem = new MenuFlyoutItem { Text = $"Tile {selectedTabs.Count} Tabs" };
            tileItem.Click += (s, args) => 
            {
                ViewModel.TileSelection(selectedTabs, TilingLayout.Horizontal);
                TileTabs(selectedTabs[0], selectedTabs[1]);
            };
            menu.Items.Add(tileItem);
        }

        menu.SystemBackdrop = new DesktopAcrylicBackdrop();
        FrameworkElement targetElement = tabPresenter ?? (FrameworkElement)RootGrid;
        if (e.TryGetPosition(targetElement, out Point point)) menu.ShowAt(targetElement, new FlyoutShowOptions { Position = point });
        else menu.ShowAt(targetElement);
        e.Handled = true;
    }

    private void Tab_MiddleClicked(object sender, PointerRoutedEventArgs e) 
    { 
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab) ViewModel.CloseTabCommand.Execute(tab); 
    }
    
    private void Tab_CloseClicked(object sender, RoutedEventArgs e) 
    { 
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab) ViewModel.CloseTabCommand.Execute(tab); 
    }

    private void NewTab_Click(object sender, RoutedEventArgs e) 
    { 
        LoggingService.Info("NewTab_Click: Button pressed. Executing AddTabCommand...");
        ViewModel.AddTabCommand.Execute(null); 
    }
}
