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
        private readonly ILogger _logger = App.Logger;

        public AddressBar(IBrowserService svc)
        {
            InitializeComponent();
            _svc = svc;
            _svc.UrlChanged += (_, u) => UrlBox.Text = u;
        }

        private void Back_Click(object s, RoutedEventArgs e)
        {
            _logger.Debug("AddressBar", "Back button clicked");
            _svc.GoBack();
        }
        private void Forward_Click(object s, RoutedEventArgs e)
        {
            _logger.Debug("AddressBar", "Forward button clicked");
            _svc.GoForward();
        }
        private void Reload_Click(object s, RoutedEventArgs e)
        {
            _logger.Debug("AddressBar", "Reload button clicked");
            _svc.Reload();
        }
        private void Go_Click(object s, RoutedEventArgs e)
        {
            var url = UrlBox.Text;
            _logger.Info("AddressBar", $"User submitted URL: {url}");
            _svc.Navigate(url);
        }
        private void Url_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var url = UrlBox.Text;
                _logger.Info("AddressBar", $"User pressed Enter for: {url}");
                _svc.Navigate(url);
            }
        }
    }
}
