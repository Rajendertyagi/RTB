using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace TradingBrowser.Services;

/// <summary>
/// Generates HTML content for the download history page.
/// Separates presentation logic from the MainWindow code-behind for better maintainability.
/// </summary>
public static class DownloadPageGenerator
{
    /// <summary>
    /// Generates a complete HTML page displaying download history grouped by date.
    /// </summary>
    /// <param name="records">List of download records from the database.</param>
    /// <returns>Complete HTML string ready to be loaded into WebView2.</returns>
    public static string GenerateHtml(List<DownloadRecord> records)
    {
        // Group downloads by date for organized display
        var grouped = records.GroupBy(r => r.StartTime.ToString("MMM dd, yyyy"));
        
        string itemsHtml = "";
        foreach (var group in grouped)
        {
            // Add date header for each group
            itemsHtml += $"<div class='date-header'>{WebUtility.HtmlEncode(group.Key)}</div>";
            
            foreach (var item in group)
            {
                // Determine status color based on download state
                string statusColor = GetStatusColor(item.State);
                
                // Build HTML for each download item
                itemsHtml += $@"
                <div class='download-item'>
                    <div class='icon'>📄</div>
                    <div class='info'>
                        <div class='name'>{WebUtility.HtmlEncode(item.FileName)}</div>
                        <div class='status' style='color:{statusColor}'>{WebUtility.HtmlEncode(item.State)}</div>
                    </div>
                    <div class='actions'>
                        <button onclick=""copyLink('{WebUtility.HtmlEncode(item.SourceUrl)}')"">🔗</button>
                        <button onclick=""removeDownload({item.Id})"">🗑️</button>
                    </div>
                </div>";
            }
        }

        // Show empty state if no downloads exist
        if (string.IsNullOrEmpty(itemsHtml))
        {
            itemsHtml = "<div class='empty-state'>No downloads found.</div>";
        }

        // Return complete HTML document with embedded CSS and JavaScript
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ 
                    background-color: #202124; 
                    color: #e8eaed; 
                    font-family: 'Segoe UI', sans-serif; 
                    margin: 0; 
                    padding: 20px; 
                }}
                .header {{ 
                    display: flex; 
                    justify-content: space-between; 
                    align-items: center; 
                    margin-bottom: 20px; 
                    border-bottom: 1px solid #3c4043; 
                    padding-bottom: 10px; 
                }}
                .header h2 {{ 
                    margin: 0; 
                    font-weight: 500; 
                }}
                .clear-btn {{ 
                    background: #303134; 
                    border: 1px solid #5f6368; 
                    color: #8ab4f8; 
                    padding: 5px 15px; 
                    border-radius: 4px; 
                    cursor: pointer; 
                }}
                .clear-btn:hover {{ 
                    background: #3c4043; 
                }}
                .date-header {{ 
                    color: #9aa0a6; 
                    font-size: 12px; 
                    font-weight: bold; 
                    margin-top: 20px; 
                    margin-bottom: 10px; 
                    padding-left: 10px; 
                }}
                .download-item {{ 
                    display: flex; 
                    align-items: center; 
                    background: #303134; 
                    padding: 10px; 
                    margin-bottom: 8px; 
                    border-radius: 4px; 
                }}
                .icon {{ 
                    font-size: 24px; 
                    margin-right: 15px; 
                }}
                .info {{ 
                    flex-grow: 1; 
                }}
                .name {{ 
                    font-size: 14px; 
                    margin-bottom: 4px; 
                }}
                .status {{ 
                    font-size: 12px; 
                }}
                .actions button {{ 
                    background: none; 
                    border: none; 
                    color: #9aa0a6; 
                    font-size: 16px; 
                    cursor: pointer; 
                    padding: 0 5px; 
                }}
                .actions button:hover {{ 
                    color: #e8eaed; 
                }}
                .empty-state {{ 
                    text-align: center; 
                    color: #9aa0a6; 
                    margin-top: 50px; 
                    font-size: 16px;
                }}
            </style>
        </head>
        <body>
            <div class='header'>
                <h2>Downloads</h2>
                <button class='clear-btn' onclick=""clearAll()"">Clear all</button>
            </div>
            {itemsHtml}
            <script>
                function copyLink(url) {{ 
                    window.chrome.webview.postMessage('COPY_LINK:' + url); 
                }}
                function removeDownload(id) {{ 
                    window.chrome.webview.postMessage('REMOVE_DOWNLOAD:' + id); 
                }}
                function clearAll() {{ 
                    window.chrome.webview.postMessage('CLEAR_ALL_DOWNLOADS'); 
                }}
            </script>
        </body>
        </html>";
    }

    /// <summary>
    /// Returns the appropriate color for a download status.
    /// </summary>
    private static string GetStatusColor(string state)
    {
        return state switch
        {
            "Completed" => "#4CAF50",  // Green
            "Failed" => "#F44336",     // Red
            "InProgress" => "#FFC107", // Yellow
            "Canceled" => "#9E9E9E",   // Gray
            _ => "#9aa0a6"             // Default gray
        };
    }
}
