using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        public static int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            var criticalRate = GetAbilityCriticalRate(
                attacker,
                skillType,
                isAreaAbility,
                criticalRateAdjustment,
                defender);

            return criticalRate > 0 && Random.D100(1) <= criticalRate
                ? StandardCriticalRating
                : 0;
        }

        private static void ApplyGuardedHitNextAttackEffects(uint creature)
        {
            var primaryDMGBonus = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackDMGBonus);
            var criticalRate = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackCriticalRatePercentAdjustment);
            var primaryWindow = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitNextAttackWindowSeconds);
            var secondaryDMGBonus = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackDMGBonus);
            var enmityBonus = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackEnmityBonus);
            var secondaryWindow = Stat.GetStatAdjustment(
                creature,
                StatType.GuardedHitSecondaryNextAttackWindowSeconds);
            var dmgBonus = primaryDMGBonus + secondaryDMGBonus;
            var window = Math.Max(primaryWindow, secondaryWindow);
            if (window <= 0 || dmgBonus == 0 && criticalRate == 0 && enmityBonus == 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitDMGBonus,
                dmgBonus,
                window,
                StatType.NextAttackGuardedHitDMGBonus);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitCriticalRatePercentAdjustment,
                criticalRate,
                window,
                StatType.NextAttackGuardedHitDMGBonus);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackGuardedHitEnmityBonus,
                enmityBonus,
                window,
                StatType.NextAttackGuardedHitDMGBonus);

            if (GetIsPC(creature))
            {
                var criticalText = criticalRate != 0
                    ? $", +{criticalRate}% Crit"
                    : string.Empty;
                var enmityText = enmityBonus != 0
                    ? $", +{enmityBonus} Enmity"
                    : string.Empty;
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Counter Ready: +{dmgBonus} DMG{criticalText}{enmityText}"),
                    creature,
                    false);
            }
        }

        public static int GetAbilityCriticalRate(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalAdjustment = criticalRateAdjustment;
            totalAdjustment += GetSkillCriticalRatePercentAdjustment(attacker, skillType);
            totalAdjustment += GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                PerkType.Invalid,
                StatType.AbilityCriticalRatePercentAdjustmentSkillType,
                StatType.AbilityCriticalRatePercentAdjustmentPerkType,
                StatType.AbilityCriticalRatePercentAdjustmentSecondaryPerkType,
                StatType.AbilityCriticalRatePercentAdjustment,
                false,
                defender);

            if (isAreaAbility && skillType == SkillType.TwinBlade)
            {
                totalAdjustment += Stat.GetStatAdjustment(attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment);
            }

            if (skillType == SkillType.Throwing &&
                GetIsObjectValid(defender) &&
                (StatusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect)) ||
                 StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding)))
            {
                totalAdjustment += Stat.GetStatAdjustment(attacker, StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment);
            }

            if (GetIsObjectValid(defender) && IsTargetNotFacingAttacker(attacker, defender))
            {
                totalAdjustment += Stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment);
            }

            totalAdjustment += GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender);
            totalAdjustment += GetTargetStatusCriticalRateAdjustment(attacker, defender);
            totalAdjustment += GetSideAttackCriticalRateAdjustment(attacker, defender, skillType);
            totalAdjustment += GetBackAttackCriticalRateAdjustment(attacker, defender, skillType);

            return CalculateAbilityCriticalChance(totalAdjustment);
        }

        private static bool? _abilityHitResolutionOverride;

        /// <summary>
        /// Forces every TryResolveAbilityHit call to the given outcome instead of rolling.
        /// Intended solely for the in-engine test harness: ability behavior assertions cannot
        /// be made against a hit roll that legitimately misses up to 5% of the time even at
        /// capped hit rates. Pass null to restore normal resolution. Always restore in a
        /// finally block.
        /// </summary>
        public static void SetAbilityHitResolutionOverride(bool? forcedOutcome)
        {
            _abilityHitResolutionOverride = forcedOutcome;
        }

        private static bool? _autoAttackHitResolutionOverride;

        /// <summary>
        /// Forces every native auto-attack roll (ResolveAttackRoll hook) to the given outcome
        /// instead of rolling. Intended solely for the in-engine test harness: ability damage
        /// assertions cannot distinguish ability damage from the activator's resumed
        /// auto-attacks, so behavior sweeps force auto-attack misses. Pass null to restore
        /// normal resolution. Always restore in a finally block.
        /// </summary>
        public static void SetAutoAttackHitResolutionOverride(bool? forcedOutcome)
        {
            _autoAttackHitResolutionOverride = forcedOutcome;
        }

        /// <summary>
        /// The current auto-attack resolution override, if any. Read by the native
        /// ResolveAttackRoll hook.
        /// </summary>
        public static bool? GetAutoAttackHitResolutionOverride()
        {
            return _autoAttackHitResolutionOverride;
        }

        public static bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate,
            int hitChancePercentAdjustment = 0,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid,
            int accuracyFlatAdjustment = 0)
        {
            hitRate = 100;
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return true;

            var accuracy = GetAbilityAccuracy(attacker, defender, skillType, skillLevelOverride, statOverride) + accuracyFlatAdjustment;
            var evasion = Stat.GetEvasion(defender, SkillType.Invalid, skillType);
            evasion = ApplySideAttackEvasionIgnore(attacker, defender, skillType, evasion);

            var modifier = hitChancePercentAdjustment + GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.AbilityHitChancePercentAdjustment,
                false,
                defender);
            modifier += GetTargetedAbilityAdjustment(
                attacker,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.TargetedAbilityHitChancePercentAdjustment);
            modifier += GetPhysicalAndForceAbilityHitChanceAdjustment(attacker, skillType);
            modifier += GetIncomingAbilityHitChanceAdjustment(defender, skillType);
            modifier += GetSideAttackHitChanceAdjustment(attacker, defender, skillType);
            modifier += GetIdleAbilityHitChanceAdjustment(attacker, skillType);
            modifier += ConsumeSuppressionRangedAttackAccuracyAdjustment(attacker, defender, skillType);
            modifier += GetHitChanceAgainstSunderedTargetAdjustment(attacker, defender);
            if (skillType == SkillType.Force)
            {
                modifier += Perk.GetForceAffinityHitChanceAdjustment(attacker, perkType);
            }

            hitRate = CalculateHitRate(accuracy, evasion, modifier, GetMinimumHitRate(attacker));
            var isHit = _abilityHitResolutionOverride ?? Random.D100(1) <= hitRate;
            if (!isHit && GetIsReactionTypeHostile(attacker, defender))
                PairedRangedTraits.RecordAccuracyEvasionMiss(defender);
            if (!isHit && skillType == SkillType.Force)
            {
                ApplyForceAbilityEvadedEffects(defender);
            }

            return isHit;
        }

        private static int GetAbilityAccuracy(
            uint attacker,
            uint defender,
            SkillType skillType,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid)
        {
            if (WeaponImpactProfile.Get(attacker) is { } explicitProfile)
                return ApplyStatusSourceAccuracyModifiers(attacker, defender,
                    explicitProfile.Weapon.Accuracy + explicitProfile.AccuracyAdjustment + Ability.GetWeaponTraitAccuracy(attacker));
            var weapon = GetRelevantSkillWeapon(attacker, skillType);
            var usesForceAccuracy = skillType == SkillType.Force;
            var accuracyStatOverride = usesForceAccuracy ? AbilityType.Willpower : statOverride;
            var accuracy = Stat.GetAccuracy(
                attacker,
                weapon,
                accuracyStatOverride,
                skillType,
                skillLevelOverride,
                ignoreWeaponAccuracyStatOverride: usesForceAccuracy);
            return ApplyStatusSourceAccuracyModifiers(attacker, defender, accuracy + Ability.GetWeaponTraitAccuracy(attacker));
        }

        private static int GetAbilityHitOrCriticalAdjustment(
            uint creature,
            SkillType skillType,
            PerkType perkType,
            StatType skillTypeStat,
            StatType primaryPerkStat,
            StatType secondaryPerkStat,
            StatType adjustmentStat,
            bool includePerkTargeting,
            uint defender = OBJECT_INVALID)
        {
            // Each source carries its own skill selector alongside its own magnitude. Reading the
            // aggregated selector would sum unrelated skill ids (Force 5 + Force 5 becomes
            // Fabrication 10, Pistol 45 + Devices 33 becomes nothing at all), which silently
            // disables every contributing bonus. Resolve per source instead, and honour the
            // selectorless sources that are authored to affect every ability.
            var adjustment = GetSkillSelectedStatAdjustment(
                creature,
                skillType,
                skillTypeStat,
                adjustmentStat,
                unselectedSourcesApplyToEverySkill: true);

            // Long-range bonuses are independent of the generic skill-selector stats. This
            // allows a perk to affect every ranged ability without installing a second selector.
            if (adjustmentStat == StatType.AbilityHitChancePercentAdjustment)
                adjustment += GetRangedAbilityLongRangeHitChanceAdjustment(creature, defender, skillType);
            else if (adjustmentStat == StatType.AbilityCriticalRatePercentAdjustment)
                adjustment += GetRangedAbilityLongRangeCriticalRateAdjustment(creature, defender, skillType);

            if (includePerkTargeting)
            {
                adjustment += GetTargetedAbilityAdjustment(
                    creature,
                    perkType,
                    primaryPerkStat,
                    secondaryPerkStat,
                    adjustmentStat);
            }

            return adjustment;
        }

        public static void ApplyRangedAbilityTargetDefenseReduction(
            uint attacker,
            uint defender,
            SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                !IsRangedWeaponSkill(skillType))
            {
                return;
            }

            var reduction = Stat.GetStatAdjustment(attacker, StatType.RangedAbilityTargetDefenseReductionPercent);
            var duration = Stat.GetStatAdjustment(attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds);
            if (reduction <= 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                defender,
                StatType.PhysicalDefensePercentAdjustment,
                -reduction,
                duration,
                StatType.RangedAbilityTargetDefenseReductionPercent);
        }

        private static int GetIncomingAbilityHitChanceAdjustment(uint defender, SkillType skillType)
        {
            return GetSkillSelectedStatAdjustment(
                defender,
                skillType,
                StatType.IncomingAbilityHitChancePercentAdjustmentSkillType,
                StatType.IncomingAbilityHitChancePercentAdjustment,
                unselectedSourcesApplyToEverySkill: true);
        }

        public static int GetHitChanceAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? Stat.GetStatAdjustment(attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        public static int GetCriticalRateAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? Stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        private static void ApplyForceAbilityEvadedEffects(uint defender)
        {
            var forceDefense = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedDurationSeconds);
            var staminaRestore = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedCooldownSeconds);
            if (forceDefense == 0 && staminaRestore <= 0 ||
                duration <= 0 ||
                !TryUseStatTrigger(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, cooldown))
                return;

            if (forceDefense != 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            }

            if (staminaRestore > 0)
            {
                RestoreStaminaFromStat(defender, StatType.ForceAbilityEvadedStaminaRestore, staminaRestore);
            }
        }
    }
}
