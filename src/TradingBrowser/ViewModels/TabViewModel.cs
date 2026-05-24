using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace TradingBrowser.ViewModels;

public partial class TabViewModel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    
    [ObservableProperty] private string _title = "New Tab";
    [ObservableProperty] private string _url = "about:blank";
    [ObservableProperty] private bool _isPinned;
    [ObservableProperty] private bool _isLoading;
    
    // State tracking for Single WebView2 architecture
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoForward;
}
