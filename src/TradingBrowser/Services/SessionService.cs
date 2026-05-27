using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using TradingBrowser.ViewModels;

namespace TradingBrowser.Services;

public class SessionService
{
    private readonly DatabaseService _db;

    public SessionService(DatabaseService db) => _db = db;

    public void SaveSession(IEnumerable<TabViewModel> tabs, string? activeTabId)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            using var deleteCmd = conn.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Sessions;";
            deleteCmd.ExecuteNonQuery();

            int pos = 0;
            foreach (var tab in tabs)
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Sessions (TabId, Url, Title, IsActive, Position) VALUES (@id, @url, @title, @active, @pos);";
                insertCmd.Parameters.AddWithValue("@id", tab.Id.ToString());
                insertCmd.Parameters.AddWithValue("@url", tab.Url);
                insertCmd.Parameters.AddWithValue("@title", tab.Title);
                insertCmd.Parameters.AddWithValue("@active", tab.Id.ToString() == activeTabId);
                insertCmd.Parameters.AddWithValue("@pos", pos++);
                insertCmd.ExecuteNonQuery();
            }
            transaction.Commit();
            LoggingService.Info($"[Session] Saved {tabs.Count()} tabs. Active: {activeTabId ?? "none"}");
        }
        catch (Exception ex)
        {
            LoggingService.Error("[Session] SaveSession failed", ex);
        }
    }

    public List<TabViewModel> LoadSession(out string? activeTabId)
    {
        activeTabId = null;
        var tabs = new List<TabViewModel>();
        
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TabId, Url, Title, IsActive FROM Sessions ORDER BY Position ASC;";
            using var reader = cmd.ExecuteReader();
            
            while (reader.Read())
            {
                string tabIdString = reader.GetString(0);
                
                // FIX: Parse the Guid from database to properly restore TabViewModel.Id
                Guid tabId = Guid.Parse(tabIdString);
                
                var tab = new TabViewModel
                {
                    Id = tabId,  // ✅ Critical: Restore the exact Id for active tab matching
                    Url = reader.GetString(1),
                    Title = reader.GetString(2)
                };
                tabs.Add(tab);
                
                // Check if this tab was the active one
                if (reader.GetBoolean(3)) 
                {
                    activeTabId = tabIdString;
                }
            }
            LoggingService.Info($"[Session] Loaded {tabs.Count} tabs from database");
        }
        catch (Exception ex)
        {
            LoggingService.Error("[Session] LoadSession failed", ex);
        }
        
        return tabs;
    }
}
