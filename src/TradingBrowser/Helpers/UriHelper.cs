using System;

namespace TradingBrowser.Helpers;

/// <summary>
/// Handles URL validation, auto-completion, and search engine fallback.
/// </summary>
public static class UriHelper
{
    /// <summary>
    /// Parses user input into a navigable URL or search query.
    /// </summary>
    /// <param name="input">Raw text from omnibox.</param>
    /// <param name="searchEngine">Default search engine name (Google, Bing, DuckDuckGo).</param>
    /// <returns>Fully qualified URL ready for navigation.</returns>
    public static string ResolveUrl(string input, string searchEngine = "Google")
    {
        if (string.IsNullOrWhiteSpace(input)) return "https://www.google.com";

        string trimmed = input.Trim();

        // 1. Check if input is already a valid absolute URI
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uriResult) && 
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            return trimmed;
        }

        // 2. Check if input looks like a domain (contains dot, no spaces)
        bool isDomain = trimmed.Contains('.') && !trimmed.Contains(' ') && !trimmed.Contains('/');
        if (isDomain)
        {
            // Auto-prepend https://
            return $"https://{trimmed}";
        }

        // 3. Fallback to search engine query
        string query = Uri.EscapeDataString(trimmed);
        return searchEngine switch
        {
            "Bing" => $"https://www.bing.com/search?q={query}",
            "DuckDuckGo" => $"https://duckduckgo.com/?q={query}",
            _ => $"https://www.google.com/search?q={query}"
        };
    }
}
