using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NyghtshadeHollow.Faction;

namespace NyghtshadeHollow.Core
{
    public enum JournalTab { Quests, Relationships, Factions, Freeform }

    [System.Serializable]
    public class JournalEntry
    {
        public string ID;
        public JournalTab Tab;
        public string Title;
        public string Body;
        public bool IsRead;
        public System.DateTime Timestamp;
    }

    public class JournalManager : MonoBehaviour
    {
        private readonly List<JournalEntry> _entries = new();
        private int _unreadCount;

        public static event System.Action<JournalEntry> OnEntryAdded;
        public static event System.Action<int> OnUnreadChanged;

        public void AddEntry(JournalTab tab, string title, string body)
        {
            var entry = new JournalEntry
            {
                ID        = System.Guid.NewGuid().ToString(),
                Tab       = tab,
                Title     = title,
                Body      = body,
                IsRead    = false,
                Timestamp = System.DateTime.Now
            };
            _entries.Add(entry);
            _unreadCount++;
            OnEntryAdded?.Invoke(entry);
            OnUnreadChanged?.Invoke(_unreadCount);
        }

        public void MarkRead(string id)
        {
            var entry = _entries.FirstOrDefault(e => e.ID == id);
            if (entry == null || entry.IsRead) return;
            entry.IsRead = true;
            _unreadCount = Mathf.Max(0, _unreadCount - 1);
            OnUnreadChanged?.Invoke(_unreadCount);
        }

        public void MarkAllRead()
        {
            foreach (var e in _entries) e.IsRead = true;
            _unreadCount = 0;
            OnUnreadChanged?.Invoke(0);
        }

        public List<JournalEntry> GetTab(JournalTab tab) =>
            _entries.Where(e => e.Tab == tab).OrderByDescending(e => e.Timestamp).ToList();

        public List<JournalEntry> Search(string query)
        {
            query = query.ToLower();
            return _entries.Where(e =>
                e.Title.ToLower().Contains(query) ||
                e.Body.ToLower().Contains(query)
            ).ToList();
        }

        public int GetUnreadCount() => _unreadCount;
    }
}
