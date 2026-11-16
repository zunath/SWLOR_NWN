using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The per-target resolution of a hostile ability impact (part 11 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md): the hit roll (<see cref="IAbilityHitRollService"/>), then the hit's base
    /// damage and damage formula, then the hit itself (<see cref="IAbilityHostileImpactService"/>), all inside one damage-derived
    /// healing scope. <see cref="IAbilityService"/> calls it for each target of an impact.
    /// </summary>
    public interface IAbilityTargetImpactService
    {
        /// <summary>
        /// Resolves one target of a hostile impact and returns the damage it dealt: 0 when the impact may not hit the target or
        /// misses.
        /// </summary>
        int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false);
    }
}
