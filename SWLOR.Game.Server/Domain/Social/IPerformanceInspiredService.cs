using System;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Puts an Inspired status effect on a creature. A part of the Performance service; it has no static facade.
    /// </summary>
    public interface IPerformanceInspiredService
    {
        /// <summary>
        /// Applies the effect, rebuilding it only when its composition changed; otherwise only its remaining time is refreshed.
        /// Returns true when a new effect was applied, false when an existing one was only refreshed.
        /// </summary>
        /// <param name="creature">The watcher or performer who holds the effect; they are also its source.</param>
        /// <param name="data">The composed bonuses.</param>
        /// <param name="remainingSeconds">The remaining time to leave on the effect. It is never shorter than what the effect has now.</param>
        /// <param name="bankedAgain">True when the holder banked time this tick, which gives them a fresh Requiem.</param>
        bool Apply(uint creature, InspiredEffectData data, double remainingSeconds, bool bankedAgain);

        /// <summary>The seconds of Inspired the creature has right now, or 0 when they have none.</summary>
        double GetRemainingSeconds(uint creature, DateTime nowUtc);

        /// <summary>Extends the creature's existing Inspired to a longer remaining time without changing what it grants.</summary>
        /// <param name="bankedAgain">True when this adds banked time, which gives the holder a fresh Requiem.</param>
        void SetRemaining(uint creature, double remainingSeconds, bool bankedAgain = false);
    }
}
