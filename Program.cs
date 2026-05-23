using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace TB;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Bootstrap.Initialize(0x00010001);
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p => { var ctx = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()); SynchronizationContext.SetSynchronizationContext(ctx); new App(); });
    }
}
