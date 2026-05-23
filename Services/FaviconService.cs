using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using TB_Browser.Infrastructure;

namespace TB_Browser.Services;

/// <summary>
/// Fetches favicons on-demand and caches them in memory.
/// Returns URL strings compatible with XAML Image.Source binding.
/// </summary>
public class FaviconService
{
    private static readonly HttpClient _httpClient = new();
    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private static readonly TimeSpan _ttl = TimeSpan.FromHours(24);

    private record CacheEntry(string Url, DateTime ExpiresAt);

    public async Task<string> GetFaviconUrlAsync(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return string.Empty;

        // Check cache
        if (_cache.TryGetValue(domain, out var entry) && DateTime.UtcNow < entry.ExpiresAt)
        {
            return entry.Url;
        }

        // Fetch from Google Favicon API
        var fallbackUrl = $"https://www.google.com/s2/favicons?domain={domain}&sz=32";
        try
        {
            // Verify URL is reachable (HEAD request)
            using var request = new HttpRequestMessage(HttpMethod.Head, fallbackUrl);
            using var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _cache[domain] = new CacheEntry(fallbackUrl, DateTime.UtcNow.Add(_ttl));
                return fallbackUrl;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Favicon fetch failed for {domain}", ex);
        }

        // Return fallback on failure (Google API usually returns default icon anyway)
        return fallbackUrl;
    }

    public void ClearCache() => _cache.Clear();
}
