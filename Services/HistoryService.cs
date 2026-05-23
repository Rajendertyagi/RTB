using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using TB_Browser.Infrastructure;
using TB_Browser.Models;
using TB_Browser.Repositories;

namespace TB_Browser.Services;
public class HistoryService
{
    private readonly HistoryRepository _repository;
    private readonly ConcurrentQueue<HistoryEntry> _queue = new();
    private readonly object _flushLock = new();
    private bool _isFlushing, _purgeDone;
    public HistoryService(HistoryRepository repository) => _repository = repository;
    public void QueueVisit(string url, string? title = null, bool typed = false) => _queue.Enqueue(new HistoryEntry { Url = url, Title = title, LastVisited = DateTime.UtcNow, FirstVisited = DateTime.UtcNow, TypedCount = typed ? 1 : 0 });
    public async Task FlushAsync()
    {
        if (!_purgeDone) { await _repository.PurgeOlderThanAsync(DateTime.UtcNow.AddDays(-30)); _purgeDone = true; }
        lock (_flushLock) { if (_isFlushing || _queue.IsEmpty) return; _isFlushing = true; }
        try
        {
            var batch = new List<HistoryEntry>(); while (_queue.TryDequeue(out var item)) batch.Add(item);
            if (batch.Count == 0) return;
            await RetryAsync(() => _repository.UpsertBatchAsync(batch), 3);
        }
        catch (Exception ex) { LoggingService.Error("History flush failed", ex); }
        finally { lock (_flushLock) _isFlushing = false; }
    }
    private static async Task RetryAsync(Func<Task> action, int maxRetries)
    {
        for (int i = 1; i <= maxRetries; i++) try { await action(); return; } catch (Exception ex) { LoggingService.Error($"Retry {i}", ex); if (i == maxRetries) throw; await Task.Delay(100 * (int)Math.Pow(2, i)); }
    }
}
