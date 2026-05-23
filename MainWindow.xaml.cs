using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using TB.Services;
using TB.ViewModels;

namespace TB;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly NavigationViewModel _navigationVM;
    private readonly WebViewService _webViewService;
    private readonly TabStateManager _tabManager;

    public MainViewModel ViewModel => _viewModel;
    public NavigationViewModel NavigationVM => _navigationVM;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        _navigationVM = App.Services.GetRequiredService<NavigationViewModel>();
        _webViewService = App.Services.GetRequiredService<WebViewService>();
        _tabManager = App.Services.GetRequiredService<TabStateManager>();

        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(TitleBarGrid);
        
        // ✅ WinUI 3 requires AppWindow for sizing (XAML Width/Height are ignored)
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 600));

        _webViewService.InitializeAsync(WebView).ContinueWith(_ =>
        {
            _webViewService.SourceChanged += (s, url) => NavigationVM.AddressBarText = url;
            _webViewService.NavigationCompleted += (s, title) =>
            {
                if (_viewModel.SelectedTab != null) _viewModel.SelectedTab.Title = title ?? "New Tab";
            };
            _tabManager.CreateNewTab("https://www.google.com");
        });
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) 
    {
        var presenter = this.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
        presenter?.Minimize();
    }

    private void Maximize_Click(object sender, RoutedEventArgs e) 
    {
        var presenter = this.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
        if (presenter?.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
            presenter.Restore();
        else
            presenter?.Maximize();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab is TabViewModel tab) _tabManager.CloseTab(tab.Id);
    }

    private void Omnibox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrWhiteSpace(NavigationVM.AddressBarText))
        {
            var url = NavigationVM.AddressBarText.Contains(".") && !NavigationVM.AddressBarText.StartsWith("http")
                ? $"https://{NavigationVM.AddressBarText}"
                : $"https://www.google.com/search?q={Uri.EscapeDataString(NavigationVM.AddressBarText)}";
            _tabManager.NavigateToUrl(url);
        }
    }
}
