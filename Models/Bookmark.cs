using System;

namespace TB_Browser.Models;

/// <summary>
/// Represents a user bookmark persisted in SQLite.
/// Maps directly to Dapper queries and the Bookmarks table schema.
/// </summary>
public class Bookmark
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? FaviconUrl { get; set; }
    public string Folder { get; set; } = "General";
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public int VisitCount { get; set; }
}
