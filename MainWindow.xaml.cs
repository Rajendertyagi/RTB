using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace TB_Browser;

public sealed partial class MainWindow : Window
{
    private readonly Dictionary<string, string> _tabUrls = new();
    private string? _currentTabId;

    public MainWindow()
    {
        InitializeComponent();
        TabView.SelectionChanged += TabView_SelectionChanged;
        TabView_AddTabButtonClick(null, null);
    }

    private void TabView_AddTabButtonClick(TabView sender, object? args)
    {
        var id = Guid.NewGuid().ToString();
        var tab = new TabViewItem { Header = "New Tab", Tag = id };
        _tabUrls[id] = "https://www.google.com";
        sender.TabItems.Add(tab);
        sender.SelectedItem = tab;
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        var id = (string)args.Tab.Tag;
        _tabUrls.Remove(id);
        sender.TabItems.Remove(args.Tab);
        if (sender.TabItems.Count == 0) TabView_AddTabButtonClick(sender, null);
    }

    private void TabView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TabView.SelectedItem is TabViewItem tab)
        {
            _currentTabId = (string)tab.Tag;
            UrlBox.Text = _tabUrls[_currentTabId];
            WebView.Source = new Uri(_tabUrls[_currentTabId]);
        }
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e) => WebView.GoBack();
    private void FwdBtn_Click(object sender, RoutedEventArgs e) => WebView.GoForward();
    private void GoBtn_Click(object sender, RoutedEventArgs e) => Navigate();
    private void UrlBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) Navigate();
    }

    private void Navigate()
    {
        var url = UrlBox.Text.Trim();
        if (!url.StartsWith("http")) url = "https://" + url;
        if (_currentTabId != null) _tabUrls[_currentTabId] = url;
        WebView.Source = new Uri(url);
    }
}
