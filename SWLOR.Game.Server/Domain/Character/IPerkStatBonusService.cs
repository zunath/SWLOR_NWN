using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The stat bonuses perks give: the tables built when the module loads and the reads <see cref="IStatService"/> makes on every
    /// stat recalculation. The reads allocate nothing.
    /// </summary>
    public interface IPerkStatBonusService
    {
        /// <summary>
        /// When the module loads, builds the stat bonus tables from every perk the catalog cached.
        /// </summary>
        void CachePerkStatBonuses();

        IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat);

        int GetStatBonus(uint creature, StatType stat);

        int GetTargetedStatBonus(
            uint creature,
            PerkType targetPerkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType);
    }
}
