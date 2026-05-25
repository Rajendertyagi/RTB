using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using TradingBrowser.Helpers;

namespace TradingBrowser.Services;

/// <summary>
/// Handles database operations for browsing history, bookmarks, and omnibox auto-completion.
/// </summary>
public class HistoryBookmarkService
{
    private readonly DatabaseService _db;

    public HistoryBookmarkService(DatabaseService db) => _db = db;

    public void AddHistory(string url, string title)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO History (Url, Title, VisitTime) VALUES (@url, @title, datetime('now'));";
            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex) { LoggingService.Error("Failed to add history entry", ex); }
    }

    public List<string> GetSuggestions(string query)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(query)) return results;
        string likeQuery = $"%{query}%";
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Url FROM History WHERE Url LIKE @query UNION SELECT Url FROM Bookmarks WHERE Url LIKE @query LIMIT 5;";
            cmd.Parameters.AddWithValue("@query", likeQuery);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) results.Add(reader.GetString(0));
        }
        catch (Exception ex) { LoggingService.Error("Failed to load suggestions", ex); }
        return results;
    }

    public List<(string Url, string Title)> GetBookmarks()
    {
        var bookmarks = new List<(string Url, string Title)>();
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Url, Title FROM Bookmarks ORDER BY Position ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) bookmarks.Add((reader.GetString(0), reader.GetString(1)));
        }
        catch (Exception ex) { LoggingService.Error("Failed to load bookmarks", ex); }
        return bookmarks;
    }

    public List<(string Url, string Title, string Time)> GetHistory()
    {
        var history = new List<(string Url, string Title, string Time)>();
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Url, Title, VisitTime FROM History ORDER BY VisitTime DESC LIMIT 100;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) history.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }
        catch (Exception ex) { LoggingService.Error("Failed to load history", ex); }
        return history;
    }

    /// <summary>
    /// Checks if a specific URL exists in the Bookmarks table.
    /// </summary>
    public bool IsBookmarked(string url)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Bookmarks WHERE Url = @url;";
            cmd.Parameters.AddWithValue("@url", url);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// Adds a URL and Title to the Bookmarks table.
    /// </summary>
    public void AddBookmark(string url, string title)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Bookmarks (Url, Title) VALUES (@url, @title);";
            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex) { LoggingService.Error("Failed to add bookmark", ex); }
    }

    /// <summary>
    /// Removes a URL from the Bookmarks table.
    /// </summary>
    public void RemoveBookmark(string url)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Bookmarks WHERE Url = @url;";
            cmd.Parameters.AddWithValue("@url", url);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex) { LoggingService.Error("Failed to remove bookmark", ex); }
    }
}
