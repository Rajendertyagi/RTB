using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TB_Browser.Core.Services;
using TB_Browser.Core.Models;

namespace TB_Browser.UI.Controls;

public partial class TabBar : UserControl
{
    private readonly TabService _svc;
    public TabBar(TabService svc)
    {
        InitializeComponent();
        _svc = svc;
        _svc.TabAdded += (_, t) => AddTabUI(t);
        _svc.TabRemoved += (_, t) => RemoveTabUI(t.Id);
    }

    private void AddTabUI(Tab t)
    {
        var btn = new Button { Content = t.Title, MinWidth = 120, MaxWidth = 200, Height = 40,
                               Background = (Brush)FindResource("SurfaceBrush"),
                               Foreground = (Brush)FindResource("TextBrush"),
                               BorderThickness = new Thickness(0,0,1,0), Padding = new Thickness(12,0,0,0),
                               Tag = t.Id, HorizontalContentAlignment = HorizontalAlignment.Left };
        btn.Click += (_, _) => Activate(t.Id);
        TabsPanel.Children.Add(btn);
        Activate(t.Id);
    }

    private void Activate(int id)
    {
        foreach (Button btn in TabsPanel.Children)
        {
            bool isActive = (int)btn.Tag == id;
            btn.Background = isActive ? (Brush)FindResource("BgBrush") : (Brush)FindResource("SurfaceBrush");
            btn.Foreground = isActive ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("TextBrush");
        }
        _svc.ActivateTab(id);
    }

    private void RemoveTabUI(int id)
    {
        foreach (Button btn in TabsPanel.Children)
            if ((int)btn.Tag == id) { TabsPanel.Children.Remove(btn); break; }
    }

    private void NewTab_Click(object s, RoutedEventArgs e) => _svc.CreateTab();
    private void Minimize_Click(object s, RoutedEventArgs e) => Window.GetWindow(this)?.WindowState = WindowState.Minimized;
    private void Maximize_Click(object s, RoutedEventArgs e) { var w = Window.GetWindow(this); w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
    private void Close_Click(object s, RoutedEventArgs e) => Application.Current.Shutdown();
}
