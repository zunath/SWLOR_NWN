using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The orchestrator of a direct hit and the auto-attack cycle damage. The steps a landed hit grants the attacker are
    /// <see cref="ICombatDealtEffectsService"/>'s, what an auto-attack adds is <see cref="ICombatAutoAttackEffectsService"/>'s, a critical
    /// hit is <see cref="ICombatCriticalHitEffectsService"/>'s, a kill is <see cref="ICombatDefeatedEnemyEffectsService"/>'s, and triggered
    /// damage is <see cref="ICombatTriggeredDamageService"/>'s.
    /// </summary>
    public interface ICombatHitEffectsService
    {
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

        /// <summary>
        /// The cycle damage of a radius cycle perk (Edge Rhythm): the counter advance and the triggered hit on a nearby enemy.
        /// </summary>
        void ApplyAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType);

        void ApplySourceStatusAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType);

        void ForgetHitEffectState(uint creature);
    }
}
