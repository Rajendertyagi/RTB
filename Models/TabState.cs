using System.Text.Json.Serialization;

namespace TB_Browser.Models;

/// <summary>
/// Serializable state for restoring tabs (URLs, order, pinned status).
/// </summary>
public class TabState
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }

    [JsonPropertyName("orderIndex")]
    public int OrderIndex { get; set; }
}
