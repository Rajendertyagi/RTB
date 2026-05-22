using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TB_Browser.Core.Logging;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls
{
    public partial class AddressBar : UserControl
    {
        private readonly IBrowserService _svc;

        public AddressBar(IBrowserService svc)
        {
            InitializeComponent();
            _svc = svc;
            _svc.UrlChanged += (_, u) => UrlBox.Text = u;
        }

        private void Back_Click(object s, RoutedEventArgs e) { App.Logger.Debug("AddressBar", "Back"); _svc.GoBack(); }
        private void Forward_Click(object s, RoutedEventArgs e) { App.Logger.Debug("AddressBar", "Forward"); _svc.GoForward(); }
        private void Reload_Click(object s, RoutedEventArgs e) { App.Logger.Debug("AddressBar", "Reload"); _svc.Reload(); }
        
        private void Go_Click(object s, RoutedEventArgs e)
        {
            var url = UrlBox.Text;
            App.Logger.Info("AddressBar", $"Submit: {url}");
            _svc.Navigate(url);
        }
        private void Url_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var url = UrlBox.Text;
                App.Logger.Info("AddressBar", $"Enter: {url}");
                _svc.Navigate(url);
            }
        }
    }
}
