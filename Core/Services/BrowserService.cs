using System;
using Microsoft.Web.WebView2.Core;
using TB_Browser.Core.Logging;
using TB_Browser.Core.Models;

namespace TB_Browser.Core.Services
{
    public class BrowserService : IBrowserService
    {
        private readonly ILogger _logger;
        public ITabService TabService { get; set; } = null!;
        public event EventHandler<string>? UrlChanged;
        private CoreWebView2? _webView;

        public BrowserService(ILogger logger) => _logger = logger;

        public void SetWebView(CoreWebView2 webView)
        {
            _webView = webView;
            _webView.SourceChanged += (s, e) =>
            {
                if (TabService.ActiveTab != null)
                {
                    TabService.UpdateTab(TabService.ActiveTab.Id, _webView.Source, _webView.DocumentTitle);
                    UrlChanged?.Invoke(this, _webView.Source);
                    _logger.Debug("BrowserService", $"Navigated to: {_webView.Source}");
                }
            };
            _webView.NavigationStarting += (s, e) =>
            {
                _logger.Info("BrowserService", $"Navigation starting: {e.Uri}");
            };
            _webView.NavigationCompleted += (s, e) =>
            {
                if (e.IsSuccess)
                    _logger.Info("BrowserService", $"Navigation completed: {e.Source}");
                else
                    _logger.Warning("BrowserService", $"Navigation failed: {e.Source} (WebErrorStatus: {e.WebErrorStatus})");
            };
            _webView.WebResourceRequested += (s, e) =>
            {
                // Log blocked resources if needed
                _logger.Debug("BrowserService", $"Resource request: {e.Request.Uri}");
            };
        }

        public void Navigate(string url)
        {
            try
            {
                if (!url.StartsWith("http")) url = "https://" + url;
                _webView?.Navigate(url);
                _logger.Info("BrowserService", $"Navigate requested: {url}");
            }
            catch (Exception ex)
            {
                _logger.Error("BrowserService", $"Navigate failed: {url}", ex);
            }
        }

        public void GoBack()
        {
            try { _webView?.GoBack(); _logger.Debug("BrowserService", "GoBack"); }
            catch (Exception ex) { _logger.Warning("BrowserService", "GoBack failed", ex); }
        }

        public void GoForward()
        {
            try { _webView?.GoForward(); _logger.Debug("BrowserService", "GoForward"); }
            catch (Exception ex) { _logger.Warning("BrowserService", "GoForward failed", ex); }
        }

        public void Reload()
        {
            try { _webView?.Reload(); _logger.Debug("BrowserService", "Reload"); }
            catch (Exception ex) { _logger.Warning("BrowserService", "Reload failed", ex); }
        }
    }
}
