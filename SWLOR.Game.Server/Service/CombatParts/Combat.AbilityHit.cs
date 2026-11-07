using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    // The ability hit roll lives in Domain/Combat/CombatAbilityHitService.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
        private static ICombatAbilityHitService AbilityHit => FacadeAccessor.Resolve<ICombatAbilityHitService>();

        public static int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID) =>
            AbilityHit.CalculateAbilityCriticalRating(attacker, skillType, isAreaAbility, criticalRateAdjustment, defender);

        private static void ApplyGuardedHitNextAttackEffects(uint creature) => AbilityHit.ApplyGuardedHitNextAttackEffects(creature);

        public static int GetAbilityCriticalRate(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID) =>
            AbilityHit.GetAbilityCriticalRate(attacker, skillType, isAreaAbility, criticalRateAdjustment, defender);

        /// <inheritdoc cref="ICombatAbilityHitService.SetAbilityHitResolutionOverride"/>
        public static void SetAbilityHitResolutionOverride(bool? forcedOutcome) => AbilityHit.SetAbilityHitResolutionOverride(forcedOutcome);

        /// <inheritdoc cref="ICombatAbilityHitService.SetAutoAttackHitResolutionOverride"/>
        public static void SetAutoAttackHitResolutionOverride(bool? forcedOutcome) => AbilityHit.SetAutoAttackHitResolutionOverride(forcedOutcome);

        /// <inheritdoc cref="ICombatAbilityHitService.GetAutoAttackHitResolutionOverride"/>
        public static bool? GetAutoAttackHitResolutionOverride() => AbilityHit.GetAutoAttackHitResolutionOverride();

        public static bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate,
            int hitChancePercentAdjustment = 0,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid,
            int accuracyFlatAdjustment = 0) =>
            AbilityHit.TryResolveAbilityHit(attacker, defender, skillType, perkType, out hitRate, hitChancePercentAdjustment, skillLevelOverride, statOverride, accuracyFlatAdjustment);

        public static void ApplyRangedAbilityTargetDefenseReduction(uint attacker, uint defender, SkillType skillType) =>
            AbilityHit.ApplyRangedAbilityTargetDefenseReduction(attacker, defender, skillType);

        public static int GetHitChanceAgainstSunderedTargetAdjustment(uint attacker, uint defender) =>
            AbilityHit.GetHitChanceAgainstSunderedTargetAdjustment(attacker, defender);

        public static int GetCriticalRateAgainstSunderedTargetAdjustment(uint attacker, uint defender) =>
            AbilityHit.GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender);
    }
}
