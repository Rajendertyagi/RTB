using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace TB_Browser.Core.Models
{
    public class TabModel
    {
        public int Id { get; init; }
        public string Title { get; set; } = "New Tab";
        public string Url { get; set; } = "about:blank";
        public CoreWebView2? WebView { get; set; }
        public ImageSource? Favicon { get; set; }
    }
}
