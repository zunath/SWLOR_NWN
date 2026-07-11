using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Marks a status effect as a member of the Leadership damage-reduction family
    /// (Watchful Presence, Cleanse Order, Bolster Resolve, Hold the Line). These grants
    /// do not stack: at any moment, only the strongest active member's damage-reduction
    /// stats are contributed to the creature. Weaker members remain applied (so any other
    /// stats they grant, such as Hold the Line's crowd-control immunity, still function)
    /// but their damage-reduction stat values are suppressed to zero while a stronger
    /// member is active.
    /// </summary>
    public interface ILeadershipDamageReductionStatusEffect
    {
        /// <summary>
        /// The nominal (uncapped by family exclusivity) damage-reduction stat values this
        /// effect grants, captured once when the effect is applied. This is the source of
        /// truth used to rank family members and to restore a member's contribution once a
        /// stronger member is removed; it is never itself mutated by exclusivity handling.
        /// </summary>
        IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; }
    }
}
