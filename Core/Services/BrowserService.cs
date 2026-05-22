using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using TB_Browser.Core.Logging;

namespace TB_Browser.Core.Services
{
    public class BrowserService : IBrowserService
    {
        public ITabService TabService { get; set; } = null!;
        public event EventHandler<string>? UrlChanged;
        public event EventHandler<bool>? IsLoadingChanged;
        public event EventHandler<bool>? CanGoBackChanged;
        public event EventHandler<bool>? CanGoForwardChanged;
        public event EventHandler<ImageSource?>? FaviconChanged;
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
            _webView.HistoryChanged += (s, e) =>
            {
                CanGoBackChanged?.Invoke(this, _webView.CanGoBack);
                CanGoForwardChanged?.Invoke(this, _webView.CanGoForward);
            };
            _webView.NavigationStarting += (s, e) =>
            {
                IsLoadingChanged?.Invoke(this, true);
                Logger.Info("Web", $"Start: {e.Uri}");
            };
            _webView.NavigationCompleted += (s, e) =>
            {
                IsLoadingChanged?.Invoke(this, false);
                Logger.Info("Web", e.IsSuccess ? $"Done: {_webView.Source}" : $"Fail: {_webView.Source}");
            };
            _webView.FaviconChanged += async (s, e) =>
            {
                try
                {
                    using var stream = await _webView.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
                    if (stream != null)
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        FaviconChanged?.Invoke(this, bitmap);
                    }
                }
                catch { }
            };
        }

        public void Navigate(string url) { if (!url.StartsWith("http")) url = "https://" + url; _webView?.Navigate(url); }
        public void GoBack() => _webView?.GoBack();
        public void GoForward() => _webView?.GoForward();
        public void Reload() => _webView?.Reload();
    }
}
