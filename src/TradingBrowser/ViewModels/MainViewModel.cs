using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TradingBrowser.Helpers;

namespace TradingBrowser.ViewModels;

public enum TilingLayout { None, Horizontal, Vertical, Grid }

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

    public event Action<TilingLayout>? TilingLayoutChanged;
    public event Action<ICollection<TabViewModel>>? TilingTabsChanged;
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
        var newTab = new TabViewModel 
        { 
            Id = Guid.NewGuid(), 
            Title = "New Tab", 
            Url = "https://www.google.com" 
        };
        Tabs.Add(newTab);
        SelectedTab = newTab;
    }

    [RelayCommand] 
    private void CloseTab(TabViewModel? tab) 
    { 
        if (tab == null) return;
        int index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        
        if (Tabs.Count == 0) { AddTab(); return; }
        if (index >= Tabs.Count) index = Tabs.Count - 1;
        SelectedTab = Tabs[index];
    }

    [RelayCommand] private void ReopenClosedTab() { if (_closedTabs.Any()) AddTab(); }
    
    [RelayCommand] 
    private void DuplicateTab(TabViewModel? tab) 
    { 
        if (tab != null) 
        { 
            var t = new TabViewModel { Id = Guid.NewGuid(), Title = tab.Title, Url = tab.Url }; 
            Tabs.Add(t); 
            SelectedTab = t; 
        } 
    }
    
    [RelayCommand] private void PinTab(TabViewModel? tab) { /* Reserved for future */ }
    
    [RelayCommand] 
    private void CloseOtherTabs(TabViewModel? tab) 
    { 
        if (tab == null) return; 
        Tabs.Clear(); 
        Tabs.Add(tab); 
        SelectedTab = tab; 
    }
    
    [RelayCommand] 
    private void CloseTabsToRight(TabViewModel? tab) 
    { 
        if (tab == null) return; 
        int idx = Tabs.IndexOf(tab); 
        for(int i = Tabs.Count - 1; i > idx; i--) Tabs.RemoveAt(i); 
    }
    
    [RelayCommand] 
    private void NavigateOmnibox() 
    { 
        string text = OmniboxText.Trim();
        if (string.IsNullOrEmpty(text)) return;
        
        string url = text;
        if (!text.StartsWith("http://") && !text.StartsWith("https://") && !text.Contains("."))
        {
            url = $"https://www.google.com/search?q={Uri.EscapeDataString(text)}";
        }
        else if (!text.StartsWith("http"))
        {
            url = "https://" + text;
        }
        
        NavigationRequested?.Invoke(url);
    }

    [RelayCommand] 
    private void GoHome() 
    { 
        OmniboxText = "https://www.google.com";
        NavigationRequested?.Invoke("https://www.google.com");
    }

    [RelayCommand] 
    private void NavigateToUrl(string url) 
    { 
        OmniboxText = url;
        NavigationRequested?.Invoke(url);
    }

    public void UpdateNavigationState(bool canGoBack, bool canGoForward)
    {
        CanGoBack = canGoBack;
        CanGoForward = canGoForward;
    }

    public void NextTab() { if (SelectedTab != null) { int i = Tabs.IndexOf(SelectedTab); SelectedTab = Tabs[(i + 1) % Tabs.Count]; } }
    public void PreviousTab() { if (SelectedTab != null) { int i = Tabs.IndexOf(SelectedTab); SelectedTab = Tabs[(i - 1 + Tabs.Count) % Tabs.Count]; } }
    public void SwitchToTab(int index) { if (index >= 0 && index < Tabs.Count) SelectedTab = Tabs[index]; }
    
    public void TriggerFocusOmnibox() => FocusOmniboxRequested?.Invoke();
    public void TriggerToggleFullscreen() => ToggleFullscreenRequested?.Invoke();
    public void TriggerOpenDevTools() => OpenDevToolsRequested?.Invoke();

    partial void OnSelectedTabChanging(TabViewModel? value) { if (value != null) OmniboxText = value.Url; }

    // ==========================================
    // TILING ENGINE
    // ==========================================
    public void TileSelection(IEnumerable<TabViewModel> selection, TilingLayout layout)
    {
        var tabs = selection.Take(2).ToList(); 
        if (tabs.Count < 2) return;

        TiledTabs.Clear();
        foreach (var t in tabs) TiledTabs.Add(t);
        
        CurrentTilingLayout = layout;
        TilingTabsChanged?.Invoke(TiledTabs);
        TilingLayoutChanged?.Invoke(layout);
    }

    [RelayCommand]
    private void UntileTabs()
    {
        TiledTabs.Clear();
        CurrentTilingLayout = TilingLayout.None;
        TilingTabsChanged?.Invoke(TiledTabs);
        TilingLayoutChanged?.Invoke(TilingLayout.None);
    }

    [RelayCommand]
    private void SwitchTilingLayout(TilingLayout layout)
    {
        if (TiledTabs.Count >= 2 && layout != CurrentTilingLayout)
        {
            CurrentTilingLayout = layout;
            TilingLayoutChanged?.Invoke(layout);
        }
    }
}
