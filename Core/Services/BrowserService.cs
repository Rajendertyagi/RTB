using System;
using Microsoft.Web.WebView2.Core;
using TB_Browser.Core.Models;

namespace TB_Browser.Core.Services
{
    public class BrowserService : IBrowserService
    {
        public ITabService TabService { get; set; } = null!;
        public event EventHandler<string>? UrlChanged;
        private CoreWebView2? _webView;

        public void SetWebView(CoreWebView2 webView)
        {
            _webView = webView;
            _webView.SourceChanged += (s, e) =>
            {
                if (TabService.ActiveTab != null)
                {
                    TabService.UpdateTab(TabService.ActiveTab.Id, _webView.Source, _webView.DocumentTitle);
                    UrlChanged?.Invoke(this, _webView.Source);
                }
            };
        }

        public void Navigate(string url)
        {
            if (!url.StartsWith("http")) url = "https://" + url;
            _webView?.Navigate(url);
        }
        public void GoBack() => _webView?.GoBack();
        public void GoForward() => _webView?.GoForward();
        public void Reload() => _webView?.Reload();
    }
}
