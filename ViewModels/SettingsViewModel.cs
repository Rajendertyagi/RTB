using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB_Browser.Models;
using TB_Browser.Services;

namespace TB_Browser.ViewModels;

/// <summary>
/// View Model for the Settings Flyout/Page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly UpdateService _updateService;

    public SettingsViewModel(SettingsService settingsService, UpdateService updateService)
    {
        _settingsService = settingsService;
        _updateService = updateService;
        
        // Sync properties from service
        Theme = _settingsService.Settings.Theme;
        StartupMode = _settingsService.Settings.StartupMode;
        StartupUrl = _settingsService.Settings.StartupUrl;
    }

    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private string _startupMode = "NewTab";

    [ObservableProperty]
    private string _startupUrl = "https://www.google.com";

    [ObservableProperty]
    private string _updateStatus = "Up to date";

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _settingsService.Settings.Theme = Theme;
        _settingsService.Settings.StartupMode = StartupMode;
        _settingsService.Settings.StartupUrl = StartupUrl;
        
        await _settingsService.SaveAsync();
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        UpdateStatus = "Checking...";
        var result = await _updateService.CheckLatestAsync();
        UpdateStatus = result ? "Update Available" : "Up to date";
    }
}
