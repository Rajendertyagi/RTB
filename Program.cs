using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace TB;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Microsoft.WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
