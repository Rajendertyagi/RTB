using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using Microsoft.Web.WebView2.Core;
using System;

namespace TB_Browser;

public sealed partial class MainWindow : Window
{
    private AppWindow? appWindow;
    private OverlappedPresenter? presenter;

    public MainWindow()
    {
        InitializeComponent();
        
        // ✅ Programmatic Title Bar Setup (Bypasses XAML compiler crash)
        appWindow = this.AppWindow;
        if (appWindow != null)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            presenter = appWindow.Presenter as OverlappedPresenter;
        }

        TabView.TabItems.Add(new TabViewItem { Header = "New Tab" });
        TabView.SelectedIndex = 0;
        Navigate("https://www.google.com");
    }

    private void TabView_AddTabButtonClick(TabView sender, object args)
    {
        var tab = new TabViewItem { Header = "New Tab" };
        sender.TabItems.Add(tab);
        sender.SelectedItem = tab;
        Navigate("https://www.google.com");
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        sender.TabItems.Remove(args.Tab);
        if (sender.TabItems.Count == 0)
        {
            var tab = new TabViewItem { Header = "New Tab" };
            sender.TabItems.Add(tab);
            sender.SelectedItem = tab;
            Navigate("https://www.google.com");
        }
    }

    private void Navigate(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            if (!url.StartsWith("http") && !url.StartsWith("file"))
                url = "https://" + url;
            try { WebView.Source = new Uri(url); } catch { }
        }
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e) { if (WebView.CanGoBack) WebView.GoBack(); }
    private void FwdBtn_Click(object sender, RoutedEventArgs e) { if (WebView.CanGoForward) WebView.GoForward(); }
    private void RefBtn_Click(object sender, RoutedEventArgs e) { WebView.Reload(); }
    private void HomeBtn_Click(object sender, RoutedEventArgs e) { Navigate("https://www.google.com"); }
    private void GoBtn_Click(object sender, RoutedEventArgs e) { Navigate(UrlBox.Text); }
    
    private void UrlBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) { Navigate(UrlBox.Text); UrlBox.SelectAll(); }
    }

    private void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (WebView.Source != null) UrlBox.Text = WebView.Source.AbsoluteUri;
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
