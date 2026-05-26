using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace TradingBrowser.ViewModels;

public partial class TabViewModel : ObservableObject
{
    // Standard property (never changes after creation, so it doesn't need [ObservableProperty])
    // Required by SessionService for saving/restoring sessions
    public Guid Id { get; set; } = Guid.NewGuid();

    // AOT-compatible partial properties (Fixes MVVMTK0045 warnings)
    [ObservableProperty]
    public partial string Title { get; set; } = "New Tab";

    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool CanGoBack { get; set; }

    [ObservableProperty]
    public partial bool CanGoForward { get; set; }
}
