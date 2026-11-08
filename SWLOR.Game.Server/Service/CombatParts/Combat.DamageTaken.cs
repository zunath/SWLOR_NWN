using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    // The damage taken part lives in Domain/Combat/CombatDamageTakenService.cs. These forwarders keep every member on Combat for
    // its callers, and the damage handler stays here under its name.
    public static partial class Combat
    {
        private static ICombatDamageTakenService DamageTaken => FacadeAccessor.Resolve<ICombatDamageTakenService>();

        public static int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            int targetStatusDamagePercentAdjustment = 0,
            bool isLandedAttack = true,
            bool typedLeadershipReductionAlreadyApplied = false) =>
            DamageTaken.ApplyDamageTakenModifiers(defender, damage, attacker, damageType, deliveryType, targetStatusDamagePercentAdjustment, isLandedAttack, typedLeadershipReductionAlreadyApplied);

        public static int ApplyTypedLeadershipDamageTakenModifier(uint defender, int damage, CombatDamageType damageType) =>
            DamageTaken.ApplyTypedLeadershipDamageTakenModifier(defender, damage, damageType);

        public static void ApplyDamageReflectionEffects(uint attacker, uint defender, int damage, CombatDamageType damageType) =>
            DamageTaken.ApplyDamageReflectionEffects(attacker, defender, damage, damageType);

        public static int ApplyDamageOverTimeTakenModifiers(uint defender, int damage, CombatDamageType damageType) =>
            DamageTaken.ApplyDamageOverTimeTakenModifiers(defender, damage, damageType);

        public static int ApplyDamageOverTimeTakenModifiers(
            uint defender,
            int damage,
            CombatDamageType damageType,
            out int targetStatusDamagePercentAdjustment) =>
            DamageTaken.ApplyDamageOverTimeTakenModifiers(defender, damage, damageType, out targetStatusDamagePercentAdjustment);

        public static bool TryPreventFatalDamageAndGrantTemporaryHP(uint defender, int damage, bool restoreToOneHP) =>
            DamageTaken.TryPreventFatalDamageAndGrantTemporaryHP(defender, damage, restoreToOneHP);

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void ApplyDamageTakenEffects() => DamageTaken.ApplyDamageTakenEffects();

        public static void ApplyDamageTakenEffects(uint defender, uint attacker, int damage) =>
            DamageTaken.ApplyDamageTakenEffects(defender, attacker, damage);

        public static void ApplyLowHPDamageTakenEffects(uint defender, int damage) => DamageTaken.ApplyLowHPDamageTakenEffects(defender, damage);

        public static void ApplyLowHPGuardEffectFromProtectedTarget(uint guardRecipient, uint protectedTarget, int damage) =>
            DamageTaken.ApplyLowHPGuardEffectFromProtectedTarget(guardRecipient, protectedTarget, damage);

        public static void TrackGuardedHit(uint creature) => DamageTaken.TrackGuardedHit(creature);

        public static void TrackAvoidedAttack(uint creature, uint attacker) => DamageTaken.TrackAvoidedAttack(creature, attacker);

        public static void ApplyMeleeDamageTakenEffects(uint defender, uint attacker) => DamageTaken.ApplyMeleeDamageTakenEffects(defender, attacker);

        public static int ApplyGuardedHitModifiers(uint defender, uint attacker, int damage, CombatDamageType damageType, bool isLandedAttack) =>
            DamageTaken.ApplyGuardedHitModifiers(defender, attacker, damage, damageType, isLandedAttack);

        internal static bool IsHostileAttackSource(uint defender, uint attacker) => DamageTaken.IsHostileAttackSource(defender, attacker);

        /// <inheritdoc cref="ICombatDamageTakenService.GetGuardDamageReductionPercent"/>
        public static int GetGuardDamageReductionPercent(uint defender) => DamageTaken.GetGuardDamageReductionPercent(defender);

        public static bool HasRecentGuardedHit(uint creature, float windowSeconds) => DamageTaken.HasRecentGuardedHit(creature, windowSeconds);

        public static void TrackDeflection(uint creature, DeflectionSource source) => DamageTaken.TrackDeflection(creature, source);

        public static bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds) =>
            DamageTaken.HasRecentDeflection(creature, source, windowSeconds);

        private static int ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(uint creature, SkillType skillType) =>
            DamageTaken.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(creature, skillType);

        /// <inheritdoc cref="ICombatDamageTakenService.ApplyIncomingPhysicalToForceDefenseConversion"/>
        public static int ApplyIncomingPhysicalToForceDefenseConversion(
            uint defender,
            CombatDamageType damageType,
            int physicalDefense,
            Func<int> forceDefenseProvider) =>
            DamageTaken.ApplyIncomingPhysicalToForceDefenseConversion(defender, damageType, physicalDefense, forceDefenseProvider);

        /// <inheritdoc cref="ICombatDamageTakenService.ApplyIncomingPhysicalToForceConversion"/>
        public static int ApplyIncomingPhysicalToForceConversion(
            uint attacker,
            uint defender,
            CombatDamageType damageType,
            ref int physicalDamage,
            int targetStatusDamagePercentAdjustment = 0,
            Action<Action> convertedDamageScheduler = null) =>
            DamageTaken.ApplyIncomingPhysicalToForceConversion(attacker, defender, damageType, ref physicalDamage, targetStatusDamagePercentAdjustment, convertedDamageScheduler);

        /// <inheritdoc cref="ICombatDamageTakenService.ApplyRangedDeflectionReflection"/>
        public static int ApplyRangedDeflectionReflection(uint defender, uint attacker, SkillType attackerWeaponSkill) =>
            DamageTaken.ApplyRangedDeflectionReflection(defender, attacker, attackerWeaponSkill);

        public static string BuildDeflectingReturnCombatLogMessage(uint observer, uint defender, uint attacker, int damage) =>
            DamageTaken.BuildDeflectingReturnCombatLogMessage(observer, defender, attacker, damage);

        private static void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType,
            StatType deflectionFPRestoreStatType = StatType.Invalid) =>
            DamageTaken.ApplyAbilityUsedAttackDeflection(activator, attackDeflectionStatType, durationStatType, deflectionFPRestoreStatType);

        public static void ApplyAbilityGrantedAttackDeflectionEffects(uint activator, DeflectionSource source) =>
            DamageTaken.ApplyAbilityGrantedAttackDeflectionEffects(activator, source);

        private static void ForgetDamageTakenState(uint creature) => DamageTaken.ForgetDamageTakenState(creature);
    }
}
