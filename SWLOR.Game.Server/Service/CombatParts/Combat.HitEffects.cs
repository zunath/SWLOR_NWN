using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    // The hit effects live in Domain/Combat/CombatHitEffectsService.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
        private static ICombatHitEffectsService HitEffects => FacadeAccessor.Resolve<ICombatHitEffectsService>();

        public static int ApplyAutoAttackDamageModifiers(uint attacker, uint defender, int damage, SkillType skillType = SkillType.Invalid) =>
            HitEffects.ApplyAutoAttackDamageModifiers(attacker, defender, damage, skillType);

        public static void ConsumeSuppressedAutoAttackDamageBonuses(uint attacker, SkillType skillType) =>
            HitEffects.ConsumeSuppressedAutoAttackDamageBonuses(attacker, skillType);

        /// <inheritdoc cref="ICombatHitEffectsService.ConsumeAutoAttackCycleDamageBonus"/>
        public static int ConsumeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType) =>
            HitEffects.ConsumeAutoAttackCycleDamageBonus(attacker, skillType);

        public static void ApplyDamageDealtEffects(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            bool isAbilityDamage = false,
            bool allowsHitProcs = true) =>
            HitEffects.ApplyDamageDealtEffects(attacker, defender, damage, skillType, damageType, deliveryType, isAbilityDamage, allowsHitProcs);

        public static void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid) =>
            HitEffects.ApplyCriticalHitEffects(attacker, defender, damage, criticalRating, isSingleTargetImpact, skillType);

        public static void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType) =>
            HitEffects.ApplyNonCriticalAbilityEffects(activator, target, skillType);

        public static void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID) =>
            HitEffects.ApplyDefeatedEnemyEffects(creature, defeated);

        private static void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated) => HitEffects.ApplyRecentDamagerDefeatedEnemyEffects(defeated);

        private static void ApplySourceStatusStackEffects(uint attacker, uint defender) =>
            HitEffects.ApplySourceStatusStackEffects(attacker, defender);

        private static void SpreadPoisonFromTarget(uint activator, uint target) => HitEffects.SpreadPoisonFromTarget(activator, target);

        public static int ApplyTriggeredDamage(
            uint activator,
            uint target,
            int damage,
            CombatDamageType damageType,
            SkillType skillType = SkillType.Invalid,
            bool typedLeadershipReductionAlreadyApplied = false,
            int? targetStatusDamagePercentAdjustment = null,
            bool outgoingModifiersAlreadyApplied = false) =>
            HitEffects.ApplyTriggeredDamage(activator, target, damage, damageType, skillType, typedLeadershipReductionAlreadyApplied, targetStatusDamagePercentAdjustment, outgoingModifiersAlreadyApplied);

        private static void ForgetHitEffectState(uint creature) => HitEffects.ForgetHitEffectState(creature);
    }
}
