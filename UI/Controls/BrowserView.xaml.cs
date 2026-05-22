using System;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using TB_Browser.Core.Models;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls;

public partial class BrowserView : UserControl
{
    private readonly BrowserService _svc;
    private WebView2? _currentWebView;

    public BrowserView(BrowserService svc)
    {
        InitializeComponent();
        _svc = svc;
    }

    public async void SwitchTo(Tab tab)
    {
        try
        {
            var wv = new WebView2();
            _currentWebView = wv;
            WebViewHost.Children.Clear();
            WebViewHost.Children.Add(wv);
            await wv.EnsureCoreWebView2Async();
            _svc.SetWebView(wv.CoreWebView2);
            wv.Source = new Uri(tab.Url);
        }
        catch (Exception ex) { Console.WriteLine($"Browser init failed: {ex.Message}"); }
    }

    public void Reload() => _currentWebView?.Reload();
    public void GoBack() => _currentWebView?.GoBack();
    public void GoForward() => _currentWebView?.GoForward();
}
