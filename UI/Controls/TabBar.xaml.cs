using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TB_Browser.Core.Models; // ✅ Ensure this using is present
using TB_Browser.Core.Services;

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

    private void AddTabUI(Tab t) // ✅ Changed 'TabModel' to 'Tab'
    {
        // Create Tab Button
        var btn = new Button { Content = t.Title, Style = (Style)FindResource("TabBtn"), Tag = t.Id };
        
        // Create Close Button (hidden by default, shown on hover via style)
        // Note: The style handles the visual close button logic via triggers, 
        // but if you want an explicit close button inside the template:
        
        // Add to panel
        TabsPanel.Children.Add(btn);
        Activate(t.Id);
    }

    private void Activate(int id)
    {
        foreach (Button btn in TabsPanel.Children)
        {
            bool isActive = (int)btn.Tag == id;
            btn.Background = isActive ? (Brush)FindResource("ActiveBrush") : Brushes.Transparent;
            btn.Foreground = isActive ? Brushes.White : (Brush)FindResource("TextDimBrush");
            
            if (isActive) _svc.ActivateTab(id);
        }
    }

    private void RemoveTabUI(int id)
    {
        foreach (Button btn in TabsPanel.Children)
            if ((int)btn.Tag == id) { TabsPanel.Children.Remove(btn); break; }
    }

    private void Minimize_Click(object s, RoutedEventArgs e) => Window.GetWindow(this)?.WindowState = WindowState.Minimized;
    private void Maximize_Click(object s, RoutedEventArgs e) { var w = Window.GetWindow(this); w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
    private void Close_Click(object s, RoutedEventArgs e) => Application.Current.Shutdown();
}
