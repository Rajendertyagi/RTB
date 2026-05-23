using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinUI;

namespace TB.Services;

public class WebViewService
{
    public CoreWebView2? CoreWebView2 { get; private set; }
    public event EventHandler<object>? SourceChanged;
    public event EventHandler<object>? NavigationCompleted;

    public async Task InitializeAsync(WebView2 webView)
    {
        await webView.EnsureCoreWebView2Async();
        CoreWebView2 = webView.CoreWebView2;
        CoreWebView2.SourceChanged += (s, e) => SourceChanged?.Invoke(this, EventArgs.Empty);
        CoreWebView2.NavigationCompleted += (s, e) => NavigationCompleted?.Invoke(this, EventArgs.Empty);
    }
}
