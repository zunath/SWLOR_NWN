using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Damage a trigger deals (a reflection, a proc, a splash or a retaliation): the outgoing modifiers, the target's adjustment
    /// and resistance, the damage taken modifiers and the damage effect.
    /// </summary>
    public interface ICombatTriggeredDamageService
    {
        int ApplyTriggeredDamage(
            uint activator,
            uint target,
            int damage,
            CombatDamageType damageType,
            SkillType skillType = SkillType.Invalid,
            bool typedLeadershipReductionAlreadyApplied = false,
            int? targetStatusDamagePercentAdjustment = null,
            bool outgoingModifiersAlreadyApplied = false);
    }
}
