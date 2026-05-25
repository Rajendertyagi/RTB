using Microsoft.Web.WebView2.Core;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using TradingBrowser.Helpers;

namespace TradingBrowser.Services;

/// <summary>
/// Manages browser downloads, intercepting them to save to a portable folder and logging history to SQLite.
/// </summary>
public class DownloadService
{
    private readonly DatabaseService _db;
    private readonly string _downloadsFolder;

    public DownloadService(DatabaseService db)
    {
        _db = db;
        _downloadsFolder = Path.Combine(AppContext.BaseDirectory, "Downloads");
        Directory.CreateDirectory(_downloadsFolder);
    }

    public void Initialize(CoreWebView2 webView)
    {
        webView.DownloadStarting += WebView_DownloadStarting;
    }

    private void WebView_DownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs args)
    {
        try
        {
            string fileName = Path.GetFileName(args.ResultFilePath);
            string savePath = Path.Combine(_downloadsFolder, fileName);
            
            if (File.Exists(savePath))
            {
                string ext = Path.GetExtension(fileName);
                string name = Path.GetFileNameWithoutExtension(fileName);
                int counter = 1;
                while (File.Exists(savePath))
                {
                    savePath = Path.Combine(_downloadsFolder, $"{name} ({counter}){ext}");
                    counter++;
                }
            }

            args.ResultFilePath = savePath;
            args.Handled = true;
            SaveDownloadToDb(args.DownloadOperation.Uri, fileName, savePath, "InProgress");
            LoggingService.Log($"Download started: {fileName}");
        }
        catch (Exception ex) { LoggingService.Error("Error in WebView_DownloadStarting", ex); }
    }

    private void SaveDownloadToDb(string url, string fileName, string path, string state)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Downloads (FileName, SourceUrl, SavePath, State, StartTime) 
                                VALUES (@file, @url, @path, @state, datetime('now'));";
            cmd.Parameters.AddWithValue("@file", fileName);
            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@path", path);
            cmd.Parameters.AddWithValue("@state", state);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex) { LoggingService.Error("Failed to save download record", ex); }
    }

    public void DeleteDownload(int id)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Downloads WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex) { LoggingService.Error("Failed to delete download record", ex); }
    }

    public void ClearAllDownloads()
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Downloads;";
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex) { LoggingService.Error("Failed to clear download records", ex); }
    }

    public List<DownloadRecord> GetHistory()
    {
        var records = new List<DownloadRecord>();
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, FileName, SourceUrl, State, StartTime FROM Downloads ORDER BY StartTime DESC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new DownloadRecord
                {
                    Id = reader.GetInt32(0),
                    FileName = reader.GetString(1),
                    SourceUrl = reader.GetString(2),
                    State = reader.GetString(3),
                    StartTime = reader.GetDateTime(4),
                    Time = reader.GetDateTime(4).ToString("MMM dd, yyyy")
                });
            }
        }
        catch (Exception ex) { LoggingService.Error("Failed to load download history", ex); }
        return records;
    }
}

/// <summary>
/// Data class representing a single download record for UI binding.
/// </summary>
public class DownloadRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}
