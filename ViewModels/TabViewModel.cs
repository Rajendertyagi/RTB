using CommunityToolkit.Mvvm.ComponentModel;

namespace TB.ViewModels;

public partial class TabViewModel : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString()[..8];
    [ObservableProperty] private string _title = "New Tab";
    [ObservableProperty] private string _url = string.Empty;
}
