using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls;

public partial class AddressBar : UserControl
{
    private readonly BrowserService _svc;
    public AddressBar(BrowserService svc)
    {
        InitializeComponent();
        _svc = svc;
        _svc.UrlChanged += (_, u) => UrlBox.Text = u;
    }

    private void Back_Click(object s, RoutedEventArgs e) => _svc.GoBack();
    private void Forward_Click(object s, RoutedEventArgs e) => _svc.GoForward();
    private void Reload_Click(object s, RoutedEventArgs e) => _svc.Reload(); // ✅ Added
    private void Go_Click(object s, RoutedEventArgs e) => _svc.Navigate(UrlBox.Text);
    private void Url_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Enter) _svc.Navigate(UrlBox.Text); }
}
