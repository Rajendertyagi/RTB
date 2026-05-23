using System;
using TB_Browser.Infrastructure;
using TB_Browser.ViewModels;

namespace TB_Browser.Services;

public class TabService
{
    // Fully qualified to resolve ambiguity with System.Threading.Timer
    private readonly System.Timers.Timer _timer;
    public event Action<TabViewModel>? SuspendRequested;

    public TabService()
    {
        _timer = new System.Timers.Timer(60_000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // Timer infrastructure placeholder
    }

    public void RequestSuspend(TabViewModel tab) => SuspendRequested?.Invoke(tab);
}
