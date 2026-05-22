using System;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using TB_Browser.Core.Logging;
using TB_Browser.Core.Models;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls
{
    public partial class BrowserView : UserControl
    {
        private readonly IBrowserService _svc;
        private WebView2? _currentWebView;

        public BrowserView(IBrowserService svc)
        {
            InitializeComponent();
            _svc = svc;
        }

        public async void SwitchTo(TabModel tab)
        {
            try
            {
                Logger.Info("BrowserView", $"Switching to tab #{tab.Id}: {tab.Url}");

                var wv = new WebView2();
                _currentWebView = wv;
                WebViewHost.Content = wv;

                await wv.EnsureCoreWebView2Async();
                Logger.Debug("BrowserView", "WebView2 ready.");

                _svc.SetWebView(wv.CoreWebView2!);

                Logger.Debug("BrowserView", $"Navigating to: {tab.Url}");
                wv.Source = new Uri(tab.Url);
            }
            catch (Exception ex)
            {
                // ✅ Fixed: Logger.Error takes 2 arguments
                Logger.Error("BrowserView", $"Navigation failed: {ex.Message}");
            }
        }
    }
}
