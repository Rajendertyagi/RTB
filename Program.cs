using Photino.NET;
using System;

namespace TbBrowser
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            // 1. Create Toolbar Window (UI Strip)
            var toolbar = new PhotinoWindow(null) // null = no parent
                .SetTitle("TB Toolbar")
                .SetSize(900, 50)
                .SetUseOsDefaultLocation(false)
                .SetLeft(200)
                .SetTop(200)
                .SetResizable(false)
                .SetMaximizable(false)
                .Load("wwwroot/toolbar.html");

            // 2. Create Browser Window (Content)
            var browser = new PhotinoWindow(null)
                .SetTitle("TB Browser")
                .SetSize(900, 600)
                .SetUseOsDefaultLocation(false)
                .SetLeft(200)
                .SetTop(250)
                .Load("https://www.bing.com");

            // 3. Link: Toolbar sends URL → Browser loads it
            toolbar.RegisterWebMessageReceivedHandler((sender, url) =>
            {
                browser.Load(url);
            });

            // 4. Run the app (blocks until windows close)
            // Main window is toolbar; browser closes independently
            toolbar.WaitForClose();
        }
    }
}
