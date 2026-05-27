using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace TradingBrowser.ViewModels;

/// <summary>
/// ViewModel for MainWindow that handles UI state and commands.
/// Uses CommunityToolkit.Mvvm for automatic property change notification and command generation.
/// </summary>
public partial class MainViewModel : ObservableObject  // ✅ FIX: Renamed to match XAML bindings
{
    /// <summary>
    /// The text displayed in the omnibox (address bar).
    /// Observable property automatically notifies UI of changes.
    /// </summary>
    // ✅ FIX: AOT-safe public partial property
    [ObservableProperty]
    public partial string OmniboxText { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the back button should be enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool CanGoBack { get; set; }

    /// <summary>
    /// Indicates whether the forward button should be enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool CanGoForward { get; set; }

    /// <summary>
    /// Indicates whether the current page is loading.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// The currently selected tab.
    /// </summary>
    [ObservableProperty]
    public partial TabViewModel? SelectedTab { get; set; }

    /// <summary>
    /// Event triggered when navigation is requested from the ViewModel.
    /// Allows the View to handle the actual navigation.
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Event triggered when omnibox focus is requested.
    /// </summary>
    public event Action? FocusOmniboxRequested;

    /// <summary>
    /// Event triggered when fullscreen toggle is requested.
    /// </summary>
    public event Action? ToggleFullscreenRequested;

    /// <summary>
    /// Event triggered when dev tools should be opened.
    /// </summary>
    public event Action? OpenDevToolsRequested;

    /// <summary>
    /// Command to navigate to the URL in the omnibox.
    /// Generated automatically by [RelayCommand] attribute.
    /// </summary>
    [RelayCommand]
    private void NavigateOmnibox()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(OmniboxText)) return;
        
        string input = OmniboxText.Trim();
        
        // Determine if input is a URL or search query
        bool isUrl = Uri.TryCreate(input, UriKind.Absolute, out var uriResult) 
                     && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        
        // Auto-prepend https:// if it looks like a domain
        if (!isUrl && input.Contains('.') && !input.Contains(' '))
        {
            isUrl = true;
            input = $"https://{input}";
        }

        // Build final URL (either direct navigation or Google search)
        string finalUrl = isUrl ? input : $"https://www.google.com/search?q={Uri.EscapeDataString(input)}";
        
        // Update tab state
        SelectedTab.Url = finalUrl;
        
        // Notify View to perform navigation
        NavigationRequested?.Invoke(finalUrl);
    }

    /// <summary>
    /// Command to navigate to the home page.
    /// </summary>
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
    /// Triggers focus on the omnibox.
    /// Called by keyboard shortcuts.
    /// </summary>
    public void TriggerFocusOmnibox() => FocusOmniboxRequested?.Invoke();

    /// <summary>
    /// Toggles fullscreen mode.
    /// Called by F11 key or menu.
    /// </summary>
    public void TriggerToggleFullscreen() => ToggleFullscreenRequested?.Invoke();

    /// <summary>
    /// Opens developer tools.
    /// Called by F12 key or menu.
    /// </summary>
    public void TriggerOpenDevTools() => OpenDevToolsRequested?.Invoke();

    /// <summary>
    /// Updates navigation button states based on WebView capabilities.
    /// </summary>
    public void UpdateNavigationState(bool canGoBack, bool canGoForward)
    {
        CanGoBack = canGoBack;
        CanGoForward = canGoForward;
    }

    /// <summary>
    /// Partial method called automatically when SelectedTab changes.
    /// Updates the omnibox text to match the new tab's URL.
    /// </summary>
    // ✅ FIX: Correct partial method name (CommunityToolkit.Mvvm generates On{Property}Changing)
    partial void OnSelectedTabChanging(TabViewModel? value)
    {
        if (value != null) 
            OmniboxText = value.Url;
    }

    /// <summary>
    /// Navigates to a specific URL.
    /// Used by bookmarks and history.
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
}
