using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace TB.Features.Tabs;

public partial class TabViewModel : ObservableObject
{
    private readonly TabService _service;
    [ObservableProperty] private string _url = string.Empty;
    [ObservableProperty] private string _title = "New Tab";
    [ObservableProperty] private bool _isBusy;

    // Explicit constructor (no tuple deconstruction)
    public TabViewModel(string url, string title, TabService service)
    {
        _url = url;
        _title = title;
        _service = service;
    }

    // ✅ Make public so MainWindow can call directly
    public void Close() => _service.Remove(this);
}
