using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TradingBrowser.Helpers;

namespace TradingBrowser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private TabViewModel? _selectedTab;
    [ObservableProperty] private string _omniboxText = string.Empty;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoForward;

    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    private readonly Stack<string> _closedTabs = new();
    private string _searchEngine = "Google"; // Loaded from SettingsService in production

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
    }

    [RelayCommand]
    private void CloseTabsToRight(TabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null) return;
        
        int index = Tabs.IndexOf(tab);
        var toClose = Tabs.Skip(index + 1).ToList();
        foreach (var t in toClose) _closedTabs.Push(t.Url);
        
        foreach (var t in toClose) Tabs.Remove(t);
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
}
