using System;
using Microsoft.Web.WebView2.Core;
using TB_Browser.Core.Logging;

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
            _webView.NavigationStarting += (s, e) => Logger.Info("Web", $"Start: {e.Uri}");
            _webView.NavigationCompleted += (s, e) =>
            {
                if (e.IsSuccess) Logger.Info("Web", $"Done: {e.Uri}");
                else Logger.Warning("Web", $"Fail: {e.Uri} ({e.WebErrorStatus})");
            };
        }

        public void Navigate(string url)
        {
            if (!url.StartsWith("http")) url = "https://" + url;
            _webView?.Navigate(url);
            Logger.Info("Web", $"Nav: {url}");
        }
        public void GoBack() => _webView?.GoBack();
        public void GoForward() => _webView?.GoForward();
        public void Reload() => _webView?.Reload();
    }
}
