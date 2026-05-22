using System;
using System.Windows;
using TB_Browser.Core.Services;
using TB_Browser.UI.Controls;

namespace TB_Browser;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var tabSvc = new TabService();
            var browserSvc = new BrowserService { TabService = tabSvc };

            var tabBar = new TabBar(tabSvc);
            var addressBar = new AddressBar(browserSvc);
            var browserView = new BrowserView(browserSvc);

            tabSvc.ActiveTabChanged += (_, tab) => { if (tab != null) browserView.SwitchTo(tab); };
            tabSvc.CreateTab(); // Create first tab after UI is ready

            new MainWindow(tabBar, addressBar, browserView).Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
