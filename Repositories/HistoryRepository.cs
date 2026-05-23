using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using TB_Browser.Infrastructure;
using TB_Browser.Models; // ✅ ADDED: Fixes CS0246 HistoryEntry not found

namespace TB_Browser.Repositories;

/// <summary>
/// Data access layer for browsing history.
/// Uses Dapper + SQLite with UPSERT support and 30-day auto-purge.
/// </summary>
public class HistoryRepository
{
    private readonly IDbConnectionFactory _factory;

    public HistoryRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Fetches the most recent history entries for autocomplete/session restore.
    /// </summary>
    public async Task<IEnumerable<HistoryEntry>> GetRecentAsync(int limit = 50)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM History ORDER BY LastVisited DESC LIMIT @Limit;";
        return await conn.QueryAsync<HistoryEntry>(sql, new { Limit = limit });
    }

    /// <summary>
    /// Bulk upserts history entries from the in-memory queue.
    /// Uses SQLite ON CONFLICT to avoid race conditions and increment VisitCount.
    /// </summary>
    public async Task UpsertBatchAsync(IEnumerable<HistoryEntry> entries)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            INSERT INTO History (Url, Title, LastVisited, TypedCount)
            VALUES (@Url, @Title, @LastVisited, @TypedCount)
            ON CONFLICT(Url) DO UPDATE SET
                Title = excluded.Title,
                LastVisited = excluded.LastVisited,
                TypedCount = excluded.TypedCount,
                VisitCount = History.VisitCount + 1;";
        
        await conn.ExecuteAsync(sql, entries);
    }

    /// <summary>
    /// Removes history entries older than the specified cutoff date.
    /// </summary>
    public async Task PurgeOlderThanAsync(DateTime cutoffDate)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "DELETE FROM History WHERE LastVisited < @Cutoff;";
        await conn.ExecuteAsync(sql, new { Cutoff = cutoffDate });
    }
}
