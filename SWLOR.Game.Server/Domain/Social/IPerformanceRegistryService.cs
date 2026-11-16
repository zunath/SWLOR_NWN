using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The live performance sessions and the players' performance preferences. Every other Performance service reads them
    /// here, so none of them needs the main <see cref="IPerformanceService"/> to see who is performing.
    /// </summary>
    public interface IPerformanceRegistryService
    {
        /// <summary>The performer's live session, when they have one.</summary>
        bool TryGetSession(uint performer, out PerformanceSession session);

        /// <summary>True when the player has a live session.</summary>
        bool IsPerforming(uint performer);

        /// <summary>The number of live sessions.</summary>
        int Count { get; }

        /// <summary>A copy of the live sessions.</summary>
        IReadOnlyList<PerformanceSession> GetSessions();

        /// <summary>Makes the session the performer's live one.</summary>
        void Add(PerformanceSession session);

        /// <summary>Removes the performer's live session and returns it, when they had one.</summary>
        bool Remove(uint performer, out PerformanceSession session);

        /// <summary>The player's performance preferences, created on first use.</summary>
        PerformancePreference GetPreference(uint player);
    }
}
