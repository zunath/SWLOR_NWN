using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The hit roll of one hostile ability hit (part 10 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md):
    /// whether the impact may hit the target, the activity and battle theme it starts, the roll itself and what a miss does.
    /// The per-target resolution (<see cref="IAbilityTargetImpactService"/>) calls it before it computes the damage.
    /// </summary>
    public interface IAbilityHitRollService
    {
        /// <summary>
        /// Runs the hit roll of one hit, in the order the resolution always ran it: the melee technique's validity check (false
        /// with nothing done), the weapon damage type, the activity and battle theme, then the roll with its result message. A
        /// miss sends its message, awards the miss combat point and applies the miss enmity, and returns false. On a landed or
        /// unrolled hit, <paramref name="hit"/> holds what the damage needs.
        /// </summary>
        bool TryResolveHostileImpactHit(
            uint activator,
            uint target,
            SkillType skillType,
            CombatDamageType damageType,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            int hitChancePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            AbilityType combatImpactDamageAbility,
            bool resolvesHit,
            bool useUnscaledDamage,
            AbilityImpactTracker trackedImpact,
            out AbilityHitRoll hit);
    }
}
