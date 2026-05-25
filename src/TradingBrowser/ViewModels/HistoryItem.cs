namespace TradingBrowser.ViewModels;

/// <summary>
/// Data class representing a single history entry for UI binding.
/// </summary>
public class HistoryItem
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string VisitTime { get; set; } = string.Empty;
}
