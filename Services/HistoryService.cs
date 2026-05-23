using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using TB_Browser.Infrastructure;
using TB_Browser.Models; // ADDED
using TB_Browser.Repositories;

namespace TB_Browser.Services;

public class HistoryService
{
    private readonly HistoryRepository _repository;
    private readonly ConcurrentQueue<HistoryEntry> _queue = new();
    private readonly object _flushLock = new();
    private bool _isFlushing;
    private bool _purgeDone;

    public HistoryService(HistoryRepository repository) => _repository = repository;

    public void QueueVisit(string url, string? title = null, bool typed = false)
    {
        var entry = new HistoryEntry
        {
            Url = url,
            Title = title,
            LastVisited = DateTime.UtcNow,
            FirstVisited = DateTime.UtcNow,
            TypedCount = typed ? 1 : 0
        };
        _queue.Enqueue(entry);
    }

    public async Task FlushAsync()
    {
        if (!_purgeDone)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            await _repository.PurgeOlderThanAsync(cutoff);
            _purgeDone = true;
        }

        lock (_flushLock)
        {
            if (_isFlushing || _queue.IsEmpty) return;
            _isFlushing = true;
        }

        try
        {
            var batch = DequeueAll();
            if (batch.Count == 0) return;
            await RetryAsync(() => _repository.UpsertBatchAsync(batch), maxRetries: 3);
            LoggingService.Info($"Flushed {batch.Count} history entries.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("History flush failed permanently", ex);
        }
        finally { lock (_flushLock) _isFlushing = false; }
    }

    private List<HistoryEntry> DequeueAll()
    {
        var list = new List<HistoryEntry>();
        while (_queue.TryDequeue(out var item)) list.Add(item);
        return list;
    }

    private static async Task RetryAsync(Func<Task> action, int maxRetries)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try { await action(); return; }
            catch (Exception ex)
            {
                LoggingService.Error($"History attempt {attempt} failed", ex);
                if (attempt == maxRetries) throw;
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
            }
        }
    }
}
