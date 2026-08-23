using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Marks a status effect as a member of the Leadership damage-reduction family
    /// (Watchful Presence, Rousing Shout, Hold the Line). These grants
    /// do not stack within the same damage channel: at any moment, only the strongest active
    /// contribution to each channel is used. Weaker members remain applied so unrelated stats,
    /// such as Hold the Line's crowd-control immunity and its coverage of other damage types,
    /// still function. Additional recovery riders use separate stat channels.
    /// </summary>
    public interface ILeadershipDamageReductionStatusEffect
    {
        /// <summary>
        /// The nominal (uncapped by family exclusivity) damage-reduction stat values this
        /// effect grants, captured once when the effect is applied. This is the source of
        /// truth used to rank each channel and to restore a contribution once a stronger member
        /// is removed; it is never itself mutated by exclusivity handling.
        /// </summary>
        IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; }
    }
}
