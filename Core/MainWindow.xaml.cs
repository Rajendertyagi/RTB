using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Wpf;
using TB.Features;
using TB.Features.Navigation;
using TB.Features.Tabs;

namespace TB.Core;

public partial class MainWindow
{
    public MainViewModel ViewModel { get; }

    // Win32 interop
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // Constants
    private const int WM_NCCALCSIZE = 0x83;
    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14,
                      HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
    
    // DWM constants
    private const int DWMWA_NCRENDERING_POLICY = 3;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_COLOR_NONE = -2; // 0xFFFFFFFE
    private const int NCRP_DISABLED = 0;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new TabService(), new NavigationViewModel());
        DataContext = ViewModel;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        // ✅ Official DWM method: Disable non-client rendering
        int policy = NCRP_DISABLED;
        DwmSetWindowAttribute(hwnd, DWMWA_NCRENDERING_POLICY, ref policy, sizeof(int));

        // ✅ Windows 11: Suppress border drawing
        int borderColor = DWMWA_COLOR_NONE;
        DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));

        // ✅ Fallback: Remove caption style (for older Windows)
        int style = GetWindowLong(hwnd, GWL_STYLE);
        SetWindowLong(hwnd, GWL_STYLE, style & ~WS_CAPTION);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // ✅ Official method: Handle WM_NCCALCSIZE to remove standard frame [[customframe]]
        if (msg == WM_NCCALCSIZE && wParam.ToInt32() == 1)
        {
            handled = true;
            return IntPtr.Zero; // Return 0 to use full window as client area
        }
        return IntPtr.Zero;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await BrowserView.EnsureCoreWebView2Async();
        if (BrowserView.CoreWebView2 != null && ViewModel.SelectedTab != null)
        {
            BrowserView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            BrowserView.CoreWebView2.Navigate(ViewModel.SelectedTab.Url);
            BrowserView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }
    }

    private void CoreWebView2_SourceChanged(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2SourceChangedEventArgs e)
    {
        if (BrowserView.CoreWebView2?.Source != null)
            Omnibox.Text = BrowserView.CoreWebView2.Source;
    }

    private void CoreWebView2_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        if (ViewModel.SelectedTab != null && BrowserView.CoreWebView2 != null)
        {
            ViewModel.SelectedTab.Title = string.IsNullOrEmpty(BrowserView.CoreWebView2.DocumentTitle)
                ? ViewModel.SelectedTab.Url
                : BrowserView.CoreWebView2.DocumentTitle;
        }
    }

    private async void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrowserView.CoreWebView2 == null || e.AddedItems.Count == 0) return;
        if (e.AddedItems[0] is TabViewModel tab)
        {
            await BrowserView.EnsureCoreWebView2Async();
            BrowserView.CoreWebView2.Navigate(tab.Url);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
    private void BtnMin_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMax_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void AddTab_Click(object sender, RoutedEventArgs e) => ViewModel.AddTab();
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn.DataContext is TabViewModel tab) { tab.Close(); return; }
            if (btn.TemplatedParent is ContentPresenter cp && cp.DataContext is TabViewModel tab2) { tab2.Close(); return; }
        }
        if (sender is TabItem ti && ti.DataContext is TabViewModel tab3) tab3.Close();
    }

    private void GoBack_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoBack();
    private void GoForward_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.GoForward();
    private void Reload_Click(object sender, RoutedEventArgs e) => BrowserView.CoreWebView2?.Reload();

    private void Omnibox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox tb) return;
        var query = tb.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        var url = query.Contains(".") && !query.StartsWith("http")
            ? $"https://{query}"
            : $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
        BrowserView.CoreWebView2?.Navigate(url);
        tb.Text = url;
    }

    private void ResizeBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && sender is Border)
        {
            var pos = e.GetPosition(this);
            var (w, h, t) = (ActualWidth, ActualHeight, 4.0);
            if (pos.X <= t)
            {
                if (pos.Y <= t) DragResize(HTTOPLEFT);
                else if (pos.Y >= h - t) DragResize(HTBOTTOMLEFT);
                else DragResize(HTLEFT);
            }
            else if (pos.X >= w - t)
            {
                if (pos.Y <= t) DragResize(HTTOPRIGHT);
                else if (pos.Y >= h - t) DragResize(HTBOTTOMRIGHT);
                else DragResize(HTRIGHT);
            }
            else if (pos.Y <= t) DragResize(HTTOP);
            else if (pos.Y >= h - t) DragResize(HTBOTTOM);
        }
    }
    private void DragResize(int direction) =>
        SendMessage(new WindowInteropHelper(this).Handle, WM_NCLBUTTONDOWN, direction, 0);

    private void MainWindow_Closed(object? sender, EventArgs e) => BrowserView?.Dispose();
    private void OpenSettings_Click(object sender, RoutedEventArgs e) { /* Phase 3 */ }
}
