using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TB.Features;
using TB.Features.Navigation;
using TB.Features.Tabs;

namespace TB;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new TabService(), new NavigationViewModel());
        DataContext = ViewModel;
        Opened += MainWindow_Opened;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        // ✅ Phase 1: No WebView2 init. Update status text.
        StatusText.Text = "UI Shell Ready — WebView2 in Phase 2";
    }

    private void TabStrip_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        if (e.AddedItems[0] is TabViewModel tab)
        {
            // ✅ Phase 1: Just update status, no navigation
            StatusText.Text = $"Tab: {tab.Title}";
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void BtnMin_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMax_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void BtnClose_Click(object? sender, RoutedEventArgs e) => Close();

    private void AddTab_Click(object? sender, RoutedEventArgs e) => ViewModel.AddTab();
    private void TabClose_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control btn && btn.DataContext is TabViewModel tab)
            tab.Close();
    }

    // ✅ Phase 1 stubs: No WebView2, just UI feedback
    private void GoBack_Click(object? sender, RoutedEventArgs e) => StatusText.Text = "Back (Phase 2)";
    private void GoForward_Click(object? sender, RoutedEventArgs e) => StatusText.Text = "Forward (Phase 2)";
    private void Reload_Click(object? sender, RoutedEventArgs e) => StatusText.Text = "Reload (Phase 2)";

    private void Omnibox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox tb) return;
        var query = tb.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        var url = query.Contains(".") && !query.StartsWith("http")
            ? $"https://{query}"
            : $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
        tb.Text = url;
        StatusText.Text = $"Navigate: {url} (Phase 2)";
    }

    private void OpenSettings_Click(object? sender, RoutedEventArgs e) { /* Phase 3 */ }
}
