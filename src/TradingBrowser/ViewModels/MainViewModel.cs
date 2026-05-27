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
    // ✅ AOT-Safe Properties
    [ObservableProperty] public partial TabViewModel? SelectedTab { get; set; }
    [ObservableProperty] public partial string OmniboxText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool CanGoBack { get; set; }
    [ObservableProperty] public partial bool CanGoForward { get; set; }
    [ObservableProperty] public partial TilingLayout CurrentTilingLayout { get; set; } = TilingLayout.None;

    public ObservableCollection<TabViewModel> TiledTabs { get; } = [];
    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    private readonly Stack<string> _closedTabs = new();

    public event Action<TilingLayout>? TilingLayoutChanged;
    public event Action<ICollection<TabViewModel>>? TilingTabsChanged;
    public event Action<string>? NavigationRequested;
    public event Action? FocusOmniboxRequested;
    public event Action? ToggleFullscreenRequested;
    public event Action? OpenDevToolsRequested;

    public MainViewModel() => Tabs.CollectionChanged += (s, e) => LoggingService.Info($"[VM] Tabs: {e.Action}. Count: {Tabs.Count}");

    public void InitializeSession(List<TabViewModel> restoredTabs, string? activeTabId)
    {
        Tabs.Clear();
        if (restoredTabs.Any())
        {
            foreach (var tab in restoredTabs) Tabs.Add(tab);
            SelectedTab = Tabs.FirstOrDefault(t => t.Id.ToString() == activeTabId) ?? Tabs.First();
        }
        else AddTab();
    }

    [RelayCommand] private void AddTab() { var t = new TabViewModel { Id = Guid.NewGuid(), Title = "New Tab", Url = "https://www.google.com" }; Tabs.Add(t); SelectedTab = t; }
    [RelayCommand] private void CloseTab(TabViewModel? tab) { if (tab == null) return; int i = Tabs.IndexOf(tab); _closedTabs.Push(tab.Url); Tabs.Remove(tab); if (Tabs.Count == 0) { AddTab(); return; } SelectedTab = Tabs[System.Math.Max(0, i - 1)]; }
    [RelayCommand] private void ReopenClosedTab() { if (_closedTabs.Any()) AddTab(); }
    [RelayCommand] private void DuplicateTab(TabViewModel? tab) { if (tab != null) { var t = new TabViewModel { Id = Guid.NewGuid(), Title = tab.Title + " (Copy)", Url = tab.Url }; Tabs.Add(t); SelectedTab = t; } }
    [RelayCommand] private void PinTab(TabViewModel? tab) { }
    [RelayCommand] private void CloseOtherTabs(TabViewModel? tab) { if (tab != null) { Tabs.Clear(); Tabs.Add(tab); SelectedTab = tab; } }
    [RelayCommand] private void CloseTabsToRight(TabViewModel? tab) { if (tab == null) return; int idx = Tabs.IndexOf(tab); for (int i = Tabs.Count - 1; i > idx; i--) Tabs.RemoveAt(i); }

    [RelayCommand] private void NavigateOmnibox()
    {
        string text = OmniboxText.Trim();
        if (string.IsNullOrEmpty(text)) return;
        string url = !text.StartsWith("http") && text.Contains('.') ? $"https://{text}" : !text.Contains('.') ? $"https://www.google.com/search?q={Uri.EscapeDataString(text)}" : text;
        NavigationRequested?.Invoke(url);
    }

    [RelayCommand] private void GoHome() { OmniboxText = "https://www.google.com"; NavigationRequested?.Invoke("https://www.google.com"); }
    [RelayCommand] private void NavigateToUrl(string url) { OmniboxText = url; NavigationRequested?.Invoke(url); }

    public void UpdateNavigationState(bool canGoBack, bool canGoForward) { CanGoBack = canGoBack; CanGoForward = canGoForward; }
    public void NextTab() { if (SelectedTab != null) SelectedTab = Tabs[(Tabs.IndexOf(SelectedTab) + 1) % Tabs.Count]; }
    public void PreviousTab() { if (SelectedTab != null) SelectedTab = Tabs[(Tabs.IndexOf(SelectedTab) - 1 + Tabs.Count) % Tabs.Count]; }
    public void SwitchToTab(int index) { if (index >= 0 && index < Tabs.Count) SelectedTab = Tabs[index]; }

    public void TriggerFocusOmnibox() => FocusOmniboxRequested?.Invoke();
    public void TriggerToggleFullscreen() => ToggleFullscreenRequested?.Invoke();
    public void TriggerOpenDevTools() => OpenDevToolsRequested?.Invoke();

    partial void OnSelectedTabChanging(TabViewModel? value) { if (value != null) OmniboxText = value.Url; }

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

    [RelayCommand] private void UntileTabs() { TiledTabs.Clear(); CurrentTilingLayout = TilingLayout.None; TilingTabsChanged?.Invoke(TiledTabs); TilingLayoutChanged?.Invoke(TilingLayout.None); }
    [RelayCommand] private void SwitchTilingLayout(TilingLayout layout) { if (TiledTabs.Count >= 2 && layout != CurrentTilingLayout) { CurrentTilingLayout = layout; TilingLayoutChanged?.Invoke(layout); } }
}
