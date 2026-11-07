using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    // The damage roll modifiers live in Domain/Combat/CombatDamageModifiersService.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
        private static ICombatDamageModifiersService DamageModifiers => FacadeAccessor.Resolve<ICombatDamageModifiersService>();

        public static (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(
            uint defender,
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0) =>
            DamageModifiers.CalculateDamageWithCriticalMitigation(defender, attackerAttack, attackerDMG, attackerStat, defenderDefense, defenderStat, critical, deltaCap);

        internal static bool TryUseIncomingCriticalHitDowngrade(uint defender, int criticalRating) =>
            DamageModifiers.TryUseIncomingCriticalHitDowngrade(defender, criticalRating);

        public static int ApplyCriticalDamageModifier(
            uint attacker,
            int damage,
            int criticalRating,
            SkillType skillType = SkillType.Invalid,
            uint defender = OBJECT_INVALID,
            int criticalDamagePercentAdjustment = 0) =>
            DamageModifiers.ApplyCriticalDamageModifier(attacker, damage, criticalRating, skillType, defender, criticalDamagePercentAdjustment);

        public static int GetSkillCriticalRatePercentAdjustment(uint attacker, SkillType skillType) =>
            DamageModifiers.GetSkillCriticalRatePercentAdjustment(attacker, skillType);

        public static int GetAutoAttackCriticalRateAdjustment(uint attacker, uint defender, int cycleCriticalRateAdjustment) =>
            DamageModifiers.GetAutoAttackCriticalRateAdjustment(attacker, defender, cycleCriticalRateAdjustment);

        public static int ApplyRangedAttackDefenseIgnore(uint attacker, int defense, SkillType skillType) =>
            DamageModifiers.ApplyRangedAttackDefenseIgnore(attacker, defense, skillType);

        public static int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType) =>
            DamageModifiers.ApplyTargetStatusAttackModifiers(attacker, defender, attack, skillType);

        /// <inheritdoc cref="ICombatDamageModifiersService.ApplyDamageDealtModifiers(uint, uint, int, SkillType, CombatDamageType, bool, bool, bool, AbilityDetail, out int, int, int, bool)"/>
        public static int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            bool isAbilityDamage,
            bool canApplyRandomFlatBonuses,
            bool isLandedAttack,
            AbilityDetail ability,
            out int targetStatusDamagePercentAdjustment,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0,
            bool isOrdinaryWeaponAttack = false) =>
            DamageModifiers.ApplyDamageDealtModifiers(attacker, defender, damage, skillType, damageType, isAbilityDamage, canApplyRandomFlatBonuses, isLandedAttack, ability, out targetStatusDamagePercentAdjustment, abilityDamagePercentAdjustment, lowHPAbilityDamagePercentAdjustment, isOrdinaryWeaponAttack);

        /// <inheritdoc cref="ICombatDamageModifiersService.ApplyOrdinaryAttackDamageMultiplier"/>
        public static int ApplyOrdinaryAttackDamageMultiplier(uint attacker, int damage) =>
            DamageModifiers.ApplyOrdinaryAttackDamageMultiplier(attacker, damage);

        public static int ApplyAbilityShapeDamageModifier(uint attacker, int damage, bool isAbilityDamage, AbilityDetail ability) =>
            DamageModifiers.ApplyAbilityShapeDamageModifier(attacker, damage, isAbilityDamage, ability);

        /// <inheritdoc cref="ICombatDamageModifiersService.ApplyThrowingSingleTargetDamageModifier"/>
        public static int ApplyThrowingSingleTargetDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            AbilityDetail ability) =>
            DamageModifiers.ApplyThrowingSingleTargetDamageModifier(attacker, skillType, damage, isAbilityDamage, ability);

        /// <inheritdoc cref="ICombatDamageModifiersService.ApplyMimicryAbilityDamageModifier"/>
        public static int ApplyMimicryAbilityDamageModifier(uint attacker, int damage, SkillType skillType, bool isAbilityDamage) =>
            DamageModifiers.ApplyMimicryAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);

        public static int ApplySkillAbilityDamageModifier(uint attacker, int damage, SkillType skillType, bool isAbilityDamage) =>
            DamageModifiers.ApplySkillAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);

        public static int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            bool isAbilityDamage = false,
            bool canApplyRandomFlatBonuses = true,
            bool isLandedAttack = true,
            AbilityDetail ability = null) =>
            DamageModifiers.ApplyDamageDealtModifiers(attacker, defender, damage, skillType, damageType, isAbilityDamage, canApplyRandomFlatBonuses, isLandedAttack, ability);

        public static int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage) =>
            DamageModifiers.ApplySideAttackDamageModifier(attacker, defender, skillType, damage);

        public static int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType) =>
            DamageModifiers.GetSideAttackHitChanceAdjustment(attacker, defender, skillType);

        public static int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType) =>
            DamageModifiers.GetSideAttackCriticalRateAdjustment(attacker, defender, skillType);

        public static int ApplyBackAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage) =>
            DamageModifiers.ApplyBackAttackDamageModifier(attacker, defender, skillType, damage);

        public static int GetBackAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType) =>
            DamageModifiers.GetBackAttackCriticalRateAdjustment(attacker, defender, skillType);

        public static int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion) =>
            DamageModifiers.ApplySideAttackEvasionIgnore(attacker, defender, skillType, evasion);

        public static int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType) =>
            DamageModifiers.PrepareAutoAttackCycleCriticalRate(attacker, skillType);

        private static void ClearAutoAttackCycleCriticalRateTracker(uint creature) =>
            DamageModifiers.ClearAutoAttackCycleCriticalRateTracker(creature);

        public static int GetLowHPAttackAdjustment(uint attacker) => DamageModifiers.GetLowHPAttackAdjustment(attacker);

        public static int GetLowFPAttackAdjustment(uint attacker) => DamageModifiers.GetLowFPAttackAdjustment(attacker);

        private static int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender) =>
            DamageModifiers.GetTargetStatusCriticalRateAdjustment(attacker, defender);

        public static int ApplyDamageTypeDealtModifiers(uint attacker, int damage, CombatDamageType damageType) =>
            DamageModifiers.ApplyDamageTypeDealtModifiers(attacker, damage, damageType);

        public static int GetNearbyStatusTargetAttackAdjustment(uint creature) => DamageModifiers.GetNearbyStatusTargetAttackAdjustment(creature);

        public static int ApplyTwinBladeAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isSingleTargetAbility,
            bool isAreaAbility) =>
            DamageModifiers.ApplyTwinBladeAbilityShapeDamageModifier(attacker, skillType, damage, isSingleTargetAbility, isAreaAbility);

        public static int ApplyThrowingAbilityShapeDamageModifier(uint attacker, SkillType skillType, int damage, bool isAreaAbility) =>
            DamageModifiers.ApplyThrowingAbilityShapeDamageModifier(attacker, skillType, damage, isAreaAbility);

        public static int ApplyPhysicalAbilityShapeDamageModifier(
            uint attacker,
            CombatDamageType damageType,
            int damage,
            bool isSingleTargetAbility) =>
            DamageModifiers.ApplyPhysicalAbilityShapeDamageModifier(attacker, damageType, damage, isSingleTargetAbility);

        public static int ApplySkillAreaAbilityDamageModifier(uint attacker, SkillType skillType, int damage, bool isAreaAbility) =>
            DamageModifiers.ApplySkillAreaAbilityDamageModifier(attacker, skillType, damage, isAreaAbility);

        public static int ApplyRangedAbilityLongRangeDamageModifier(uint attacker, uint defender, SkillType skillType, int damage) =>
            DamageModifiers.ApplyRangedAbilityLongRangeDamageModifier(attacker, defender, skillType, damage);

        public static int ApplyAreaAbilityAfterDeflectionDamageModifier(uint attacker, SkillType skillType, int damage, bool isAreaAbility) =>
            DamageModifiers.ApplyAreaAbilityAfterDeflectionDamageModifier(attacker, skillType, damage, isAreaAbility);

        private static void ClearRangedRepeatedTargetDamageTracker(uint creature) => DamageModifiers.ClearRangedRepeatedTargetDamageTracker(creature);

        private static void ApplySameTargetPressureDamageEffects(uint attacker, uint defender, SkillType skillType) =>
            DamageModifiers.ApplySameTargetPressureDamageEffects(attacker, defender, skillType);

        public static int GetSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType) =>
            DamageModifiers.GetSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType);

        public static void ConsumeSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType, int damage) =>
            DamageModifiers.ConsumeSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType, damage);

        public static int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense) =>
            DamageModifiers.ApplyStatusSourceDefenseModifiers(attacker, defender, defense);

        public static int ApplyStatusSourceAccuracyModifiers(uint attacker, uint defender, int accuracy) =>
            DamageModifiers.ApplyStatusSourceAccuracyModifiers(attacker, defender, accuracy);

        private static void ForgetDamageModifierState(uint creature) => DamageModifiers.ForgetDamageModifierState(creature);
    }
}
