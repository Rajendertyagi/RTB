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
        ViewModel.Tabs.CollectionChanged += (_, e) => RecalculateTabWidths();
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
        LoggingService.Info($"[Tabs] SelectionChanged. Selected: {ViewModel.SelectedTab?.Title ?? "null"}");

        // ✅ FIX: Explicitly sync IsActive state across all tabs
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

        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;
        if (TabListView.SelectedItems.Count > 1) return;

        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab)
            oldTab.Url = MainWebView.CoreWebView2.Source;

        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;

        if (MainWebView.CoreWebView2.Source != newTab.Url) 
            MainWebView.CoreWebView2.Navigate(newTab.Url);
            
        UpdateOmniboxIcon();
        BookmarkIcon.Glyph = _hbService.IsBookmarked(newTab.Url) ? "\uE735" : "\uE734";
    }

    private void Tab_ContextRequested(object sender, RightTappedRoutedEventArgs e)
    {
        LoggingService.Info("[Tabs] Right-click menu triggered.");
        
        var selectedTabs = TabListView.SelectedItems.Cast<TabViewModel>().ToList();
        var tabPresenter = sender as TabItemPresenter;
        if (tabPresenter?.DataContext is TabViewModel tabVM && !selectedTabs.Contains(tabVM))
            selectedTabs = new List<TabViewModel> { tabVM };

        var menu = new MenuFlyout
        {
            SystemBackdrop = new DesktopAcrylicBackdrop()
        };

        menu.Items.Add(new MenuFlyoutItem { Text = "Close tab" }.Apply(i => i.Click += (s, a) => ViewModel.CloseTabCommand.Execute(selectedTabs.LastOrDefault())));
        menu.Items.Add(new MenuFlyoutItem { Text = "Close other tabs" }.Apply(i => i.Click += (s, a) => { foreach(var t in ViewModel.Tabs.Where(t => !selectedTabs.Contains(t))) ViewModel.CloseTabCommand.Execute(t); }));
        
        if (selectedTabs.Count >= 2)
        {
            menu.Items.Add(new MenuFlyoutItem { Text = $"Tile {selectedTabs.Count} Tabs" }.Apply(i => i.Click += (s, a) => { ViewModel.TileSelection(selectedTabs, TilingLayout.Horizontal); TileTabs(selectedTabs[0], selectedTabs[1]); }));
        }

        FrameworkElement target = tabPresenter ?? RootGrid;
        if (e.TryGetPosition(target, out Point pos)) 
            menu.ShowAt(target, new FlyoutShowOptions { Position = pos });
        else 
            menu.ShowAt(target);
            
        e.Handled = true;
    }

    private void Tab_MiddleClicked(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab)
            ViewModel.CloseTabCommand.Execute(tab);
    }
    
    private void Tab_CloseClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab)
            ViewModel.CloseTabCommand.Execute(tab);
    }

    private void NewTab_Click(object sender, RoutedEventArgs e)
    {
        LoggingService.Info("[Tabs] NewTab clicked.");
        ViewModel.AddTabCommand.Execute(null);
    }
}

// Helper extension for cleaner inline event attachment
public static class MenuFlyoutItemExtensions
{
    public static T Apply<T>(this T item, Action<T> action) { action(item); return item; }
}
