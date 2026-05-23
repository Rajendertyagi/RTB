using System.Collections.ObjectModel;

namespace TB.Features.Tabs;

public class TabService
{
    public ObservableCollection<TabViewModel> Tabs { get; } = new();

    public void Add(string url = "https://www.google.com", string title = "New Tab")
    {
        var vm = new TabViewModel(url, title, this);
        Tabs.Add(vm);
        SelectedTab = vm;
    }

    public void Remove(TabViewModel tab)
    {
        Tabs.Remove(tab);
        if (Tabs.Count == 0) Add();
        else SelectedTab = Tabs[^1];
    }

    public TabViewModel? SelectedTab { get; set; }
}
