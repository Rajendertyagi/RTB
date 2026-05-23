using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB_Browser.Models;
using TB_Browser.Services;

namespace TB_Browser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly TabService _tabService;
    private readonly TabViewModel _newTabVm;
    
    // ✅ FIX MVVMTK0014: Use _lowerCamel for private fields, no [ObservableProperty] on DI deps
    private readonly NavigationViewModel _navigationViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    [ObservableProperty] private TabViewModel? _selectedTab;

    public ObservableCollection<TabViewModel> Tabs { get; } = new();
    
    // ✅ Expose as regular properties (no source generation needed)
    public NavigationViewModel NavigationViewModel => _navigationViewModel;
    public SettingsViewModel SettingsViewModel => _settingsViewModel;

    public MainViewModel(NavigationViewModel navigationViewModel, SettingsViewModel settingsViewModel, TabService tabService)
    {
        _navigationViewModel = navigationViewModel;
        _settingsViewModel = settingsViewModel;
        _tabService = tabService;
        _newTabVm = new TabViewModel("https://www.google.com", "New Tab", _tabService);
    }

    public void InitializeTabs()
    {
        AddTab(string.Empty, string.Empty);
    }

    public void AddTab(string url, string title)
    {
        var tab = new TabViewModel(url, title, _tabService);
        Tabs.Add(tab);
        SelectedTab = tab;
    }

    [RelayCommand]
    private void CloseTab(TabViewModel tab)
    {
        if (tab != null)
        {
            Tabs.Remove(tab);
            if (Tabs.Count == 0) AddTab("https://www.google.com", "New Tab");
            else SelectedTab = Tabs.Last();
        }
    }
}
