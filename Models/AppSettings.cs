using System;

namespace TB_Browser.Models;

/// <summary>
/// User preferences persisted to settings.json.
/// </summary>
public class AppSettings
{
    public string StartupMode { get; set; } = "NewTab"; // NewTab, SpecificUrl, LastSession
    public string StartupUrl { get; set; } = "https://www.google.com";
    public string Theme { get; set; } = "System"; // Light, Dark, System
    public bool BlockThirdPartyCookies { get; set; } = true;
    public bool HttpsOnly { get; set; } = true;
    public bool ClearOnExit { get; set; } = false;
    public string SearchProvider { get; set; } = "https://www.google.com/search?q={searchTerms}";
}
