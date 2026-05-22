using Microsoft.UI.Xaml;
using TB_Browser.Core.Services;
using TB_Browser.UI.Controls;

namespace TB_Browser
{
    public partial class App : Application
    {
        public App() => InitializeComponent();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // 1. Instantiate Services
            var tabService = new TabService();
            var browserService = new BrowserService();

            // 2. Wire Services Together
            browserService.TabService = tabService;

            // 3. Create UI Controls & Inject Services
            var tabBar = new TabBar(tabService);
            var addressBar = new AddressBar(browserService);
            var browserView = new BrowserView(browserService);

            // 4. Pass to MainWindow
            m_window = new MainWindow(tabBar, addressBar, browserView, tabService);
            m_window.Activate();
        }

        private Window m_window;
    }
}
