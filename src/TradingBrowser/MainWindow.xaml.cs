using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using TradingBrowser.ViewModels;
using TradingBrowser.Controls;
using TradingBrowser.Services;
using System.Linq;
using System.Collections.Specialized;

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
        // Tabs are fixed at 240px in XAML ItemContainerStyle.
    }

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoggingService.Info($"[Tabs] SelectionChanged fired. Selected: {ViewModel.SelectedTab?.Title ?? "null"}");

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

        if (MainWebView.CoreWebView2.Source != newTab.Url) MainWebView.CoreWebView2.Navigate(newTab.Url);
        UpdateOmniboxIcon();

        bool isBookmarked = _hbService.IsBookmarked(newTab.Url);
        BookmarkIcon.Glyph = isBookmarked ? "\uE735" : "\uE734";
    }

    // ✅ FIX: Handles the "Close tab" click from the native XAML ContextFlyout
    private void Tab_CloseRequested(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab)
        {
            LoggingService.Info($"[Tabs] ContextFlyout: Close tab {tab.Title}");
            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    // ✅ FIX: Handles the "Close other tabs" click from the native XAML ContextFlyout
    private void Tab_CloseOthersRequested(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab)
        {
            LoggingService.Info($"[Tabs] ContextFlyout: Close other tabs (keeping {tab.Title})");
            foreach (var t in ViewModel.Tabs.Where(t => t != tab).ToList())
                ViewModel.CloseTabCommand.Execute(t);
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
        LoggingService.Info("[Tabs] NewTab button clicked.");
        ViewModel.AddTabCommand.Execute(null);
    }
}
