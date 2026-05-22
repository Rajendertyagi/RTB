using System;
using System.Windows;
using TB_Browser.Core.Logging;
using TB_Browser.Core.Services;
using TB_Browser.UI.Controls;

namespace TB_Browser
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Logger.Info("App", "Started");
            try
            {
                var tabSvc = new TabService();
                var browserSvc = new BrowserService();
                browserSvc.TabService = tabSvc;

                // ✅ FIX: Create the first tab so the UI isn't empty
                tabSvc.CreateTab(); 

                var tabBar = new TabBar(tabSvc);
                var addressBar = new AddressBar(browserSvc);
                var browserView = new BrowserView(browserSvc);
                tabSvc.ActiveTabChanged += (s, tab) => browserView.SwitchTo(tab);

                var win = new MainWindow(tabBar, addressBar, browserView);
                win.Show();
                Logger.Info("App", "Window shown");
            }
            catch (Exception ex) { Logger.Error("App", ex.Message); MessageBox.Show("Startup failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); Shutdown(1); }
        }
    }
}
