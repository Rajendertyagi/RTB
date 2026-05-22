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

            var logger = new FileLogger();
            logger.Info("App", "Application started");

            try
            {
                var tabService = new TabService(logger);
                var browserService = new BrowserService(logger);
                browserService.TabService = tabService;

                var tabBar = new TabBar(tabService);
                var addressBar = new AddressBar(browserService);
                var browserView = new BrowserView(browserService);

                tabService.ActiveTabChanged += (s, tab) => browserView.SwitchTo(tab);

                var mainWindow = new MainWindow(tabBar, addressBar, browserView);
                mainWindow.Show();

                logger.Info("App", "MainWindow shown");
            }
            catch (Exception ex)
            {
                logger.Critical("App", "Startup failed", ex);
                MessageBox.Show($"Fatal Error: {ex.Message}", "TB Browser", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}
