using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The hit effects for combat: the riders of an auto-attack, of landed damage, of a critical and of a non-critical ability,
    /// and the effects of defeating an enemy. The steps a landed hit grants the attacker are
    /// <see cref="ICombatDealtEffectsService"/>'s, and triggered damage is <see cref="ICombatTriggeredDamageService"/>'s.
    /// </summary>
    public interface ICombatHitEffectsService
    {
        int ApplyAutoAttackDamageModifiers(uint attacker, uint defender, int damage, SkillType skillType = SkillType.Invalid);

        void ConsumeSuppressedAutoAttackDamageBonuses(uint attacker, SkillType skillType);

        /// <summary>
        /// Advances the auto-attack cycle counter for cycle perks with no radius (Follow-Through) and,
        /// on the attack that completes the cycle, returns the cycle bonus as DMG so it feeds the
        /// standard attack damage formula and scales with the attacker's stats. Radius-based cycle
        /// perks (Edge Rhythm) hit a nearby enemy instead and are handled after the damage roll by
        /// ApplyAutoAttackCycleDamage.
        /// </summary>
        int ConsumeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType);

        void ApplyDamageDealtEffects(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            bool isAbilityDamage = false,
            bool allowsHitProcs = true);

        void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid);

        void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType);

        void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID);

        void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated);

        void ApplySourceStatusStackEffects(uint attacker, uint defender);

        void SpreadPoisonFromTarget(uint activator, uint target);

        void ForgetHitEffectState(uint creature);
    }
}
