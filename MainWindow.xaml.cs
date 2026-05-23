using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
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

    private void Minimize_Click(object sender, RoutedEventArgs e) => this.AppWindow.Changed += (s, e) => {}; // Handled by AppWindow
    private void Maximize_Click(object sender, RoutedEventArgs e) => this.AppWindow.Presenter = this.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter op && op.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized 
        ? this.AppWindow.Presenter = Microsoft.UI.Windowing.OverlappedPresenter.Create() 
        : ((Microsoft.UI.Windowing.OverlappedPresenter)this.AppWindow.Presenter).Maximize();
    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

    private void TabView_TabCloseRequested(winui.TabView sender, winui.TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab is ViewModels.TabViewModel tab) _tabManager.CloseTab(tab.Id);
    }

    private void TabView_TabItemsChanged(winui.TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
            _viewModel.SelectedTab = _viewModel.Tabs[^1];
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
