using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace TradingBrowser.ViewModels;

/// <summary>
/// ViewModel for the History/Bookmark Sidebar UI.
/// Exposes collections to bind to ListView controls and events to trigger navigation.
/// </summary>
public partial class HistoryBookmarkViewModel : ObservableObject
{
    // Collection of items to display in the Bookmarks tab of the sidebar
    public ObservableCollection<(string Url, string Title)> Bookmarks { get; } = [];
    // Collection of items to display in the History tab of the sidebar
    public ObservableCollection<(string Url, string Title)> History { get; } = [];

    /// <summary>
    /// Event triggered when the user clicks an item in the sidebar lists.
    /// </summary>
    public event System.Action<string>? ItemSelected;

    /// <summary>
    /// Updates the sidebar lists with fresh data from the database.
    /// </summary>
    public void LoadData(System.Collections.Generic.List<(string Url, string Title)> bookmarks, System.Collections.Generic.List<(string Url, string Title)> history)
    {
        // Clear current UI lists before repopulating
        Bookmarks.Clear();
        History.Clear();
        
        foreach (var item in bookmarks) Bookmarks.Add(item);
        foreach (var item in history) History.Add(item);
    }
}
