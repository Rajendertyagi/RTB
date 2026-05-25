using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;

namespace TradingBrowser.Services;

/// <summary>
/// Handles all WebView2 navigation logic including special URI routing.
/// Separates navigation concerns from MainWindow for better testability and maintainability.
/// </summary>
public class WebViewNavigationService
{
    private readonly DownloadService _downloadService;
    private readonly CoreWebView2 _webView;

    public WebViewNavigationService(DownloadService downloadService, CoreWebView2 webView)
    {
        _downloadService = downloadService;
        _webView = webView;
    }

    /// <summary>
    /// Handles navigation to special internal URIs like 'about:downloads'.
    /// </summary>
    /// <param name="uri">The URI being navigated to.</param>
    /// <returns>True if the navigation was handled internally, false if WebView should proceed normally.</returns>
    public bool HandleSpecialUri(string uri)
    {
        // Check if navigating to the downloads page
        if (uri == "about:downloads")
        {
            LoadDownloadsPage();
            return true; // Cancel default navigation
        }

        return false; // Let WebView handle normally
    }

    /// <summary>
    /// Loads the download history page by generating HTML and navigating to it.
    /// </summary>
    private void LoadDownloadsPage()
    {
        // Fetch download records from database
        var records = _downloadService.GetHistory();
        
        // Generate HTML using the DownloadPageGenerator service
        string html = DownloadPageGenerator.GenerateHtml(records);
        
        // Navigate WebView to the generated HTML
        _webView.NavigateToString(html);
    }

    /// <summary>
    /// Handles messages received from the downloads page JavaScript.
    /// </summary>
    /// <param name="message">The message string from webview.postMessage().</param>
    public void HandleDownloadPageMessage(string message)
    {
        if (message.StartsWith("REMOVE_DOWNLOAD:"))
        {
            int id = int.Parse(message.Replace("REMOVE_DOWNLOAD:", ""));
            _downloadService.DeleteDownload(id);
            LoadDownloadsPage(); // Refresh the page
        }
        else if (message == "CLEAR_ALL_DOWNLOADS")
        {
            _downloadService.ClearAllDownloads();
            LoadDownloadsPage(); // Refresh the page
        }
        else if (message.StartsWith("COPY_LINK:"))
        {
            string url = message.Replace("COPY_LINK:", "");
            // In a full implementation, this would copy to clipboard
            // For now, we just log it
            LoggingService.Log($"Copy link requested: {url}");
        }
    }
}
