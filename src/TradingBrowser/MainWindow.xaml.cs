using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Web.WebView2.Core;
using TradingBrowser.ViewModels;
using TradingBrowser.Services;
using TradingBrowser.Helpers;
using TradingBrowser.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using System.Collections.Generic;

namespace TradingBrowser;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    public DownloadService DownloadManager => _downloadService; 

    private bool _isWebViewInitialized;
    private bool _isSplitPaneActive;
    
    private readonly SessionService _sessionService;
    private readonly ShortcutService _shortcutService;
    private readonly HistoryBookmarkService _hbService;
    private readonly DownloadService _downloadService;
    private WebViewNavigationService? _navService;
    
    private readonly string _shortcutsJs;
    private readonly string _tradingViewJs;

    // Tiling State Variables
    private TabViewModel? _primaryTab;
    private TabViewModel? _secondaryTab;

    public MainWindow()
    {
        this.InitializeComponent();
        RootGrid.DataContext = this; 
        
        if (this.Content is FrameworkElement content) 
            content.RequestedTheme = ElementTheme.Dark;

        _sessionService = new SessionService(App.Db!);
        _hbService = new HistoryBookmarkService(App.Db!);
        _downloadService = new DownloadService(App.Db!);
        
        _shortcutService = new ShortcutService(
            ViewModel, 
            () => _isWebViewInitialized ? MainWebView.CoreWebView2 : null
        );

        _shortcutService.BookmarkRequested += () => {
            if (ViewModel.SelectedTab != null)
                ToggleBookmark(ViewModel.SelectedTab.Url, ViewModel.SelectedTab.Title);
        };

        ViewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(MainViewModel.SelectedTab) || e.PropertyName == nameof(MainViewModel.OmniboxText))
                UpdateOmniboxIcon();
        };

        string shortcutsPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "shortcuts.js");
        _shortcutsJs = File.Exists(shortcutsPath) ? File.ReadAllText(shortcutsPath) : "";

        string tvJsPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "tradingview-tweaks.js");
        _tradingViewJs = File.Exists(tvJsPath) ? File.ReadAllText(tvJsPath) : "";

        SetupTitleBar();
        SetupEventHooks();
        SetupOmniboxAnimations(); 
        
        // PHASE 1: Live Theme Hook
        RootGrid.ActualThemeChanged += RootGrid_ActualThemeChanged;

        // SILKY MOTION: Setup Adaptive Tab Scaling
        SetupAdaptiveTabScaling();

        // VIVALDI TILING: Setup Tiling Engine
        SetupTilingEngine();

        _ = InitializeWebViewAsync();
    }

    private void SetupTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        var appWindow = this.AppWindow;
        appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        appWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
    }

    private void SetupEventHooks()
    {
        RootGrid.PointerPressed += (s, e) => _shortcutService.HandlePointerPressed(e);
        RootGrid.KeyDown += (s, e) => _shortcutService.HandleUiKeyDown(e);
        
        ViewModel.NavigationRequested += url => { if (_isWebViewInitialized) MainWebView.CoreWebView2.Navigate(url); };
        ViewModel.FocusOmniboxRequested += () => { Omnibox.Focus(FocusState.Programmatic); Omnibox.SelectAll(); };
        ViewModel.ToggleFullscreenRequested += ToggleFullscreen;
        ViewModel.OpenDevToolsRequested += () => { if (_isWebViewInitialized) MainWebView.CoreWebView2.OpenDevToolsWindow(); };

        this.AppWindow.Closing += (s, e) => {
            if (ViewModel.SelectedTab != null)
                _sessionService.SaveSession(ViewModel.Tabs, ViewModel.SelectedTab.Id.ToString());
        };
    }

    // ==========================================
    // OMNIBOX & THEME ANIMATIONS
    // ==========================================
    private void RootGrid_ActualThemeChanged(FrameworkElement sender, object args)
    {
        RefreshThemeBrushes();
    }

    private void RefreshThemeBrushes()
    {
        if (Omnibox.FocusState != FocusState.Unfocused)
        {
            OmniboxBorder.BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        }
        else
        {
            OmniboxBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
    }

    private void SetupOmniboxAnimations()
    {
        Omnibox.GotFocus += (s, e) => {
            OmniboxBorder.Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"];
            OmniboxBorder.BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            OmniboxBorder.BorderThickness = new Thickness(1);
        };
        
        Omnibox.LostFocus += (s, e) => {
            OmniboxBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            OmniboxBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            OmniboxBorder.BorderThickness = new Thickness(0);
        };
    }

    private void UpdateOmniboxIcon()
    {
        string url = ViewModel.OmniboxText ?? "";
        bool isHttps = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        bool isNewTab = string.IsNullOrWhiteSpace(url) || url == "https://www.google.com";
        
        OmniboxIcon.Glyph = (isHttps && !isNewTab) ? "\uE72E" : "\uE721";
    }

    private void Omnibox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) 
        { 
            ViewModel.NavigateOmniboxCommand.Execute(null); 
            e.Handled = true; 
        }
    }

    // ==========================================
    // ADAPTIVE TAB WIDTH SCALING
    // ==========================================
    public void SetupAdaptiveTabScaling()
    {
        TabListView.SizeChanged += (_, _) => RecalculateTabWidths();
        ViewModel.Tabs.CollectionChanged += (_, _) => RecalculateTabWidths();
    }

    private void RecalculateTabWidths()
    {
        if (TabListView.ActualWidth <= 0 || ViewModel.Tabs.Count == 0) return;

        // 44px buffer for New Tab button + padding
        double availableWidth = TabListView.ActualWidth - 44; 
        int tabCount = ViewModel.Tabs.Count;
        double targetWidth = availableWidth / tabCount;
        
        // Clamp between 72px (min) and 240px (max)
        double finalWidth = Math.Max(72, Math.Min(240, targetWidth)); 

        foreach (var item in TabListView.Items)
        {
            if (TabListView.ContainerFromItem(item) is ListViewItem container)
            {
                container.Width = finalWidth;
                container.MinWidth = 72;
                container.MaxWidth = 240;
            }
        }
    }

    // ==========================================
    // VIVALDI-STYLE TILING ENGINE
    // ==========================================
    private void SetupTilingEngine()
    {
        ViewModel.TilingLayoutChanged += ApplyTilingLayout;
        ViewModel.TilingTabsChanged += SyncTiledWebViews;
    }

    private void SyncTiledWebViews(ICollection<TabViewModel> tabs)
    {
        if (tabs.Count >= 2)
        {
            _primaryTab = tabs.First();
            _secondaryTab = tabs.Skip(1).First();
            
            if (_isWebViewInitialized)
            {
                MainWebView.CoreWebView2.Navigate(_primaryTab.Url);
                SecondaryWebView.CoreWebView2.Navigate(_secondaryTab.Url);
            }
        }
    }

    private async void ApplyTilingLayout(TilingLayout layout)
    {
        TilingHeader.Visibility = layout == TilingLayout.None ? Visibility.Collapsed : Visibility.Visible;

        if (layout == TilingLayout.None)
        {
            TilingDivider.Visibility = Visibility.Collapsed;
            SecondaryWebView.Visibility = Visibility.Collapsed;
            ResetGridToSingle();
            return;
        }

        SecondaryWebView.Visibility = Visibility.Visible;
        TilingDivider.Visibility = Visibility.Visible;

        await SecondaryWebView.EnsureCoreWebView2Async();
        SecondaryWebView.CoreWebView2.DocumentTitleChanged += (s, e) => UpdateTabTitle(_secondaryTab, SecondaryWebView);
        SecondaryWebView.CoreWebView2.NavigationStarting += (s, e) => UpdateTabUrl(_secondaryTab, e.Uri);

        MainWebView.CoreWebView2.DocumentTitleChanged += (s, e) => UpdateTabTitle(_primaryTab, MainWebView);
        MainWebView.CoreWebView2.NavigationStarting += (s, e) => UpdateTabUrl(_primaryTab, e.Uri);

        switch (layout)
        {
            case TilingLayout.Horizontal:
                ConfigureHorizontalLayout();
                break;
            case TilingLayout.Vertical:
                ConfigureVerticalLayout();
                break;
            case TilingLayout.Grid:
                ConfigureGridLayout();
                break;
        }
    }

    private void ConfigureHorizontalLayout()
    {
        ResetGridToDualColumn();
        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        Grid.SetRow(SecondaryWebView, 0); Grid.SetColumn(SecondaryWebView, 1);
        Grid.SetRow(TilingDivider, 0); Grid.SetColumn(TilingDivider, 1);
        TilingDivider.Height = double.NaN; TilingDivider.Width = 4;
    }

    private void ConfigureVerticalLayout()
    {
        ResetGridToDualRow();
        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        Grid.SetRow(SecondaryWebView, 1); Grid.SetColumn(SecondaryWebView, 0);
        Grid.SetRow(TilingDivider, 1); Grid.SetColumn(TilingDivider, 0);
        TilingDivider.Width = double.NaN; TilingDivider.Height = 4;
    }

    private void ConfigureGridLayout()
    {
        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        Grid.SetRow(SecondaryWebView, 1); Grid.SetColumn(SecondaryWebView, 1);
        TilingDivider.Visibility = Visibility.Collapsed;
    }

    private void ResetGridToSingle()
    {
        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        SecondaryWebView.Visibility = Visibility.Collapsed;
    }

    private void ResetGridToDualColumn()
    {
        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }

    private void ResetGridToDualRow()
    {
        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }

    private void TilingDivider_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (ViewModel.CurrentTilingLayout == TilingLayout.Horizontal)
        {
            double newCol0Width = TilingHost.ColumnDefinitions[0].ActualWidth + e.HorizontalChange;
            double totalWidth = TilingHost.ActualWidth;
            if (newCol0Width > 150 && (totalWidth - newCol0Width) > 150)
            {
                TilingHost.ColumnDefinitions[0].Width = new GridLength(newCol0Width, GridUnitType.Pixel);
                TilingHost.ColumnDefinitions[1].Width = new GridLength(totalWidth - newCol0Width, GridUnitType.Pixel);
            }
        }
        else if (ViewModel.CurrentTilingLayout == TilingLayout.Vertical)
        {
            double newRow0Height = TilingHost.RowDefinitions[0].ActualHeight + e.VerticalChange;
            double totalHeight = TilingHost.ActualHeight;
            if (newRow0Height > 150 && (totalHeight - newRow0Height) > 150)
            {
                TilingHost.RowDefinitions[0].Height = new GridLength(newRow0Height, GridUnitType.Pixel);
                TilingHost.RowDefinitions[1].Height = new GridLength(totalHeight - newRow0Height, GridUnitType.Pixel);
            }
        }
    }

    private void UpdateTabTitle(TabViewModel? tab, WebView2 wv) => tab?.Title = wv.CoreWebView2.DocumentTitle;
    private void UpdateTabUrl(TabViewModel? tab, string url) => tab?.Url = url;

    // Header Button Handlers
    private void SwitchToHorizontal_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchTilingLayoutCommand.Execute(TilingLayout.Horizontal);
    private void SwitchToVertical_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchTilingLayoutCommand.Execute(TilingLayout.Vertical);
    private void SwitchToGrid_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchTilingLayoutCommand.Execute(TilingLayout.Grid);
    private void Untile_Click(object sender, RoutedEventArgs e) => ViewModel.UntileTabsCommand.Execute(null);

    // ==========================================
    // EXISTING NAVIGATION & PANE HANDLERS
    // ==========================================
    private void Back_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoBack) MainWebView.CoreWebView2.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized && MainWebView.CoreWebView2.CanGoForward) MainWebView.CoreWebView2.GoForward(); }
    private void Reload_Click(object sender, RoutedEventArgs e) { if (_isWebViewInitialized) MainWebView.CoreWebView2.Reload(); }
    private void Home_Click(object sender, RoutedEventArgs e) { ViewModel.GoHomeCommand.Execute(null); }
    private void NewTab_Click(object sender, RoutedEventArgs e) { ViewModel.AddTabCommand.Execute(null); }
    private void SplitPane_Click(object sender, RoutedEventArgs e) 
    { 
        if (TabListView.SelectedItems.Count >= 2)
        {
            var selected = TabListView.SelectedItems.Cast<TabViewModel>().ToList();
            ViewModel.TileSelection(selected, TilingLayout.Horizontal);
            TileTabs(selected[0], selected[1]);
        }
        else
        {
            SplitPane(); 
        }
    }

    private void TabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var item in TabListView.Items)
        {
            if (TabListView.ContainerFromItem(item) is ListViewItem container && container.Content is TabViewModel vm)
            {
                if (container.ContentTemplateRoot is TabItemPresenter presenter)
                {
                    presenter.IsActive = (vm == ViewModel.SelectedTab);
                }
            }
        }

        if (!_isWebViewInitialized || ViewModel.SelectedTab == null) return;
        if (TabListView.SelectedItems.Count > 1) return;

        if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabViewModel oldTab) oldTab.Url = MainWebView.CoreWebView2.Source;
        
        var newTab = ViewModel.SelectedTab;
        ViewModel.OmniboxText = newTab.Url;
        if (MainWebView.CoreWebView2.Source != newTab.Url) MainWebView.CoreWebView2.Navigate(newTab.Url);
        UpdateOmniboxIcon();
        
        bool isBookmarked = _hbService.IsBookmarked(newTab.Url);
        BookmarkIcon.Glyph = isBookmarked ? "\uE735" : "\uE734";
    }

    private void Tab_ContextRequested(object sender, ContextRequestedEventArgs e)
    {
        var selectedTabs = TabListView.SelectedItems.Cast<TabViewModel>().ToList();
        TabItemPresenter? tabPresenter = sender as TabItemPresenter;
        
        if (tabPresenter?.DataContext is TabViewModel tabVM)
        {
            if (!selectedTabs.Contains(tabVM)) selectedTabs = new List<TabViewModel> { tabVM };
        }

        var menu = new MenuFlyout();
        var closeItem = new MenuFlyoutItem { Text = "Close tab" };
        closeItem.Click += (s, args) => ViewModel.CloseTabCommand.Execute(selectedTabs.LastOrDefault());
        menu.Items.Add(closeItem);

        var closeOtherItem = new MenuFlyoutItem { Text = "Close other tabs" };
        closeOtherItem.Click += (s, args) => 
        {
            foreach (var t in ViewModel.Tabs.Where(t => !selectedTabs.Contains(t))) ViewModel.CloseTabCommand.Execute(t);
        };
        menu.Items.Add(closeOtherItem);

        if (selectedTabs.Count >= 2)
        {
            var tileItem = new MenuFlyoutItem { Text = $"Tile {selectedTabs.Count} Tabs" };
            tileItem.Click += (s, args) => 
            {
                ViewModel.TileSelection(selectedTabs, TilingLayout.Horizontal);
                TileTabs(selectedTabs[0], selectedTabs[1]);
            };
            menu.Items.Add(tileItem);
        }

        menu.SystemBackdrop = new DesktopAcrylicBackdrop();
        FrameworkElement targetElement = tabPresenter ?? (FrameworkElement)RootGrid;
        if (e.TryGetPosition(targetElement, out Point point)) menu.ShowAt(targetElement, new FlyoutShowOptions { Position = point });
        else menu.ShowAt(targetElement);
        e.Handled = true;
    }

    private void Tab_MiddleClicked(object sender, PointerRoutedEventArgs e) 
    { 
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab) ViewModel.CloseTabCommand.Execute(tab); 
    }
    
    private void Tab_CloseClicked(object sender, RoutedEventArgs e) 
    { 
        if (sender is FrameworkElement el && el.DataContext is TabViewModel tab) ViewModel.CloseTabCommand.Execute(tab); 
    }

    private void TileTabs(TabViewModel primary, TabViewModel secondary)
    {
        ViewModel.SelectedTab = primary;
        _secondaryTab = secondary;
        SplitPane(secondary.Url);
    }

    private async void SplitPane(string? url = null)
    {
        if (_isSplitPaneActive) return;
        _isSplitPaneActive = true;

        TilingHost.RowDefinitions.Clear();
        TilingHost.ColumnDefinitions.Clear();
        TilingHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TilingHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(MainWebView, 0); Grid.SetColumn(MainWebView, 0);
        Grid.SetRow(SecondaryWebView, 0); Grid.SetColumn(SecondaryWebView, 1);
        Grid.SetRow(TilingDivider, 0); Grid.SetColumn(TilingDivider, 1);
        
        TilingDivider.Visibility = Visibility.Visible;
        SecondaryWebView.Visibility = Visibility.Visible;
        TilingHeader.Visibility = Visibility.Visible;

        try
        {
            await SecondaryWebView.EnsureCoreWebView2Async();
            SecondaryWebView.CoreWebView2.Navigate(url ?? "https://www.tradingview.com");
        }
        catch (Exception ex)
        {
            // LoggingService.Error("Secondary WebView Init Error", ex);
        }
    }

    private void CollapsePane()
    {
        if (!_isSplitPaneActive) return;
        _isSplitPaneActive = false;
        _secondaryTab = null;

        ResetGridToSingle();
        ViewModel.UntileTabsCommand.Execute(null);
    }

    private void CollapsePane_Click(object sender, RoutedEventArgs e) => CollapsePane();

    private async void Settings_Click(object sender, RoutedEventArgs e) 
    { 
        var dialog = new ContentDialog
        {
            Title = "Settings",
            Content = "Native settings panel coming soon.",
            CloseButtonText = "Ok",
            XamlRoot = RootGrid.XamlRoot
        };
        await dialog.ShowAsync();
    }
    
    private void ToggleFullscreen()
    {
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            if (presenter.State == OverlappedPresenterState.Maximized && !ExtendsContentIntoTitleBar)
            {
                presenter.Restore();
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }
            else
            {
                ExtendsContentIntoTitleBar = false;
                SetTitleBar(null);
                presenter.SetBorderAndTitleBar(false, false);
                presenter.Maximize();
            }
        }
    }
}
