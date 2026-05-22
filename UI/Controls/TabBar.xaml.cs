using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TB_Browser.Core.Logging;
using TB_Browser.Core.Services;
using TB_Browser.Core.Models;

namespace TB_Browser.UI.Controls
{
    public partial class TabBar : UserControl
    {
        private readonly ITabService _svc;
        private Button? _activeBtn;

        public TabBar(ITabService svc)
        {
            InitializeComponent();
            _svc = svc;
            _svc.TabAdded += (_, t) => AddTabUI(t);
            _svc.TabRemoved += (_, t) => RemoveTabUI(t.Id);
            Logger.Debug("TabBar", "Control initialized");
        }

        private void AddTabUI(TabModel t)
        {
            Logger.Debug("TabBar", $"Adding UI for tab #{t.Id}");
            var grid = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,4,0) };
            
            var btn = new Button 
            { 
                Content = t.Title, 
                Style = (Style)FindResource("TabBtn"), 
                MinWidth = 120, 
                MaxWidth = 200, 
                Tag = t.Id 
            };
            
            var close = new Button 
            { 
                Content = "✕", 
                Width = 20, 
                Height = 20, 
                Background = Brushes.Transparent, 
                BorderThickness = new Thickness(0), 
                Foreground = Brushes.Gray, 
                Margin = new Thickness(8,0,0,0),
                FontSize = 10
            };
            
            btn.Click += (_, _) => Activate(t.Id);
            close.Click += (_, _) => 
            {
                Logger.Info("TabBar", $"Close button clicked for tab #{t.Id}");
                _svc.CloseTab(t.Id);
            };
            
            grid.Children.Add(btn); 
            grid.Children.Add(close);
            TabsPanel.Children.Add(grid);
            Activate(t.Id);
        }
        
        private void Activate(int id)
        {
            Logger.Debug("TabBar", $"Activating tab #{id}");
            foreach (StackPanel sp in TabsPanel.Children)
            {
                var btn = (Button)sp.Children[0];
                bool isActive = (int)btn.Tag == id;
                btn.Background = isActive ? (Brush)FindResource("ActiveTabBg") : (Brush)FindResource("TabBtnBg");
                btn.Foreground = isActive ? Brushes.White : Brushes.Gray;
                if (isActive) _activeBtn = btn;
            }
            _svc.ActivateTab(id);
        }
        
        private void RemoveTabUI(int id)
        {
            Logger.Debug("TabBar", $"Removing UI for tab #{id}");
            foreach (StackPanel sp in TabsPanel.Children)
            {
                if ((int)((Button)sp.Children[0]).Tag == id)
                {
                    TabsPanel.Children.Remove(sp);
                    break;
                }
            }
        }
        
        // ✅ New Tab button click handler
        private void NewTab_Click(object s, RoutedEventArgs e)
        {
            Logger.Info("TabBar", "New Tab (+) button clicked");
            _svc.CreateTab();
        }
        
        public void CreateNewTab()
        {
            Logger.Info("TabBar", "CreateNewTab() called via keyboard shortcut");
            _svc.CreateTab();
        }
        
        public void CloseActive()
        {
            Logger.Info("TabBar", "CloseActive() called via keyboard shortcut");
            _svc.CloseTab(_svc.ActiveTab?.Id ?? 0);
        }
        
        private void Minimize_Click(object s, RoutedEventArgs e)
        {
            Logger.Info("TabBar", "Minimize clicked");
            Window.GetWindow(this).WindowState = WindowState.Minimized;
        }
        
        private void Maximize_Click(object s, RoutedEventArgs e)
        {
            var w = Window.GetWindow(this);
            Logger.Info("TabBar", $"Maximize clicked (current: {w.WindowState})");
            w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        
        // ✅ Fix close button
        private void Close_Click(object s, RoutedEventArgs e)
        {
            Logger.Info("TabBar", "Close (X) button clicked - shutting down");
            Application.Current.Shutdown();
        }
    }
}
