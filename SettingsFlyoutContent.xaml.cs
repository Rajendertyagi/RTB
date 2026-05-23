using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TB_Browser.ViewModels;

namespace TB_Browser;

public sealed partial class SettingsFlyoutContent : UserControl
{
    public SettingsFlyoutContent() => InitializeComponent();

    public SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    public List<string> ThemeOptions => new() { "System", "Light", "Dark" };
    public List<string> StartupOptions => new() { "NewTab", "SpecificUrl", "LastSession" };

    private void CloseFlyout_Click(object sender, RoutedEventArgs e)
    {
        var flyout = this.Parent as Flyout;
        flyout?.Hide();
    }
}
