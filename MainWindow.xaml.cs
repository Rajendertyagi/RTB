using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;

namespace TB_Browser;

public sealed partial class MainWindow : Window
{
    private AppWindow? appWindow;
    private OverlappedPresenter? presenter;
    private readonly Dictionary<string, string> _tabUrls = new();

    public MainWindow()
    {
        InitializeComponent();

        appWindow = this.AppWindow;
        if (appWindow != null)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

            // ✅ Removed DraggableRects line to fix build error.
            // The solid background on Row 0 Grid handles dragging automatically.

            presenter = appWindow.Presenter as OverlappedPresenter;
        }

        AddNewTab("https://www.google.com", "New Tab");
    }

    private void AddNewTab(string url, string title)
    {
        var tab = new TabViewItem
        {
            Header = title,
            IconSource = new FontIconSource
            {
                Glyph = "\uE774",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets")
            }
        };

        var tabId = Guid.NewGuid().ToString();
        tab.Tag = tabId;
        _tabUrls[tabId] = url;

        TabView.TabItems.Add(tab);
        TabView.SelectedItem = tab;

        if (WebView.CoreWebView2 != null)
            WebView.Source = new Uri(url);
    }

    private void TabView_AddTabButtonClick(TabView sender, object args)
    {
        AddNewTab("https://www.google.com", "New Tab");
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        var tabId = args.Tab.Tag?.ToString();
        if (!string.IsNullOrEmpty(tabId))
            _tabUrls.Remove(tabId);

        sender.TabItems.Remove(args.Tab);

        if (sender.TabItems.Count == 0)
            AddNewTab("https://www.google.com", "New Tab");
    }

    private void Navigate(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        if (!url.StartsWith("http") && !url.StartsWith("file"))
            url = "https://" + url;

        try
        {
            WebView.Source = new Uri(url);
            UpdateCurrentTabUrl(url);
        }
        catch { }
    }

    private void UpdateCurrentTabUrl(string url)
    {
        if (TabView.SelectedItem is TabViewItem tab)
        {
            var tabId = tab.Tag?.ToString();
            if (!string.IsNullOrEmpty(tabId))
                _tabUrls[tabId] = url;
        }
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CanGoBack) WebView.GoBack();
    }

    private void FwdBtn_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CanGoForward) WebView.GoForward();
    }

    private void RefBtn_Click(object sender, RoutedEventArgs e)
    {
        WebView.Reload();
    }

    private void GoBtn_Click(object sender, RoutedEventArgs e)
    {
        Navigate(UrlBox.Text);
    }

    private void UrlBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            Navigate(UrlBox.Text);
        }
    }

    private void BookmarkBtn_Click(object sender, RoutedEventArgs e)
    {
        // Bookmark functionality placeholder
    }

    private void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (WebView.Source != null)
        {
            UrlBox.Text = WebView.Source.AbsoluteUri;
            UpdateCurrentTabUrl(WebView.Source.AbsoluteUri);

            if (TabView.SelectedItem is TabViewItem tab)
            {
                tab.Header = WebView.CoreWebView2?.DocumentTitle ?? "New Tab";
            }
        }
    }

    private void MinBtn_Click(object sender, RoutedEventArgs e) => presenter?.Minimize();

    private void MaxBtn_Click(object sender, RoutedEventArgs e)
    {
        if (presenter == null) return;
        if (presenter.State == OverlappedPresenterState.Maximized)
            presenter.Restore();
        else
            presenter.Maximize();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
}
