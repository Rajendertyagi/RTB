using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using System;

namespace TB_Browser;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
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
            
            try { WebView.Source = new Uri(url); }
            catch { }
        }
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e) => _ = WebView.CanGoBack ? WebView.GoBack() : 0;
    private void FwdBtn_Click(object sender, RoutedEventArgs e) => _ = WebView.CanGoForward ? WebView.GoForward() : 0;
    private void RefBtn_Click(object sender, RoutedEventArgs e) => WebView.Reload();
    private void HomeBtn_Click(object sender, RoutedEventArgs e) => Navigate("https://www.google.com");
    private void GoBtn_Click(object sender, RoutedEventArgs e) => Navigate(UrlBox.Text);
    
    private void UrlBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            Navigate(UrlBox.Text);
            UrlBox.SelectAll();
        }
    }

    // ✅ FIXED: Sender must be WebView2, not CoreWebView2
    private void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (WebView.Source != null)
            UrlBox.Text = WebView.Source.AbsoluteUri;
    }
}
