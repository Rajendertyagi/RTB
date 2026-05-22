using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls
{
    public sealed partial class AddressBar : UserControl
    {
        private readonly IBrowserService _svc;
        public AddressBar(IBrowserService svc) { InitializeComponent(); _svc = svc; _svc.UrlChanged += (_, u) => UrlBox.Text = u; }

        private void Back_Click(object s, RoutedEventArgs e) => _svc.GoBack();
        private void Forward_Click(object s, RoutedEventArgs e) => _svc.GoForward();
        private void Reload_Click(object s, RoutedEventArgs e) => _svc.Reload();
        private void Go_Click(object s, RoutedEventArgs e) => _svc.Navigate(UrlBox.Text);
        private void Url_KeyDown(object s, KeyRoutedEventArgs e) { if (e.Key == Windows.System.VirtualKey.Enter) _svc.Navigate(UrlBox.Text); }
    }
}
