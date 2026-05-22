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
            Logger.Debug("AddressBar", "Control initialized");
            
            _svc.UrlChanged += (_, u) => 
            {
                Logger.Debug("AddressBar", $"URL changed: {u}");
                UrlBox.Text = u;
            };
            _svc.CanGoBackChanged += (_, b) => 
            {
                BackBtn.IsEnabled = b;
                Logger.Debug("AddressBar", $"CanGoBack: {b}");
            };
            _svc.CanGoForwardChanged += (_, b) => 
            {
                FwdBtn.IsEnabled = b;
                Logger.Debug("AddressBar", $"CanGoForward: {b}");
            };
        }
        
        private void Back_Click(object s, RoutedEventArgs e)
        {
            Logger.Info("AddressBar", "Back button clicked");
            _svc.GoBack();
        }
        
        private void Forward_Click(object s, RoutedEventArgs e)
        {
            Logger.Info("AddressBar", "Forward button clicked");
            _svc.GoForward();
        }
        
        private void Reload_Click(object s, RoutedEventArgs e)
        {
            Logger.Info("AddressBar", "Reload button clicked");
            _svc.Reload();
        }
        
        private void Go_Click(object s, RoutedEventArgs e)
        {
            var url = UrlBox.Text;
            Logger.Info("AddressBar", $"Go button clicked: {url}");
            _svc.Navigate(url);
        }
        
        private void Url_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var url = UrlBox.Text;
                Logger.Info("AddressBar", $"Enter pressed in URL bar: {url}");
                _svc.Navigate(url);
            }
        }
        
        private void Url_TextChanged(object s, TextChangedEventArgs e)
        {
            if (!UrlBox.IsFocused) return;
            Logger.Debug("AddressBar", $"URL text changed: {UrlBox.Text}");
        }
        
        public void FocusUrl()
        {
            Logger.Debug("AddressBar", "Focusing URL box");
            UrlBox.Focus();
            UrlBox.SelectAll();
        }
    }
}
