using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TradingBrowser.ViewModels;

/// <summary>
/// Main ViewModel for the browser window. Manages tabs, navigation state, and commands.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// The currently selected tab.
    /// </summary>
    [ObservableProperty] 
    private TabViewModel? _selectedTab;

    /// <summary>
    /// The text displayed in the omnibox.
    /// </summary>
    [ObservableProperty] 
    private string _omniboxText = string.Empty;

    /// <summary>
    /// Indicates whether the back button should be enabled.
    /// </summary>
    [ObservableProperty] 
    private bool _canGoBack;

    /// <summary>
    /// Indicates whether the forward button should be enabled.
    /// </summary>
    [ObservableProperty] 
    private bool _canGoForward;

    /// <summary>
    /// Collection of open tabs bound to the TabStrip ListView.
    /// </summary>
    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    
    /// <summary>
    /// Stack to track closed tabs for Ctrl+Shift+T functionality.
    /// </summary>
    private readonly Stack<string> _closedTabs = new();
    
    /// <summary>
    /// Events to route actions from ViewModel to View (Code-Behind).
    /// </summary>
    public event Action<string>? NavigationRequested;
    public event Action? FocusOmniboxRequested;
    public event Action? ToggleFullscreenRequested;
    public event Action? OpenDevToolsRequested;

    public MainViewModel() { }

    /// <summary>
    /// Initializes tabs from a saved session or creates a default new tab.
    /// </summary>
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
    private void NavigateOmnibox()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(OmniboxText)) return;
        
        string input = OmniboxText.Trim();
        bool isUrl = Uri.TryCreate(input, UriKind.Absolute, out var uriResult) 
                     && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                     
        if (!isUrl && input.Contains('.') && !input.Contains(' '))
        {
            isUrl = true;
            input = $"https://{input}";
        }

        string finalUrl = isUrl ? input : $"https://www.google.com/search?q={Uri.EscapeDataString(input)}";
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

    /// <summary>
    /// Command to navigate to a specific URL (used by Bookmarks & History).
    /// </summary>
    [RelayCommand]
    private void NavigateToUrl(string url)
    {
        if (SelectedTab != null)
        {
            SelectedTab.Url = url;
            NavigationRequested?.Invoke(url);
        }
    }

    /// <summary>
    /// Updates the state of the Back and Forward buttons.
    /// Called by MainWindow when WebView2 navigation completes.
    /// </summary>
    public void UpdateNavigationState(bool canGoBack, bool canGoForward)
    {
        CanGoBack = canGoBack;
        CanGoForward = canGoForward;
    }

    /// <summary>
    /// Cycles to the next tab in the list.
    /// </summary>
    public void NextTab()
    {
        if (SelectedTab == null || Tabs.Count <= 1) return;
        int index = Tabs.IndexOf(SelectedTab);
        SelectedTab = Tabs[(index + 1) % Tabs.Count];
    }

    /// <summary>
    /// Cycles to the previous tab in the list.
    /// </summary>
    public void PreviousTab()
    {
        if (SelectedTab == null || Tabs.Count <= 1) return;
        int index = Tabs.IndexOf(SelectedTab);
        SelectedTab = Tabs[(index - 1 + Tabs.Count) % Tabs.Count];
    }

    /// <summary>
    /// Switches to a specific tab by index (0-8).
    /// </summary>
    public void SwitchToTab(int index)
    {
        if (index >= 0 && index < Tabs.Count) SelectedTab = Tabs[index];
    }

    /// <summary>
    /// Triggers UI focus on the Omnibox.
    /// </summary>
    public void TriggerFocusOmnibox() => FocusOmniboxRequested?.Invoke();
    
    /// <summary>
    /// Triggers Fullscreen toggle.
    /// </summary>
    public void TriggerToggleFullscreen() => ToggleFullscreenRequested?.Invoke();
    
    /// <summary>
    /// Triggers DevTools window.
    /// </summary>
    public void TriggerOpenDevTools() => OpenDevToolsRequested?.Invoke();

    partial void OnSelectedTabChanging(TabViewModel? value)
    {
        if (value != null) OmniboxText = value.Url;
    }
}
