using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace TB.Features.Navigation;

public partial class NavigationViewModel : ObservableObject
{
    [ObservableProperty] private string _addressBarText = string.Empty;
    [ObservableProperty] private List<string> _suggestions = new();
    public void UpdateSuggestions(string query) { /* Phase 2 */ }
}
