using System;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Wpf;
using TB_Browser.Core.Models;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls
{
    public partial class BrowserView : UserControl
    {
        private readonly IBrowserService _svc;
        private WebView2? _currentWebView;
        private Action<bool>? _progressHandler;
        private Action<string>? _statusHandler;
        private double _zoom = 1.0;

        public BrowserView(IBrowserService svc) { InitializeComponent(); _svc = svc; MouseWheel += (_, e) => { if (Keyboard.Modifiers == ModifierKeys.Control) { _zoom += e.Delta > 0 ? 0.1 : -0.1; _svc.SetZoom(Math.Max(0.25, Math.Min(3.0, _zoom))); } }; }

        public void SetProgressHandler(Action<bool> handler) => _progressHandler = handler;
        public void SetStatusHandler(Action<string> handler) => _statusHandler = handler;

        public async void SwitchTo(TabModel tab)
        {
            try
            {
                var wv = new WebView2(); _currentWebView = wv; WebViewHost.Content = wv;
                await wv.EnsureCoreWebView2Async();
                _svc.SetWebView(wv.CoreWebView2!);
                _svc.IsLoadingChanged += (_, l) => _progressHandler?.Invoke(l);
                wv.CoreWebView2.StatusBarTextChanged += (_, e) => _statusHandler?.Invoke(wv.CoreWebView2.StatusBarText);
                wv.Source = new Uri(tab.Url);
            }
            catch (Exception ex) { Console.WriteLine($"Browser init failed: {ex.Message}"); }
        }
        public void Reload() => _currentWebView?.Reload();
        public void GoBack() => _currentWebView?.GoBack();
        public void GoForward() => _currentWebView?.GoForward();
    }
}
