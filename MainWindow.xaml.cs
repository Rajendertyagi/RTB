using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TB_Browser.UI.Controls;

namespace TB_Browser;

public partial class MainWindow : Window
{
    public MainWindow(TabBar tabBar, AddressBar addressBar, BrowserView browserView)
    {
        InitializeComponent();
        TabBarHost.Content = tabBar;
        AddressBarHost.Content = addressBar;
        BrowserHost.Content = browserView;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
