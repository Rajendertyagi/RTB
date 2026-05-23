using TB.ViewModels;

namespace TB.Services;

public class TabStateManager
{
    private readonly MainViewModel _mainVM;
    private readonly WebViewService _webView;
    private readonly Dictionary<string, string> _tabUrls = new();

    public TabStateManager(MainViewModel mainVM, WebViewService webView)
    {
        _mainVM = mainVM;
        _webView = webView;
        _mainVM.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedTab) && _mainVM.SelectedTab != null)
                SwitchToTab(_mainVM.SelectedTab);
        };
    }

    public void CreateNewTab(string url)
    {
        var tab = new TabViewModel { Url = url, Title = "Loading..." };
        _mainVM.Tabs.Add(tab);
        _tabUrls[tab.Id] = url;
        _mainVM.SelectedTab = tab;
    }

    public void CloseTab(string tabId)
    {
        var tab = _mainVM.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            _mainVM.Tabs.Remove(tab);
            _tabUrls.Remove(tabId);
            if (_mainVM.Tabs.Count == 0) CreateNewTab("https://www.google.com");
            else _mainVM.SelectedTab = _mainVM.Tabs[^1];
        }
    }

    public void NavigateToUrl(string url)
    {
        if (_mainVM.SelectedTab != null)
        {
            _mainVM.SelectedTab.Url = url;
            _tabUrls[_mainVM.SelectedTab.Id] = url;
            _webView.CoreWebView2?.Navigate(url);
        }
    }

    private void SwitchToTab(TabViewModel tab)
    {
        if (_tabUrls.TryGetValue(tab.Id, out var url))
            _webView.CoreWebView2?.Navigate(url);
    }
}
