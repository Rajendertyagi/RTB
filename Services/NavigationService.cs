using System;
using TB_Browser.Infrastructure;

namespace TB_Browser.Services;

/// <summary>
/// Handles URL formatting, validation, and HTTPS upgrade logic.
/// Prepares user input for WebView2 navigation.
/// </summary>
public class NavigationService
{
    /// <summary>
    /// Converts raw address bar input into a valid navigation URI.
    /// Detects domains vs search queries automatically.
    /// </summary>
    public string FormatUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "https://www.google.com";

        input = input.Trim();

        // Already a valid absolute URI with http/https
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && 
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return input;
        }

        // Looks like a domain (contains dot, no spaces)
        if (input.Contains('.') && !input.Contains(' '))
            return $"https://{input}";

        // Treat as search query
        return $"https://www.google.com/search?q={Uri.EscapeDataString(input)}";
    }

    /// <summary>
    /// Determines if an HTTP URL should be upgraded to HTTPS per user settings.
    /// </summary>
    public bool ShouldUpgradeToHttps(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }
}
