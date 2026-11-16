using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The live performance sessions and the players' performance preferences: the two dictionaries that were
    /// <see cref="PerformanceService"/>'s own, moved here unchanged so the services that only read them do not need it.
    /// </summary>
    public class PerformanceRegistryService : IPerformanceRegistryService
    {
        private readonly Dictionary<uint, PerformanceSession> _sessions = new();
        private readonly Dictionary<string, PerformancePreference> _preferences = new();

        public int Count => _sessions.Count;

        public bool TryGetSession(uint performer, out PerformanceSession session)
        {
            return _sessions.TryGetValue(performer, out session);
        }

        public bool IsPerforming(uint performer)
        {
            return _sessions.ContainsKey(performer);
        }

        public IReadOnlyList<PerformanceSession> GetSessions()
        {
            return _sessions.Values.ToArray();
        }

        public void Add(PerformanceSession session)
        {
            _sessions[session.Performer] = session;
        }

        public bool Remove(uint performer, out PerformanceSession session)
        {
            return _sessions.Remove(performer, out session);
        }

        public PerformancePreference GetPreference(uint player)
        {
            var playerId = GetObjectUUID(player);
            if (!_preferences.TryGetValue(playerId, out var preference))
            {
                preference = new PerformancePreference();
                _preferences[playerId] = preference;
            }

            return preference;
        }
    }
}
