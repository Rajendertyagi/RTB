using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using TradingBrowser.Services;
using TradingBrowser.Helpers;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TradingBrowser;

public sealed partial class MainWindow
{
    private async Task PreWarmWebViewEnvironmentAsync()
    {
        try
        {
            string userDataFolder = Path.Combine(AppContext.BaseDirectory, "UserData", "Profile");
            Directory.CreateDirectory(userDataFolder);
            
            // FIX: Explicitly instantiate options to satisfy the specific SDK version compiler
            var options = new CoreWebView2EnvironmentOptions();
            await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            LoggingService.Log("WebView2 Environment pre-warmed successfully.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("WebView2 Pre-warm Error", ex);
        }
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            await MainWebView.EnsureCoreWebView2Async();
            
            var settings = MainWebView.CoreWebView2.Settings;
            settings.IsStatusBarEnabled = false;
            settings.AreDefaultContextMenusEnabled = true;
            settings.IsGeneralAutofillEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsPinchZoomEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
            
            MainWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            MainWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            MainWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            MainWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            
            _downloadService.Initialize(MainWebView.CoreWebView2);
            _navService = new WebViewNavigationService(_downloadService, MainWebView.CoreWebView2, this);
            
            if (!string.IsNullOrEmpty(_shortcutsJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_shortcutsJs);
            if (!string.IsNullOrEmpty(_tradingViewJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_tradingViewJs);

            _isWebViewInitialized = true;
            LoggingService.Log("WebView2 initialized successfully.");

            bool shouldRestore = SettingsService.Get("RestoreSession", "true") == "true";
            if (shouldRestore)
            {
                var restoredTabs = _sessionService.LoadSession(out string? activeId);
                ViewModel.InitializeSession(restoredTabs, activeId);
            }
            else
            {
                ViewModel.InitializeSession(new List<ViewModels.TabViewModel>(), null);
            }
            
            UpdateOmniboxIcon();
        }
        catch (Exception ex)
        {
            LoggingService.Error("WebView2 Init Error", ex);
        }
    }

    private async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        string? msg = args.TryGetWebMessageAsString();
        if (msg == null) return;

        if (msg.StartsWith("SHORTCUT:")) 
            _shortcutService.HandleWebViewMessage(msg);
        else if (msg.StartsWith("LOG:")) 
            LoggingService.Log(msg, "WEBVIEW_JS");
        else if (_navService != null) 
            await _navService.HandleWebMessageAsync(msg);
    }

    private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (_navService != null && _navService.HandleSpecialUri(args.Uri))
        {
            args.Cancel = true;
            return;
        }

        if (ViewModel.SelectedTab != null) 
        { 
            ViewModel.OmniboxText = args.Uri; 
            ViewModel.SelectedTab.Url = args.Uri; 
            ViewModel.SelectedTab.IsLoading = true; 
        }
    }

    private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (ViewModel.SelectedTab != null) 
        { 
            ViewModel.SelectedTab.IsLoading = false; 
            
            if (!args.IsSuccess)
            {
                // FIX: Use sender.Source instead of args.Uri (which doesn't exist on CompletedEventArgs)
                string currentUrl = sender.Source;

                if (args.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted || 
                    args.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
                {
                    LoggingService.Log($"Navigation Interrupted by user/action: {currentUrl}", "INFO");
                }
                else
                {
                    LoggingService.Error($"Nav Failed ({args.WebErrorStatus}): {currentUrl}");
                }
            }
        }
        
        ViewModel.UpdateNavigationState(sender.CanGoBack, sender.CanGoForward);

        if (args.IsSuccess && ViewModel.SelectedTab != null)
        {
            _hbService.AddHistory(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title);
            if (MainSplitView.IsPaneOpen) RefreshSidebar();
        }
    }

    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        if (ViewModel.SelectedTab != null) ViewModel.SelectedTab.Title = sender.DocumentTitle;
    }
}
