using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TB.Features.Navigation;
using TB.Features.Tabs;

namespace TB.Features;
public partial class MainViewModel : ObservableObject
{
    private readonly TabService _tabService;
    public ObservableCollection<TabViewModel> Tabs => _tabService.Tabs;
    [ObservableProperty] private TabViewModel? _selectedTab;
    [ObservableProperty] private NavigationViewModel _navigationViewModel;

    public MainViewModel(TabService tabService, NavigationViewModel navigationViewModel)
    {
        _tabService = tabService;
        _navigationViewModel = navigationViewModel;
        _tabService.Tabs.CollectionChanged += (s, e) => OnPropertyChanged(nameof(Tabs));
        AddTab();
    }

    public void AddTab()
    {
        _tabService.Add();
        SelectedTab = _tabService.SelectedTab;
    }
}
