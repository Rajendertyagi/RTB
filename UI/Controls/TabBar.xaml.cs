using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TB_Browser.Core.Services;
using TB_Browser.Core.Models;

namespace TB_Browser.UI.Controls;

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
        var grid = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,4,0) };
        var btn = new Button { Content = t.Title, Style = (Style)FindResource("FluentTab"), Tag = t.Id, Cursor = Cursors.Hand };
        var close = new Button { Content = "✕", Width = 14, Height = 14, Background = Brushes.Transparent,
                                 BorderThickness = new Thickness(0), Foreground = (Brush)FindResource("FluentTextDisabled"),
                                 Margin = new Thickness(6,0,0,0), FontSize = 9, Cursor = Cursors.Hand };
        btn.Click += (_, _) => Activate(t.Id);
        close.Click += (_, _) => _svc.CloseTab(t.Id);
        grid.Children.Add(btn); grid.Children.Add(close);
        TabsPanel.Children.Add(grid);
        Activate(t.Id);
    }

    private void Activate(int id)
    {
        foreach (StackPanel sp in TabsPanel.Children)
        {
            var btn = (Button)sp.Children[0];
            bool isActive = (int)btn.Tag == id;
            btn.Background = isActive ? (Brush)FindResource("FluentSurfaceBright") : (Brush)FindResource("FluentSurfaceBrush");
            btn.Foreground = isActive ? (Brush)FindResource("FluentTextBrush") : (Brush)FindResource("FluentTextDisabled");
        }
        _svc.ActivateTab(id);
    }

    private void RemoveTabUI(int id)
    {
        foreach (StackPanel sp in TabsPanel.Children)
            if ((int)((Button)sp.Children[0]).Tag == id) { TabsPanel.Children.Remove(sp); break; }
    }

    private void NewTab_Click(object s, RoutedEventArgs e) => _svc.CreateTab();
    public void CreateNewTab() => _svc.CreateTab();
    public void CloseActive() => _svc.CloseTab(_svc.ActiveTab?.Id ?? 0);
    private void Minimize_Click(object s, RoutedEventArgs e) { var w = Window.GetWindow(this); if (w != null) w.WindowState = WindowState.Minimized; }
    private void Maximize_Click(object s, RoutedEventArgs e) { var w = Window.GetWindow(this); if (w != null) w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
    private void Close_Click(object s, RoutedEventArgs e) => Application.Current.Shutdown();
}
