using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The stats a player's equipped Mimicry traits and the elemental-resonance set bonus add, as the stat read asks for them
    /// (part 4 of the stat split, Readmes/MaintainabilityRefactorStatSplit.md). Character owns the service and it builds its
    /// tables from the ability catalog, so the stat read reaches no service above Character (supply rule 1). The reads that
    /// scale by Trait Potency take the stat reader from their caller, which passes itself.
    /// </summary>
    public interface IMimicryTraitStatsService
    {
        /// <summary>
        /// Caches the techniques and the trait stat tables. Runs on OnModuleCacheAfter, right after the Mimicry service's own
        /// cache, so the ability catalog (built on OnModuleCacheBefore) holds every AbilityDetail.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Returns each equipped trait's stat payload with the parameters declared by that trait.
        /// </summary>
        IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat, IStatService statReader);

        /// <summary>
        /// Sums equipped trait adjustments and the elemental-resonance set bonus. These belong
        /// to the loadout rather than status effects that could be cleared on death.
        /// </summary>
        int GetStatBonus(uint creature, StatType stat, IStatService statReader);

        /// <summary>
        /// Damage-type loadout set bonus ("elemental resonance"): equipping multiple active
        /// techniques sharing a damage type grants scaling technique potency. Each damage type with
        /// at least two equipped techniques contributes (count - 1) * 5 percent, summed across types
        /// and capped at 20. Passive traits have no damage element and do not contribute.
        /// </summary>
        int GetSetBonusPotency(Player dbPlayer);
    }
}
