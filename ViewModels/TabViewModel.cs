using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TB_Browser.ViewModels;

/// <summary>
/// Represents the state of a single browser tab.
/// Bound to TabViewItem in XAML.
/// </summary>
public partial class TabViewModel : ObservableObject
{
    private readonly Action<TabViewModel> _onClose;

    public TabViewModel(string url, Action<TabViewModel> onClose)
    {
        _url = url;
        _onClose = onClose;
        _title = "New Tab";
    }

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _title = "New Tab";

    [ObservableProperty]
    private string _faviconUrl = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isSuspended;

    [RelayCommand]
    private void Close() => _onClose(this);
}
