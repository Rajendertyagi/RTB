using System.Windows;
using System.Windows.Controls;
using TB_Browser.Core.Services;

namespace TB_Browser.UI.Controls
{
    public partial class TabBar : UserControl
    {
        private readonly ITabService _svc;
        public TabBar(ITabService svc)
        {
            InitializeComponent();
            _svc = svc;
            _svc.TabAdded += (_, t) => AddTabUI(t);
            _svc.TabRemoved += (_, t) => RemoveTabUI(t.Id);
        }
        private void AddTabUI(TabModel t)
        {
            var btn = new Button { Content = t.Title, Style = (Style)FindResource("TabBtn") };
            btn.Click += (_, _) => _svc.ActivateTab(t.Id);
            btn.Tag = t.Id;
            TabsPanel.Children.Add(btn);
        }
        private void RemoveTabUI(int id)
        {
            foreach (Button b in TabsPanel.Children)
                if (b.Tag is int tid && tid == id) { TabsPanel.Children.Remove(b); break; }
        }
        private void Minimize_Click(object s, RoutedEventArgs e) => Window.GetWindow(this).WindowState = WindowState.Minimized;
        private void Maximize_Click(object s, RoutedEventArgs e)
        {
            var w = Window.GetWindow(this);
            w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        private void Close_Click(object s, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}
