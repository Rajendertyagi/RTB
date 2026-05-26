using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TradingBrowser.ViewModels;
using TradingBrowser.Controls;
using System.Linq;
using System.Collections.Generic;
using Windows.Foundation;

namespace TradingBrowser;

public sealed partial class MainWindow
{
    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // EDGE UI: Sync Active State for Tab Pills (Triggers the top highlight line)
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

        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;
        
        // Prevent MainWebView from reloading if we are just selecting a second tab for tiling
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
        // Get all currently selected tabs (for Vivaldi-style tiling)
        var selectedTabs = TabListView.SelectedItems.Cast<TabViewModel>().ToList();
        
        if (sender is TabItemPresenter tabPresenter && tabPresenter.DataContext is TabViewModel tabVM)
        {
            // If the right-clicked tab isn't in the selection, just use it
            if (!selectedTabs.Contains(tabVM))
            {
                selectedTabs = new List<TabViewModel> { tabVM };
            }
        }

        var menu = new MenuFlyout();
        
        var closeItem = new MenuFlyoutItem { Text = "Close tab" };
        closeItem.Click += (s, args) => ViewModel.CloseTabCommand.Execute(selectedTabs.LastOrDefault());
        menu.Items.Add(closeItem);

        var closeOtherItem = new MenuFlyoutItem { Text = "Close other tabs" };
        closeOtherItem.Click += (s, args) => 
        {
            var tabsToClose = ViewModel.Tabs.Where(t => !selectedTabs.Contains(t)).ToList();
            foreach (var t in tabsToClose) ViewModel.CloseTabCommand.Execute(t);
        };
        menu.Items.Add(closeOtherItem);

        // TILING LOGIC: Show Tile option if 2+ tabs are selected
        if (selectedTabs.Count >= 2)
        {
            var tileItem = new MenuFlyoutItem { Text = $"Tile {selectedTabs.Count} Tabs" };
            tileItem.Click += (s, args) => 
            {
                ViewModel.TileSelectedTabs(selectedTabs);
                TileTabs(selectedTabs[0], selectedTabs[1]);
            };
            menu.Items.Add(tileItem);
        }
        else
        {
            var splitItem = new MenuFlyoutItem { Text = "Split Right" };
            splitItem.Click += (s, args) => SplitPane();
            menu.Items.Add(splitItem);
        }

        menu.SystemBackdrop = new DesktopAcrylicBackdrop();

        if (e.TryGetPosition(tabPresenter, out Point point))
        {
            menu.ShowAt(tabPresenter, new FlyoutShowOptions { Position = point });
        }
        else
        {
            menu.ShowAt(tabPresenter);
        }
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

    private void NewTab_Click(object sender, RoutedEventArgs e) { ViewModel.AddTabCommand.Execute(null); }
}
