using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinUI;

namespace TB.Services;

public class WebViewService
{
    public CoreWebView2? CoreWebView2 { get; private set; }
    public event Action<string>? SourceChanged;
    public event Action<string?>? NavigationCompleted;

    public async Task InitializeAsync(WebView2 webView)
    {
        await webView.EnsureCoreWebView2Async();
        CoreWebView2 = webView.CoreWebView2;
        CoreWebView2.SourceChanged += (s, e) => SourceChanged?.Invoke(CoreWebView2.Source);
        CoreWebView2.NavigationCompleted += (s, e) => NavigationCompleted?.Invoke(CoreWebView2.DocumentTitle);
    }
}
