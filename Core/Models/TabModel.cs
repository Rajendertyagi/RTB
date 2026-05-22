using System;
using System.Collections.Generic;
using TB_Browser.Core.Models;

namespace TB_Browser.Core.Services;

public interface ITabService
{
    IReadOnlyList<TabModel> Tabs { get; }
    TabModel? ActiveTab { get; }
    event EventHandler<TabModel> ActiveTabChanged;
    event EventHandler<TabModel> TabAdded;
    event EventHandler<TabModel> TabRemoved;

    TabModel CreateTab();
    void CloseTab(int id);
    void ActivateTab(int id);
    void UpdateTab(int id, string url, string title);
}
