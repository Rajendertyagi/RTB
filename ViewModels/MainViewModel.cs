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

    [ObservableProperty] private TabViewModel? _selectedTab;
    [ObservableProperty] public NavigationViewModel NavigationViewModel;
    [ObservableProperty] public SettingsViewModel SettingsViewModel;

    public ObservableCollection<TabViewModel> Tabs { get; } = new();

    public MainViewModel(NavigationViewModel navVm, SettingsViewModel settingsVm, TabService tabService)
    {
        NavigationViewModel = navVm;
        SettingsViewModel = settingsVm;
        _tabService = tabService;
        _newTabVm = new TabViewModel("https://www.google.com", "New Tab", _tabService);
    }

    public void InitializeTabs()
    {
        // ✅ FIX CS8625: Use string.Empty instead of null literal
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
