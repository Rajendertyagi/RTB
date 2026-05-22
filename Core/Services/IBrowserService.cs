using System;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace TB_Browser.Core.Services
{
    public interface IBrowserService
    {
        ITabService TabService { get; set; }
        void SetWebView(CoreWebView2 webView);
        void Navigate(string url);
        void GoBack();
        void GoForward();
        void Reload();
        void SetZoom(double factor);
        event EventHandler<string> UrlChanged;
        event EventHandler<bool> IsLoadingChanged;
        event EventHandler<bool> CanGoBackChanged;
        event EventHandler<bool> CanGoForwardChanged;
        event EventHandler<ImageSource?> FaviconChanged;
    }
}
