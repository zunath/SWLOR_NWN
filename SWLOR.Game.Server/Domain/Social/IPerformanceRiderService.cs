using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What a rider perk of a performance requires, and which routines and sets a player has unlocked. A part of the
    /// Performance service.
    /// </summary>
    public interface IPerformanceRiderService
    {
        /// <summary>The Performance skill rank that unlocks a perk's first level, or 0 when it has no rank requirement.</summary>
        int GetRequiredRank(PerkType perk);

        /// <summary>True when the player has the perk that unlocks the routine.</summary>
        bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine);

        /// <summary>True when the player has the perk that unlocks the set.</summary>
        bool IsSetUnlocked(uint player, PerformanceSetType set);

        /// <summary>True when the player may blend the routine as a second one: it is unlocked and they have Showstopper.</summary>
        bool CanBlendSecondRoutine(uint player, PerformanceRoutineType routine);

        /// <summary>True when the player may weave the set in as a second one: it is unlocked and they have Virtuoso.</summary>
        bool CanWeaveSecondSet(uint player, PerformanceSetType set);
    }
}
