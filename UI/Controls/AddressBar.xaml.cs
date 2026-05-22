using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls
{
    public partial class AddressBar : UserControl
    {
        private readonly IBrowserService _svc;
        public AddressBar(IBrowserService svc) { InitializeComponent(); _svc = svc; _svc.UrlChanged += (_, u) => UrlBox.Text = u; _svc.CanGoBackChanged += (_, b) => BackBtn.IsEnabled = b; _svc.CanGoForwardChanged += (_, b) => FwdBtn.IsEnabled = b; }
        private void Back_Click(object s, RoutedEventArgs e) => _svc.GoBack();
        private void Forward_Click(object s, RoutedEventArgs e) => _svc.GoForward();
        private void Reload_Click(object s, RoutedEventArgs e) => _svc.Reload();
        private void Go_Click(object s, RoutedEventArgs e) => _svc.Navigate(UrlBox.Text);
        private void Url_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Enter) _svc.Navigate(UrlBox.Text); }
        private void Url_TextChanged(object s, TextChangedEventArgs e) { if (!UrlBox.IsFocused) return; }
        public void FocusUrl() { UrlBox.Focus(); UrlBox.SelectAll(); }
    }
}
