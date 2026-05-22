using System;
using System.Collections.Generic;
using System.Linq;
using TB_Browser.Core.Models;

namespace TB_Browser.Core.Services;

public class TabService
{
    private readonly List<Tab> _tabs = new();
    private int _nextId = 1;
    private Tab? _active;

    public IReadOnlyList<Tab> Tabs => _tabs;
    public Tab? ActiveTab => _active;
    public event EventHandler<Tab>? ActiveTabChanged;
    public event EventHandler<Tab>? TabAdded;
    public event EventHandler<Tab>? TabRemoved;

    public Tab CreateTab()
    {
        var tab = new Tab { Id = _nextId++ };
        _tabs.Add(tab);
        ActivateTab(tab.Id);
        TabAdded?.Invoke(this, tab);
        return tab;
    }

    public void CloseTab(int id)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == id);
        if (tab == null) return;
        _tabs.Remove(tab);
        TabRemoved?.Invoke(this, tab);
        if (_tabs.Count == 0) CreateTab();
        else if (_active?.Id == id) ActivateTab(_tabs.Last().Id);
    }

    public void ActivateTab(int id)
    {
        _active = _tabs.FirstOrDefault(t => t.Id == id);
        if (_active != null) ActiveTabChanged?.Invoke(this, _active);
    }

    public void UpdateTab(int id, string url, string title)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == id);
        if (tab != null) { tab.Url = url; tab.Title = title; }
    }
}
