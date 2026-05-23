using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace TB.Features.Tabs;

public partial class TabViewModel : ObservableObject
{
    private readonly TabService _service;
    [ObservableProperty] private string _url = string.Empty;
    [ObservableProperty] private string _title = "New Tab";
    [ObservableProperty] private bool _isBusy;
    public TabViewModel(string url, string title, TabService service) => (_url, _title, _service) = (url, title, service);
    [RelayCommand] private void Close() => _service.Remove(this);
}
