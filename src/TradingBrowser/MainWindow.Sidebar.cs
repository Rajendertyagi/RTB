using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TradingBrowser.ViewModels;
using System.Collections.Generic;

namespace TradingBrowser;

public sealed partial class MainWindow
{
    private void RefreshSidebar()
    {
        var b = _hbService.GetBookmarks();
        var h = _hbService.GetHistory();
        
        var bookmarkList = new List<BookmarkItem>();
        foreach(var item in b) bookmarkList.Add(new BookmarkItem { Url = item.Url, Title = item.Title });
        BookmarkListView.ItemsSource = bookmarkList;

        var historyList = new List<HistoryItem>();
        foreach(var item in h) historyList.Add(new HistoryItem { Url = item.Url, Title = item.Title, VisitTime = item.Time });
        HistoryListView.ItemsSource = historyList;

        if (ViewModel.SelectedTab != null)
        {
            bool isBookmarked = _hbService.IsBookmarked(ViewModel.SelectedTab.Url);
            BookmarkIcon.Glyph = isBookmarked ? "\uE735" : "\uE734"; 
        }
    }

    private void ToggleBookmark(string url, string title)
    {
        if (string.IsNullOrEmpty(url)) return;
        bool isBookmarked = _hbService.IsBookmarked(url);
        
        if (isBookmarked) { _hbService.RemoveBookmark(url); BookmarkIcon.Glyph = "\uE734"; }
        else { _hbService.AddBookmark(url, title); BookmarkIcon.Glyph = "\uE735"; }
        
        if (MainSplitView.IsPaneOpen) RefreshSidebar();
    }

    private void BookmarkListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BookmarkListView.SelectedItem is BookmarkItem item)
        {
            ViewModel.NavigateToUrlCommand.Execute(item.Url);
            MainSplitView.IsPaneOpen = false; 
            BookmarkListView.SelectedItem = null;
        }
    }

    private void HistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryListView.SelectedItem is HistoryItem item)
        {
            ViewModel.NavigateToUrlCommand.Execute(item.Url);
            MainSplitView.IsPaneOpen = false;
            HistoryListView.SelectedItem = null;
        }
    }

    private void Bookmark_Click(object sender, RoutedEventArgs e) { if (ViewModel.SelectedTab != null) ToggleBookmark(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title); }

    private void Library_Click(object sender, RoutedEventArgs e)
    {
        RefreshSidebar(); 
        MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
    }
}
