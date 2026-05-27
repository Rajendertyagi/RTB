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
        ViewModel.Tabs.CollectionChanged += (_, e) =>
        {
            LoggingService.Info($"[Tabs] Collection changed. Action: {e.Action}. Count: {ViewModel.Tabs.Count}");
            RecalculateTabWidths();
        };
    }

    private void RecalculateTabWidths()
    {
        // ✅ FIX: Let tabs stay their natural 240px width and scroll horizontally
        foreach (var item in TabListView.Items)
        {
            if (TabListView.ContainerFromItem(item) is ListViewItem container)
            {
                container.Width = double.NaN; // Auto width based on child (240px)
                container.MinWidth = 0;
                container.MaxWidth = double.PositiveInfinity;
            }
        }
    }

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoggingService.Info($"[Tabs] SelectionChanged fired. WebViewInit: {_isWebViewInitialized}. Selected: {ViewModel.SelectedTab?.Title ?? "null"}");

        foreach (var item in TabListView.Items)
        {
            if (TabListView.ContainerFromItem(item) is ListViewItem container && container.Content is TabViewModel vm)
            {
                vm.IsActive = (vm == ViewModel.SelectedTab);
                if (container.ContentTemplateRoot is TabItemPresenter presenter)
                {
                    presenter.IsActive = vm.IsActive;
                }
            }
        }

        if (!_isWebViewInitialized || ViewModel.SelectedTab == null)
        {
            LoggingService.Warning("[Tabs] SelectionChanged ABORTED: WebView not initialized or no tab selected.");
            return;
        }
        if (TabListView.SelectedItems.Count > 1) return;

        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab)
        {
            LoggingService.Info($"[Tabs] Switched AWAY from: {oldTab.Title} ({oldTab.Url})");
            oldTab.Url = MainWebView.CoreWebView2.Source;
        }

        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;
        LoggingService.Info($"[Tabs] Switched TO: {newTab.Title} ({newTab.Url})");

        if (MainWebView.CoreWebView2.Source != newTab.Url) MainWebView.CoreWebView2.Navigate(newTab.Url);
        UpdateOmniboxIcon();

        bool isBookmarked = _hbService.IsBookmarked(newTab.Url);
        BookmarkIcon.Glyph = isBookmarked ? "\uE735" : "\uE734";
    }

    private void Tab_ContextRequested(object sender, RightTappedRoutedEventArgs e)
    {
        LoggingService.Info("[Tabs] Tab_ContextRequested fired (right-click on tab)");

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
            foreach (var t in ViewModel.Tabs.Where(t => !selectedTabs.Contains(t)))
                ViewModel.CloseTabCommand.Execute(t);
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

        Point point = e.GetPosition(targetElement);
        menu.ShowAt(targetElement, new FlyoutShowOptions { Position = point });

        e.Handled = true;
    }

    private void Tab_MiddleClicked(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab)
        {
            LoggingService.Info($"[Tabs] Middle-click close on: {tab.Title}");
            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    private void Tab_CloseClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab)
        {
            LoggingService.Info($"[Tabs] Close button clicked on: {tab.Title}");
            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    private void NewTab_Click(object sender, RoutedEventArgs e)
    {
        LoggingService.Info("[Tabs] NewTab button clicked. Executing AddTabCommand...");
        ViewModel.AddTabCommand.Execute(null);
    }
}
