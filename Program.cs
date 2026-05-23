using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace TB;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 1. ✅ Initialize Dynamic Dependency (REQUIRED for unpackaged)
        // 0x00010000 = v1.0.0.0+, fully compatible with SDK 2.x runtime
        Bootstrap.Initialize(0x00010000);

        // 2. Initialize WinRT COM wrappers for WinUI 3
        WinRT.ComWrappersSupport.InitializeComWrappers();

        // 3. Start the application loop
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
