using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Damage roll modifiers for combat: the critical roll and its mitigation, the outgoing and target-status
    /// modifiers of a damage roll, the attack and defense adjustments around it, and the per-attacker damage stacks.
    /// </summary>
    public interface ICombatDamageModifiersService
    {
        (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(
            uint defender,
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0);

        bool TryUseIncomingCriticalHitDowngrade(uint defender, int criticalRating);

        int ApplyCriticalDamageModifier(
            uint attacker,
            int damage,
            int criticalRating,
            SkillType skillType = SkillType.Invalid,
            uint defender = OBJECT_INVALID,
            int criticalDamagePercentAdjustment = 0);

        int GetSkillCriticalRatePercentAdjustment(uint attacker, SkillType skillType);

        int GetAutoAttackCriticalRateAdjustment(
            uint attacker,
            uint defender,
            int cycleCriticalRateAdjustment);

        int ApplyRangedAttackDefenseIgnore(uint attacker, int defense, SkillType skillType);

        int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType);

        /// <summary>
        /// Rejects immune hits, then applies outgoing ability, skill, damage-type, and target
        /// modifiers to a damage roll while enforcing the shared percentage bonus cap.
        /// </summary>
        int ApplyDamageDealtModifiers(
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
            bool isOrdinaryWeaponAttack = false);

        /// <summary>Applies ordinary-attack adjustments after outgoing bonuses and before mitigation.</summary>
        int ApplyOrdinaryAttackDamageMultiplier(uint attacker, int damage);

        int ApplyAbilityShapeDamageModifier(uint attacker, int damage, bool isAbilityDamage, AbilityDetail ability);

        /// <summary>
        /// Applies ThrowingSingleTargetDamagePercentAdjustment to Throwing auto-attacks and
        /// single-target hostile Throwing abilities, the Throwing-only form of the single-target
        /// scope used by SingleTargetDamagePercentAdjustment.
        /// </summary>
        int ApplyThrowingSingleTargetDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            AbilityDetail ability);

        /// <summary>
        /// Applies analyzer potency to the complete technique damage roll, including attribute
        /// scaling. All direct impacts, including custom areas and secondary arcs, pass here once.
        /// </summary>
        int ApplyMimicryAbilityDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            bool isAbilityDamage);

        int ApplySkillAbilityDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            bool isAbilityDamage);

        int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            bool isAbilityDamage = false,
            bool canApplyRandomFlatBonuses = true,
            bool isLandedAttack = true,
            AbilityDetail ability = null);

        int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage);

        int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType);

        int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType);

        int ApplyBackAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage);

        int GetBackAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType);

        int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion);

        int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType);

        void ClearAutoAttackCycleCriticalRateTracker(uint creature);

        int GetLowHPAttackAdjustment(uint attacker);

        int GetLowFPAttackAdjustment(uint attacker);

        int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender);

        int ApplyDamageTypeDealtModifiers(uint attacker, int damage, CombatDamageType damageType);

        int GetNearbyStatusTargetAttackAdjustment(uint creature);

        int ApplyTwinBladeAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isSingleTargetAbility,
            bool isAreaAbility);

        int ApplyThrowingAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility);

        int ApplyPhysicalAbilityShapeDamageModifier(
            uint attacker,
            CombatDamageType damageType,
            int damage,
            bool isSingleTargetAbility);

        int ApplySkillAreaAbilityDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility);

        int ApplyRangedAbilityLongRangeDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage);

        int ApplyAreaAbilityAfterDeflectionDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility);

        void ClearRangedRepeatedTargetDamageTracker(uint creature);

        void ApplySameTargetPressureDamageEffects(uint attacker, uint defender, SkillType skillType);

        int GetSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType);

        void ConsumeSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType,
            int damage);

        int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense);

        int ApplyStatusSourceAccuracyModifiers(uint attacker, uint defender, int accuracy);

        void ForgetDamageModifierState(uint creature);
    }
}
