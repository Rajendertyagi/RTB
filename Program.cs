using Photino.NET;
using System;

namespace TbBrowser
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            // 1. Toolbar Window (UI Strip)
            var toolbar = new PhotinoWindow()
                .SetTitle("TB Toolbar")
                .SetSize(900, 50)
                .SetUseOsDefaultLocation(false)
                .SetLeft(200)
                .SetTop(200)
                .SetResizable(false) // ✅ Fixes: locks size and prevents maximizing
                .Load("wwwroot/toolbar.html");

            // 2. Browser Window (Content)
            var browser = new PhotinoWindow()
                .SetTitle("TB Browser")
                .SetSize(900, 600)
                .SetUseOsDefaultLocation(false)
                .SetLeft(200)
                .SetTop(250)
                .Load("https://www.bing.com");

            // 3. Link: Toolbar sends URL → Browser loads it
            toolbar.RegisterWebMessageReceivedHandler((sender, message) =>
            {
                browser.Load(message);
            });

            // 4. Run the app
            toolbar.WaitForClose();
        }
    }
}
