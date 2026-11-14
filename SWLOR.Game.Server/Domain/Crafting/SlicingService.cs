using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <inheritdoc cref="ISlicingService" />
    public class SlicingService : ISlicingService
    {
        // The attempts in progress, by player id.
        private readonly Dictionary<string, ActiveSlicingSession> _sessions = new();

        /// <inheritdoc />
        public ActiveSlicingSession GetSession(string playerId) =>
            _sessions.TryGetValue(playerId, out var session) ? session : null;

        /// <inheritdoc />
        public bool HasSession(string playerId) => _sessions.ContainsKey(playerId);

        /// <inheritdoc />
        public void SetSession(string playerId, ActiveSlicingSession session)
        {
            _sessions[playerId] = session;
        }

        /// <inheritdoc />
        public void RemoveSession(string playerId)
        {
            _sessions.Remove(playerId);
        }
    }
}
