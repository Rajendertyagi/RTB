using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using TradingBrowser.Services;
using TradingBrowser.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TradingBrowser;

public sealed partial class MainWindow
{
    private async Task InitializeWebViewAsync()
    {
        LoggingService.Info("InitializeWebViewAsync: Starting...");
        try
        {
            await MainWebView.EnsureCoreWebView2Async();
            _isWebViewInitialized = true;
            LoggingService.Info($"InitializeWebViewAsync: SUCCESS. Runtime: {MainWebView.CoreWebView2.Environment.BrowserVersionString}");

            MainWebView.CoreWebView2.DocumentTitleChanged += (s, e) =>
            {
                string title = MainWebView.CoreWebView2.DocumentTitle;
                LoggingService.Info($"[WebView] Title changed: {title}");
                if (ViewModel.SelectedTab != null) ViewModel.SelectedTab.Title = title;
            };

            MainWebView.CoreWebView2.NavigationStarting += (s, e) =>
            {
                LoggingService.Info($"[WebView] Navigating to: {e.Uri}");
                if (ViewModel.SelectedTab != null) ViewModel.SelectedTab.Url = e.Uri;
            };

            MainWebView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                string uri = MainWebView.CoreWebView2.Source;
                if (e.IsSuccess)
                    LoggingService.Info($"[WebView] Loaded: {uri}");
                else
                    LoggingService.Error($"[WebView] Failed: {uri} | {e.WebErrorStatus}");
            };

            // ✅ FIX: Sync navigation state for Back/Forward buttons
            MainWebView.CoreWebView2.HistoryChanged += (s, e) =>
            {
                ViewModel.CanGoBack = MainWebView.CoreWebView2.CanGoBack;
                ViewModel.CanGoForward = MainWebView.CoreWebView2.CanGoForward;
            };

            MainWebView.CoreWebView2.ProcessFailed += (s, e) => 
                LoggingService.Error($"[WebView] ProcessFailed: {e.ProcessFailedKind}");

            MainWebView.CoreWebView2.WebMessageReceived += (s, e) => 
                LoggingService.Info($"[WebView] JS Bridge: {e.TryGetWebMessageAsString()}");

            string jsErrorCatcher = @"
                window.addEventListener('error', e => window.chrome.webview.postMessage('JS_ERROR: ' + e.message));
                window.addEventListener('unhandledrejection', e => window.chrome.webview.postMessage('PROMISE_ERROR: ' + e.reason));
            ";
            await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(jsErrorCatcher);

            if (!string.IsNullOrEmpty(_shortcutsJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_shortcutsJs);

            if (!string.IsNullOrEmpty(_tradingViewJs))
                await MainWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(_tradingViewJs);

            MainWebView.CoreWebView2.Navigate("https://www.google.com");
            LoggingService.Info("[WebView] Initial navigation triggered.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("InitializeWebViewAsync: FATAL", ex);
            _isWebViewInitialized = false;
        }
    }
}
