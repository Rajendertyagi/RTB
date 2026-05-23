using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB_Browser.Models;
using TB_Browser.Repositories;
using TB_Browser.Services;

namespace TB_Browser.ViewModels;

/// <summary>
/// Main orchestrator for the browser application.
/// Manages tabs, navigation, and global state.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly TabService _tabService;
    private readonly SettingsService _settingsService;
    private readonly HistoryService _historyService;
    private readonly BookmarkService _bookmarkService;
    private readonly FaviconService _faviconService;
    private readonly HistoryRepository _historyRepo;

    public MainViewModel(
        TabService tabService,
        SettingsService settingsService,
        HistoryService historyService,
        BookmarkService bookmarkService,
        FaviconService faviconService,
        HistoryRepository historyRepo)
    {
        _tabService = tabService;
        _settingsService = settingsService;
        _historyService = historyService;
        _bookmarkService = bookmarkService;
        _faviconService = faviconService;
        _historyRepo = historyRepo;

        _tabs = new ObservableCollection<TabViewModel>();
        _navigationViewModel = new NavigationViewModel(_historyRepo, null, _faviconService); // Pass bookmarkRepo if needed
        
        _tabService.SuspendRequested += OnSuspendRequested;
    }

    public NavigationViewModel NavigationViewModel => _navigationViewModel;

    [ObservableProperty]
    private ObservableCollection<TabViewModel> _tabs;

    [ObservableProperty]
    private TabViewModel? _selectedTab;

    private readonly NavigationViewModel _navigationViewModel;

    public void InitializeTabs()
    {
        // Apply startup config
        var mode = _settingsService.Settings.StartupMode;
        
        if (mode == "LastSession")
        {
            // TODO: Load session from DB/JSON and restore tabs
            AddTab("https://www.google.com", "New Tab"); // Fallback
        }
        else if (mode == "SpecificUrl")
        {
            AddTab(_settingsService.Settings.StartupUrl, "Home");
        }
        else
        {
            AddTab("https://www.google.com", "New Tab");
        }
    }

    public void AddTab(string url, string title)
    {
        var tab = new TabViewModel(url, RemoveTab);
        Tabs.Add(tab);
        SelectedTab = tab;
        
        // Load favicon async
        _ = LoadFaviconAsync(tab, url);
    }

    private void RemoveTab(TabViewModel tab)
    {
        Tabs.Remove(tab);
        if (Tabs.Count == 0)
            AddTab("https://www.google.com", "New Tab");
    }

    private async Task LoadFaviconAsync(TabViewModel tab, string url)
    {
        try
        {
            var uri = new Uri(url);
            var domain = uri.Host;
            tab.FaviconUrl = await _faviconService.GetFaviconUrlAsync(domain);
        }
        catch { /* Ignore invalid URLs */ }
    }

    private void OnSuspendRequested(TabViewModel tab)
    {
        if (!tab.IsSuspended)
        {
            tab.IsSuspended = true;
            // View will handle WebView2.TrySuspendAsync() based on this flag
        }
    }
}
