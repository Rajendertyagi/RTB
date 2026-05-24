using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace TradingBrowser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private TabViewModel? _selectedTab;
    [ObservableProperty] private string _omniboxText = string.Empty;

    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    private readonly Stack<string> _closedTabs = new();
    
    public event Action<string>? NavigationRequested;
    public event Action? FocusOmniboxRequested;
    public event Action? ToggleFullscreenRequested;
    public event Action? OpenDevToolsRequested;

    public MainViewModel() { }

    public void InitializeSession(List<TabViewModel> restoredTabs, string? activeTabId)
    {
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

    [RelayCommand]
    private void NavigateOmnibox()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(OmniboxText)) return;
        string input = OmniboxText.Trim();
        bool isUrl = Uri.TryCreate(input, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        if (!isUrl && input.Contains('.') && !input.Contains(' ')) { isUrl = true; input = $"https://{input}"; }
        
        string finalUrl = isUrl ? input : $"https://www.google.com/search?q={Uri.EscapeDataString(input)}";
        SelectedTab.Url = finalUrl;
        NavigationRequested?.Invoke(finalUrl);
    }

    [RelayCommand]
    private void GoHome()
    {
        if (SelectedTab != null) { SelectedTab.Url = "https://www.google.com"; NavigationRequested?.Invoke(SelectedTab.Url); }
    }

    public void TriggerFocusOmnibox() => FocusOmniboxRequested?.Invoke();
    public void TriggerToggleFullscreen() => ToggleFullscreenRequested?.Invoke();
    public void TriggerOpenDevTools() => OpenDevToolsRequested?.Invoke();

    partial void OnSelectedTabChanging(TabViewModel? value)
    {
        if (value != null) OmniboxText = value.Url;
    }
}
