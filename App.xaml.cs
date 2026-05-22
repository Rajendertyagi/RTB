using System.Windows;
using TB_Browser.Core.Services;
using TB_Browser.UI.Controls;

namespace TB_Browser
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Services
            var tabService = new TabService();
            var browserService = new BrowserService();
            browserService.TabService = tabService;

            // 2. UI Controls
            var tabBar = new TabBar(tabService);
            var addressBar = new AddressBar(browserService);
            var browserView = new BrowserView(browserService);

            // 3. Bind tab changes
            tabService.ActiveTabChanged += (s, tab) => browserView.SwitchTo(tab);

            // 4. Show window
            var mainWindow = new MainWindow(tabBar, addressBar, browserView);
            mainWindow.Show();
        }
    }
}
