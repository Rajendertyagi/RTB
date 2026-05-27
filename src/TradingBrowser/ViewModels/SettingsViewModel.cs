using CommunityToolkit.Mvvm.ComponentModel; // Provides ObservableObject base class and [ObservableProperty] attribute
using CommunityToolkit.Mvvm.Input;        // Provides [RelayCommand] attribute for generating ICommand implementations
using TradingBrowser.Helpers;             // Provides SettingsService for SQLite key-value storage operations
using TradingBrowser.Services;            // Provides LoggingService for thread-safe file-based logging
using System;                             // Provides basic system types, exceptions, and AppContext
using System.IO;                          // Provides Directory and Path classes for file system operations

namespace TradingBrowser.ViewModels;

/// <summary>
/// ViewModel for the Settings ContentDialog overlay.
/// Manages UI state, data binding, and commands for user preferences and data management.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    /// The currently selected index for the Search Engine dropdown.
    /// ✅ FIX: Upgraded to AOT-safe partial property to resolve MVVMTK0045.
    /// </summary>
    [ObservableProperty] 
    public partial int SelectedSearchEngineIndex { get; set; }

    /// <summary>
    /// Boolean state for the "Restore Session on Startup" toggle switch.
    /// ✅ FIX: Upgraded to AOT-safe partial property to resolve MVVMTK0045.
    /// </summary>
    [ObservableProperty] 
    public partial bool RestoreSessionOnStartup { get; set; }

    /// <summary>
    /// Array of available search engines to bind to the ComboBox ItemsSource in the UI.
    /// </summary>
    public string[] SearchEngines { get; } = { "Google", "Bing", "DuckDuckGo" };

    /// <summary>
    /// Constructor: Loads saved settings from the SQLite database upon initialization.
    /// </summary>
    public SettingsViewModel()
    {
        // Retrieve the saved search engine from the database, defaulting to "Google" if the key doesn't exist
        string engine = SettingsService.Get("SearchEngine", "Google");
        
        // Map the retrieved string value to the corresponding array index for the ComboBox UI
        SelectedSearchEngineIndex = engine switch
        {
            "Bing" => 1,
            "DuckDuckGo" => 2,
            _ => 0 // Defaults to Google (index 0)
        };

        // Retrieve the session restore preference from the database, defaulting to true
        RestoreSessionOnStartup = SettingsService.Get("RestoreSession", "true") == "true";
    }

    /// <summary>
    /// Partial method triggered automatically by the Source Generator whenever 'SelectedSearchEngineIndex' changes.
    /// Saves the new selection to the SQLite database immediately.
    /// </summary>
    /// <param name="value">The new integer index selected by the user.</param>
    partial void OnSelectedSearchEngineIndexChanged(int value)
    {
        // Map the index back to the string name and save it to the database
        SettingsService.Set("SearchEngine", SearchEngines[value]);
    }

    /// <summary>
    /// Partial method triggered automatically by the Source Generator whenever 'RestoreSessionOnStartup' changes.
    /// Saves the new boolean state to the SQLite database as a lowercase string.
    /// </summary>
    /// <param name="value">The new boolean state of the toggle switch.</param>
    partial void OnRestoreSessionOnStartupChanged(bool value)
    {
        // Convert the boolean to a lowercase string ("true" or "false") and save it
        SettingsService.Set("RestoreSession", value.ToString().ToLower());
    }

    /// <summary>
    /// Command to clear all browsing data (Cache, Cookies, History) by deleting the WebView2 Profile folder.
    /// The Source Generator creates an ICommand property 'ClearBrowsingDataCommand' from this method.
    /// </summary>
    [RelayCommand]
    private void ClearBrowsingData()
    {
        // Construct the absolute path to the WebView2 user data profile folder relative to the executable
        string profilePath = Path.Combine(AppContext.BaseDirectory, "UserData", "Profile");
        
        // Verify the directory exists before attempting to delete it to prevent exceptions
        if (Directory.Exists(profilePath))
        {
            try
            {
                // Recursively delete the profile folder and all its contents (cache, cookies, local storage)
                Directory.Delete(profilePath, true);
                
                // Log the successful operation for diagnostics
                LoggingService.Log("Browsing data cleared successfully by user.");
            }
            catch (IOException ex)
            {
                // Files might be actively locked by the running WebView2 process. 
                // Log the error; the UI will advise the user that a restart is required to fully apply.
                LoggingService.Error("Could not clear all data. WebView2 may be locking files.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Handle permission issues if the app lacks OS rights to delete the folder
                LoggingService.Error("Unauthorized access when trying to clear browsing data.", ex);
            }
        }
        else
        {
            // Log if the profile folder doesn't exist (nothing to clear, but user clicked the button)
            LoggingService.Log("Clear data requested, but profile folder does not exist.");
        }
    }
}
