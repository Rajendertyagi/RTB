using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TB_Browser.Infrastructure;
using TB_Browser.Models;
using TB_Browser.Repositories;

namespace TB_Browser.Services;

/// <summary>
/// Manages bookmark queue and flushes to SQLite on demand.
/// Implements silent retry on failure.
/// </summary>
public class BookmarkService
{
    private readonly BookmarkRepository _repository;
    private readonly ConcurrentQueue<Bookmark> _queue = new();
    private readonly object _flushLock = new();
    private bool _isFlushing;

    public BookmarkService(BookmarkRepository repository) => _repository = repository;

    public void QueueBookmark(string url, string title, string? faviconUrl = null, string folder = "General")
    {
        var bookmark = new Bookmark
        {
            Url = url,
            Title = title,
            FaviconUrl = faviconUrl,
            Folder = folder,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            VisitCount = 1
        };
        _queue.Enqueue(bookmark);
    }

    public async Task FlushAsync()
    {
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
            LoggingService.Info($"Flushed {batch.Count} bookmarks.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Bookmark flush failed permanently", ex);
        }
        finally
        {
            lock (_flushLock) _isFlushing = false;
        }
    }

    private List<Bookmark> DequeueAll()
    {
        var list = new List<Bookmark>();
        while (_queue.TryDequeue(out var item)) list.Add(item);
        return list;
    }

    private static async Task RetryAsync(Func<Task> action, int maxRetries)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Attempt {attempt} failed", ex);
                if (attempt == maxRetries) throw;
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)));
            }
        }
    }
}
