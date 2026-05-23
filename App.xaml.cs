using Microsoft.UI.Xaml;

namespace TestWinUI3;

public partial class App : Application
{
    public App() { InitializeComponent(); }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // Required for unpackaged WinUI 3 dynamic dependencies
        Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.Initialize(0x00010001);
        var window = new MainWindow();
        window.Activate();
    }
}
