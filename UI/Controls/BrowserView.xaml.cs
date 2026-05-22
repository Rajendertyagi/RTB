using System;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
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

                // 1. Create & attach WebView2
                var wv = new WebView2();
                _currentWebView = wv;
                WebViewHost.Content = wv;

                // 2. Await full initialization (blocks until CoreWebView2 is ready)
                Logger.Debug("BrowserView", "Initializing WebView2 runtime...");
                await wv.EnsureCoreWebView2Async();
                Logger.Debug("BrowserView", "WebView2 ready.");

                // 3. Wire events to service
                _svc.SetWebView(wv.CoreWebView2!);

                // 4. Navigate only after everything is bound
                Logger.Debug("BrowserView", $"Navigating to: {tab.Url}");
                wv.Source = new Uri(tab.Url);
            }
            catch (Exception ex)
            {
                Logger.Error("BrowserView", $"Navigation failed: {ex.Message}", ex);
            }
        }
    }
}
