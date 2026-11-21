using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.InitiativeTrackerService
{
    /// <summary>
    /// One initiative tracker in an area: who has joined it, its entries sorted highest first, and the turn marker.
    /// Holds no engine state, so its rules can be tested on their own.
    /// </summary>
    public class InitiativeTrackerSession
    {
        public const int MinValue = -999;
        public const int MaxValue = 9999;
        public const int MinStep = 1;
        public const int MaxStep = 20;
        public const int MaxNameLength = 32;

        private readonly List<uint> _members = new();
        private readonly List<InitiativeTrackerEntry> _entries = new();
        private int _nextEntryId = 1;
        private int _nextAddedOrder;
        private int _step = MinStep;

        public InitiativeTrackerSession(int id, uint area, uint host)
        {
            Id = id;
            Area = area;
            Host = host;
            _members.Add(host);
        }

        public int Id { get; }
        public uint Area { get; }

        /// <summary>
        /// The member who started the tracker, or the longest-joined member after the host leaves.
        /// </summary>
        public uint Host { get; private set; }

        /// <summary>
        /// The entry whose turn it is, or null before anyone has moved the turn.
        /// </summary>
        public int? CurrentEntryId { get; private set; }

        /// <summary>
        /// How many places Prev and Next move the turn, shared by everyone viewing the tracker.
        /// </summary>
        public int Step
        {
            get => _step;
            set => _step = Math.Clamp(value, MinStep, MaxStep);
        }

        public IReadOnlyList<uint> Members => _members;
        public bool IsEmpty => _members.Count == 0;

        /// <summary>
        /// Entries from the highest value to the lowest. Equal values keep the order they were added in.
        /// </summary>
        public IReadOnlyList<InitiativeTrackerEntry> Entries => _entries;

        public bool IsMember(uint member) => _members.Contains(member);

        public void Join(uint member)
        {
            if (!_members.Contains(member))
                _members.Add(member);
        }

        /// <summary>
        /// Removes a member and their entry. The host role passes to the longest-joined member left.
        /// </summary>
        public void Leave(uint member)
        {
            if (!_members.Remove(member))
                return;

            var entry = GetEntryOf(member);
            if (entry != null)
                RemoveEntry(entry.Id);

            if (Host == member)
                Host = _members.Count > 0 ? _members[0] : OBJECT_INVALID;
        }

        public InitiativeTrackerEntry GetEntryOf(uint member)
        {
            return _entries.FirstOrDefault(entry => !entry.IsNamed && entry.Owner == member);
        }

        /// <summary>
        /// Adds or updates a member's own entry. Returns false for a non-member.
        /// </summary>
        public bool SetMemberEntry(uint member, int value)
        {
            if (!IsMember(member))
                return false;

            value = ClampValue(value);
            var entry = GetEntryOf(member);
            if (entry != null)
                entry.Value = value;
            else
                _entries.Add(new InitiativeTrackerEntry
                {
                    Id = _nextEntryId++,
                    Owner = member,
                    Value = value,
                    AddedOrder = _nextAddedOrder++,
                });

            Sort();
            return true;
        }

        /// <summary>
        /// Adds a named entry, such as an NPC a host or DM is running. Returns the entry, or null for a blank name.
        /// </summary>
        public InitiativeTrackerEntry AddNamedEntry(string name, int value)
        {
            name = name?.Trim() ?? string.Empty;
            if (name.Length == 0)
                return null;

            if (name.Length > MaxNameLength)
                name = name[..MaxNameLength];

            var entry = new InitiativeTrackerEntry
            {
                Id = _nextEntryId++,
                Owner = OBJECT_INVALID,
                Name = name,
                Value = ClampValue(value),
                AddedOrder = _nextAddedOrder++,
            };
            _entries.Add(entry);
            Sort();
            return entry;
        }

        /// <summary>
        /// Removes an entry. If it held the turn, the turn passes to the entry that moves into its place,
        /// or to the top of the list when it was last.
        /// </summary>
        public bool RemoveEntry(int entryId)
        {
            var index = _entries.FindIndex(entry => entry.Id == entryId);
            if (index < 0)
                return false;

            _entries.RemoveAt(index);
            if (CurrentEntryId == entryId)
            {
                CurrentEntryId = _entries.Count == 0
                    ? null
                    : _entries[index < _entries.Count ? index : 0].Id;
            }

            return true;
        }

        /// <summary>
        /// Clears every entry and the turn marker. Members stay joined.
        /// </summary>
        public void Reset()
        {
            _entries.Clear();
            CurrentEntryId = null;
        }

        /// <summary>
        /// Moves the turn by <see cref="Step"/> places, wrapping around the list. The first move lands on the top
        /// entry going forward, or the bottom entry going back.
        /// </summary>
        public void MoveTurn(bool forward)
        {
            if (_entries.Count == 0)
            {
                CurrentEntryId = null;
                return;
            }

            var index = CurrentEntryId == null
                ? -1
                : _entries.FindIndex(entry => entry.Id == CurrentEntryId);

            int next;
            if (index < 0)
            {
                next = forward ? 0 : _entries.Count - 1;
            }
            else
            {
                var offset = forward ? Step : -Step;
                next = ((index + offset) % _entries.Count + _entries.Count) % _entries.Count;
            }

            CurrentEntryId = _entries[next].Id;
        }

        public static int ClampValue(int value) => Math.Clamp(value, MinValue, MaxValue);

        private void Sort()
        {
            var sorted = _entries
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.AddedOrder)
                .ToList();
            _entries.Clear();
            _entries.AddRange(sorted);
        }
    }
}
