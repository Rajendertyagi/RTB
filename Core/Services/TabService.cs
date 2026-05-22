using System;
using System.Collections.Generic;
using System.Linq;
using TB_Browser.Core.Logging;
using TB_Browser.Core.Models;

namespace TB_Browser.Core.Services
{
    public class TabService : ITabService
    {
        private readonly ILogger _logger;
        private readonly List<TabModel> _tabs = new();
        private int _nextId = 1;
        private TabModel? _active;

        public TabService(ILogger logger) => _logger = logger;

        public IReadOnlyList<TabModel> Tabs => _tabs;
        public TabModel? ActiveTab => _active;

        public event EventHandler<TabModel>? ActiveTabChanged;
        public event EventHandler<TabModel>? TabAdded;
        public event EventHandler<TabModel>? TabRemoved;

        public TabModel CreateTab()
        {
            var tab = new TabModel { Id = _nextId++ };
            _tabs.Add(tab);
            ActivateTab(tab.Id);
            TabAdded?.Invoke(this, tab);
            _logger.Info("TabService", $"Tab created: #{tab.Id}");
            return tab;
        }

        public void CloseTab(int id)
        {
            var tab = _tabs.FirstOrDefault(t => t.Id == id);
            if (tab == null) return;
            _tabs.Remove(tab);
            TabRemoved?.Invoke(this, tab);
            _logger.Info("TabService", $"Tab closed: #{id}");
            if (_tabs.Count == 0) CreateTab();
            else if (_active?.Id == id) ActivateTab(_tabs.Last().Id);
        }

        public void ActivateTab(int id)
        {
            _active = _tabs.FirstOrDefault(t => t.Id == id);
            ActiveTabChanged?.Invoke(this, _active!);
            _logger.Debug("TabService", $"Tab activated: #{id}");
        }

        public void UpdateTab(int id, string url, string title)
        {
            var tab = _tabs.FirstOrDefault(t => t.Id == id);
            if (tab != null) { tab.Url = url; tab.Title = title; }
        }
    }
}
