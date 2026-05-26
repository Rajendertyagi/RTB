using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TradingBrowser.Helpers;

namespace TradingBrowser.ViewModels;

// NEW: Enum to track the current tiling layout
public enum TilingLayout
{
    None,
    Horizontal,
    Vertical,
    Grid
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private TabViewModel? _selectedTab;
    [ObservableProperty] private string _omniboxText = string.Empty;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoForward;

    // ==========================================
    // TILING STATE
    // ==========================================
    [ObservableProperty] private TilingLayout _currentTilingLayout = TilingLayout.None;
    public ObservableCollection<TabViewModel> TiledTabs { get; } = [];
    // ==========================================

    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    private readonly Stack<string> _closedTabs = new();
    private string _searchEngine = "Google"; 

    public event Action<string>? NavigationRequested;
    public event Action? FocusOmniboxRequested;
    public event Action? ToggleFullscreenRequested;
    public event Action? OpenDevToolsRequested;

    public MainViewModel() { }

    public void InitializeSession(List<TabViewModel> restoredTabs, string? activeTabId)
    {
        Tabs.Clear();
        if (restoredTabs.Any())
        {
            foreach (var tab in restoredTabs) Tabs.Add(tab);
            SelectedTab = Tabs.FirstOrDefault(t => t.Id.ToString() == activeTabId) ?? Tabs.First();
        }
        else
        {
            AddTab();
        }
    }

    [RelayCommand]
    private void AddTab()
    {
        var newTab = new TabViewModel { Url = "https://www.google.com" };
        Tabs.Add(newTab);
        SelectedTab = newTab;
        NavigationRequested?.Invoke(newTab.Url);
    }

    [RelayCommand]
    private void CloseTab(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;

        _closedTabs.Push(tab.Url);
        int index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        // TILING FIX: Remove from tiled tabs if closed
        if (TiledTabs.Contains(tab)) TiledTabs.Remove(tab);
        if (TiledTabs.Count < 2) UntileTabs();

        if (Tabs.Count == 0) AddTab();
        else SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
    }

    [RelayCommand]
    private void ReopenClosedTab()
    {
        if (_closedTabs.TryPop(out string? url))
        {
            var newTab = new TabViewModel { Url = url };
            Tabs.Add(newTab);
            SelectedTab = newTab;
            NavigationRequested?.Invoke(url);
        }
    }

    [RelayCommand]
    private void DuplicateTab(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;
        
        var clone = new TabViewModel { Url = tab.Url, Title = tab.Title };
        Tabs.Insert(Tabs.IndexOf(tab) + 1, clone);
        SelectedTab = clone;
        NavigationRequested?.Invoke(clone.Url);
    }

    [RelayCommand]
    private void PinTab(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;
        tab.IsPinned = !tab.IsPinned;
    }

    [RelayCommand]
    private void CloseOtherTabs(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;
        
        var toKeep = new[] { tab };
        var toClose = Tabs.Except(toKeep).ToList();
        foreach (var t in toClose) _closedTabs.Push(t.Url);
        
        Tabs.Clear();
        Tabs.Add(tab);
        SelectedTab = tab;

        // TILING FIX: Clear tiling if other tabs are closed
        UntileTabs();
    }

    [RelayCommand]
    private void CloseTabsToRight(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;
        
        int index = Tabs.IndexOf(tab);
        var toClose = Tabs.Skip(index + 1).ToList();
        foreach (var t in toClose) _closedTabs.Push(t.Url);
        
        foreach (var t in toClose) 
        {
            Tabs.Remove(t);
            if (TiledTabs.Contains(t)) TiledTabs.Remove(t);
        }

        if (TiledTabs.Count < 2) UntileTabs();
    }

    [RelayCommand]
    private void NavigateOmnibox()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(OmniboxText)) return;
        
        string finalUrl = UriHelper.ResolveUrl(OmniboxText, _searchEngine);
        SelectedTab.Url = finalUrl;
        NavigationRequested?.Invoke(finalUrl);
    }

    [RelayCommand]
    private void GoHome()
    {
        if (SelectedTab != null) 
        { 
            SelectedTab.Url = "https://www.google.com"; 
            NavigationRequested?.Invoke(SelectedTab.Url); 
        }
    }

    [RelayCommand]
    private void NavigateToUrl(string url)
    {
        if (SelectedTab != null)
        {
            SelectedTab.Url = url;
            NavigationRequested?.Invoke(url);
        }
    }

    public void UpdateNavigationState(bool canGoBack, bool canGoForward)
    {
        CanGoBack = canGoBack;
        CanGoForward = canGoForward;
    }

    public void NextTab()
    {
        if (SelectedTab == null || Tabs.Count <= 1) return;
        int index = Tabs.IndexOf(SelectedTab);
        SelectedTab = Tabs[(index + 1) % Tabs.Count];
    }

    public void PreviousTab()
    {
        if (SelectedTab == null || Tabs.Count <= 1) return;
        int index = Tabs.IndexOf(SelectedTab);
        SelectedTab = Tabs[(index - 1 + Tabs.Count) % Tabs.Count];
    }

    public void SwitchToTab(int index)
    {
        if (index >= 0 && index < Tabs.Count) SelectedTab = Tabs[index];
    }

    public void TriggerFocusOmnibox() => FocusOmniboxRequested?.Invoke();
    public void TriggerToggleFullscreen() => ToggleFullscreenRequested?.Invoke();
    public void TriggerOpenDevTools() => OpenDevToolsRequested?.Invoke();

    partial void OnSelectedTabChanging(TabViewModel? value)
    {
        if (value != null) OmniboxText = value.Url;
    }

    // ==========================================
    // TILING COMMANDS
    // ==========================================
    
    /// <summary>
    /// Called from the UI when the user selects multiple tabs and requests a tile.
    /// </summary>
    public void TileSelectedTabs(IEnumerable<TabViewModel>? selectedTabs)
    {
        if (selectedTabs == null || selectedTabs.Count() < 2) return;
        
        TiledTabs.Clear();
        foreach (var tab in selectedTabs)
        {
            if (!TiledTabs.Contains(tab)) TiledTabs.Add(tab);
        }
        
        CurrentTilingLayout = TilingLayout.Horizontal; // Default layout
    }

    [RelayCommand]
    private void UntileTabs()
    {
        TiledTabs.Clear();
        CurrentTilingLayout = TilingLayout.None;
    }

    [RelayCommand]
    private void SetTilingLayout(TilingLayout layout)
    {
        if (TiledTabs.Count >= 2)
        {
            CurrentTilingLayout = layout;
        }
    }
}
