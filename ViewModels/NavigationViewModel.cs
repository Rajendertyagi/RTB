using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB_Browser.Infrastructure;
using TB_Browser.Repositories;
using TB_Browser.Services;

namespace TB_Browser.ViewModels;

/// <summary>
/// Manages address bar state, navigation commands, and autocomplete suggestions.
/// </summary>
public partial class NavigationViewModel : ObservableObject
{
    private readonly HistoryRepository _historyRepo;
    private readonly BookmarkRepository _bookmarkRepo;
    private readonly FaviconService _faviconService;

    public NavigationViewModel(HistoryRepository historyRepo, BookmarkRepository bookmarkRepo, FaviconService faviconService)
    {
        _historyRepo = historyRepo;
        _bookmarkRepo = bookmarkRepo;
        _faviconService = faviconService;
        _suggestions = new ObservableCollection<string>();
    }

    [ObservableProperty]
    private string _addressBarText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _suggestions;

    [ObservableProperty]
    private bool _isSuggestionsVisible;

    [RelayCommand]
    private async Task LoadSuggestionsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            Suggestions.Clear();
            IsSuggestionsVisible = false;
            return;
        }

        // Fetch from DB
        var history = await _historyRepo.GetRecentAsync(20);
        var bookmarks = await _bookmarkRepo.GetAllAsync();
        
        var allItems = history.Select(h => h.Url)
            .Concat(bookmarks.Select(b => b.Url))
            .Distinct()
            .ToList();

        var filtered = FuzzyMatcher.Filter(allItems, query, threshold: 2);
        
        Suggestions.Clear();
        foreach (var item in filtered.Take(10))
            Suggestions.Add(item);
        
        IsSuggestionsVisible = Suggestions.Count > 0;
    }

    [RelayCommand]
    public void Navigate(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        // Logic to trigger MainViewModel navigation would go here
        // For now, we just log it.
        LoggingService.Info($"Navigation requested: {url}");
    }
}
