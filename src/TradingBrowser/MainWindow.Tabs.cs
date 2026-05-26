using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TradingBrowser.ViewModels;
using TradingBrowser.Controls;
using System.Linq;
using Windows.Foundation;

namespace TradingBrowser;

public sealed partial class MainWindow
{
    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;
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
        if (sender is TabItemPresenter tabPresenter && tabPresenter.DataContext is TabViewModel tabVM)
        {
            var menu = new MenuFlyout();
            
            var closeItem = new MenuFlyoutItem { Text = "Close tab" };
            closeItem.Click += (s, args) => ViewModel.CloseTabCommand.Execute(tabVM);
            menu.Items.Add(closeItem);

            var closeOtherItem = new MenuFlyoutItem { Text = "Close other tabs" };
            closeOtherItem.Click += (s, args) => 
            {
                var tabsToClose = ViewModel.Tabs.Where(t => t != tabVM).ToList();
                foreach (var t in tabsToClose) ViewModel.CloseTabCommand.Execute(t);
            };
            menu.Items.Add(closeOtherItem);

            var splitItem = new MenuFlyoutItem { Text = "Split Right" };
            splitItem.Click += (s, args) => SplitPane();
            menu.Items.Add(splitItem);

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
