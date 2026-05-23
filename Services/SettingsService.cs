using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using TB_Browser.Infrastructure;
using TB_Browser.Models;

namespace TB_Browser.Services;

/// <summary>
/// Manages app settings persistence and live theme application.
/// </summary>
public class SettingsService
{
    private readonly string _settingsPath;
    public AppSettings Settings { get; private set; } = new();

    public SettingsService(PathResolver pathResolver)
    {
        _settingsPath = Path.Combine(pathResolver.DataDir, "settings.json");
    }

    /// <summary>
    /// Loads settings from disk. Falls back to defaults on failure.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null) Settings = loaded;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load settings", ex);
        }
    }

    /// <summary>
    /// Persists current settings to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to save settings", ex);
        }
    }

    /// <summary>
    /// Applies theme instantly without restart.
    /// </summary>
    public void ApplyTheme(ElementTheme theme)
    {
        // ✅ Fixes CS0029: Explicit safe cast between numerically identical WinUI 3 enums
        Application.Current.RequestedTheme = (ApplicationTheme)(int)theme;
        
        if (Window.Current?.Content is FrameworkElement root)
            root.RequestedTheme = theme;
    }
}
