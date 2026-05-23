using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using TB_Browser.Infrastructure;
using TB_Browser.Models; // ✅ ADDED: Fixes CS0246 HistoryEntry not found
using TB_Browser.Repositories;

namespace TB_Browser.Services;

/// <summary>
/// Manages the in-memory history visit queue, flushes to SQLite, 
/// and enforces 30-day retention policy. Thread-safe and silent on failure.
/// </summary>
public class HistoryService
{
    private readonly HistoryRepository _repository;
    private readonly ConcurrentQueue<HistoryEntry> _queue = new();
    private readonly object _flushLock = new();
    private bool _isFlushing;
    private bool _purgeDone;

    public HistoryService(HistoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Queues a new page visit for batch persistence.
    /// </summary>
    public void QueueVisit(string url, string? title = null, bool typed = false)
    {
        var entry = new HistoryEntry
        {
            Url = url,
            Title = title,
            LastVisited = DateTime.UtcNow,
            FirstVisited = DateTime.UtcNow, // Will be set correctly on UPSERT
            TypedCount = typed ? 1 : 0
        };
        _queue.Enqueue(entry);
    }

    /// <summary>
    /// Flushes queued visits to SQLite. Runs 30-day purge once per session.
    /// Uses silent retry with exponential backoff on DB errors.
    /// </summary>
    public async Task FlushAsync()
    {
        // Run purge exactly once per application session
        if (!_purgeDone)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            await _repository.PurgeOlderThanAsync(cutoff);
            _purgeDone = true;
        }

        // Prevent concurrent flushes
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
            LoggingService.Info($"History flushed: {batch.Count} entries.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("History flush failed permanently", ex);
        }
        finally
        {
            lock (_flushLock) _isFlushing = false;
        }
    }

    /// <summary>
    /// Safely drains the concurrent queue into a list for batch processing.
    /// </summary>
    private List<HistoryEntry> DequeueAll()
    {
        var list = new List<HistoryEntry>();
        while (_queue.TryDequeue(out var item))
        {
            list.Add(item);
        }
        return list;
    }

    /// <summary>
    /// Retries an async action with exponential backoff.
    /// </summary>
    private static async Task RetryAsync(Func<Task> action, int maxRetries)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await action();
                return; // Success
            }
            catch (Exception ex)
            {
                LoggingService.Error($"History flush attempt {attempt} failed", ex);
                if (attempt == maxRetries) throw;
                
                // Exponential backoff: 100ms -> 200ms -> 400ms
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
            }
        }
    }
}
