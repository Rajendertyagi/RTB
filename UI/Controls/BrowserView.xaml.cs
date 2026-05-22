using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using TB_Browser.Core.Models;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls
{
    public partial class BrowserView : UserControl
    {
        private readonly IBrowserService _svc;
        public BrowserView(IBrowserService svc)
        {
            InitializeComponent();
            _svc = svc;
        }

        public async void SwitchTo(TabModel tab)
        {
            var wv = new WebView2();
            WebViewHost.Content = wv;
            await wv.EnsureCoreWebView2Async();
            _svc.SetWebView(wv.CoreWebView2);
            wv.Source = new System.Uri(tab.Url);
        }
    }
}
