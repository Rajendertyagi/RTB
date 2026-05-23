using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using TB_Browser.Infrastructure;
using TB_Browser.Models;

namespace TB_Browser.Repositories;
public class HistoryRepository
{
    private readonly IDbConnectionFactory _factory;
    public HistoryRepository(IDbConnectionFactory factory) => _factory = factory;
    public async Task<IEnumerable<HistoryEntry>> GetRecentAsync(int limit = 50)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<HistoryEntry>("SELECT * FROM History ORDER BY LastVisited DESC LIMIT @Limit", new { Limit = limit });
    }
    public async Task UpsertBatchAsync(IEnumerable<HistoryEntry> entries)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(@"INSERT INTO History (Url, Title, LastVisited, TypedCount) VALUES (@Url, @Title, @LastVisited, @TypedCount) ON CONFLICT(Url) DO UPDATE SET Title = excluded.Title, LastVisited = excluded.LastVisited, TypedCount = excluded.TypedCount, VisitCount = History.VisitCount + 1;", entries);
    }
    public async Task PurgeOlderThanAsync(DateTime cutoffDate)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM History WHERE LastVisited < @Cutoff", new { Cutoff = cutoffDate });
    }
}
