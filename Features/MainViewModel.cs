using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TB.Features.Navigation;
using TB.Features.Tabs;

namespace TB.Features;

public partial class MainViewModel : ObservableObject
{
    private readonly TabService _tabService;

    public MainViewModel(TabService tabService, NavigationViewModel navigationViewModel)
    {
        _tabService = tabService;
        NavigationViewModel = navigationViewModel;
        Tabs = _tabService.Tabs;
    }

    public ObservableCollection<TabViewModel> Tabs { get; }
    public NavigationViewModel NavigationViewModel { get; }

    [ObservableProperty] private TabViewModel? _selectedTab;

    public void AddTab()
    {
        _tabService.Add();
        SelectedTab = _tabService.SelectedTab;
    }

    public void OnNavigationStarting() { /* Phase 2 */ }
    public void OnNavigationCompleted(string url, string title)
    {
        if (SelectedTab != null)
        {
            SelectedTab.Url = url;
            SelectedTab.Title = string.IsNullOrEmpty(title) ? url : title;
        }
    }

    public void OpenInNewTab(string uri) => _tabService.Add(uri);
}
