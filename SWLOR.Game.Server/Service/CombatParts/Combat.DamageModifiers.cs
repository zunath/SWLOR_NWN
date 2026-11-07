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
        private static readonly Dictionary<uint, int> _autoAttackCycleCriticalCounts = new();
        private static readonly Dictionary<uint, RepeatedTargetDamageState> _repeatedTargetDamageStates = new();
        private static readonly Dictionary<uint, RepeatedTargetDamageState> _meleeRepeatedTargetDamageStates = new();
        private static readonly Dictionary<uint, RepeatedTargetDamageState> _rangedRepeatedTargetDamageStates = new();
        private static readonly Dictionary<uint, SameTargetPressureState> _sameTargetPressureStates = new();

        private sealed class SameTargetPressureState
        {
            public uint Target { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime LastBuildHitAt { get; set; }
            public DateTime ReadyUntil { get; set; }
        }

        public static (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(
            uint defender,
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var isDefenderValid = GetIsObjectValid(defender);
            var usedPendingCriticalDowngrade = isDefenderValid &&
                TemporaryStatModifier.Consume(
                    defender,
                    StatType.CurrentIncomingAttackMinimumDamage,
                    StatType.CurrentIncomingAttackMinimumDamage) > 0;
            var wasCriticalDowngraded = !usedPendingCriticalDowngrade &&
                TryUseIncomingCriticalHitDowngrade(defender, critical);
            var forceMinimumNormalDamage = wasCriticalDowngraded || usedPendingCriticalDowngrade;
            var effectiveCritical = forceMinimumNormalDamage ? 0 : critical;
            var (minDamage, maxDamage) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                effectiveCritical,
                deltaCap);
            var damage = forceMinimumNormalDamage
                ? minDamage
                : (int)Random.NextFloat(minDamage, maxDamage);

            return (damage, effectiveCritical, wasCriticalDowngraded || usedPendingCriticalDowngrade);
        }

        internal static bool TryUseIncomingCriticalHitDowngrade(uint defender, int criticalRating)
        {
            if (!GetIsObjectValid(defender) ||
                criticalRating <= 0 ||
                Stat.GetStatAdjustment(defender, StatType.IncomingCriticalHitDowngradeToMinimumDamage) <= 0)
            {
                return false;
            }

            var cooldownMilliseconds = Stat.GetStatAdjustment(
                defender,
                StatType.IncomingCriticalHitDowngradeCooldownMilliseconds);
            return TryUseStatTrigger(
                defender,
                StatType.IncomingCriticalHitDowngradeToMinimumDamage,
                TimeSpan.FromMilliseconds(cooldownMilliseconds));
        }

        public static int ApplyCriticalDamageModifier(
            uint attacker,
            int damage,
            int criticalRating,
            SkillType skillType = SkillType.Invalid,
            uint defender = OBJECT_INVALID,
            int criticalDamagePercentAdjustment = 0)
        {
            if (criticalRating <= 0 || damage <= 0)
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamagePercentAdjustment);
            adjustment += GetSkillCriticalDamagePercentAdjustment(attacker, skillType);
            adjustment += GetHighHPTargetCriticalDamageAdjustment(attacker, defender);
            adjustment += GetTargetStatusCriticalDamageAdjustment(attacker, defender);
            adjustment += criticalDamagePercentAdjustment;
            adjustment = ClampCriticalDamagePercentAdjustment(adjustment);
            if (adjustment == 0)
                return damage;

            return damage + damage * adjustment / 100;
        }

        private static int GetHighHPTargetCriticalDamageAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var threshold = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageHighHPTargetThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageHighHPTargetPercentAdjustment);
            var maximumHP = GetMaxHitPoints(defender);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            if (GetCurrentHitPoints(defender) < maximumHP * (threshold / 100f))
                return 0;

            if (GetIsPC(attacker))
            {
                PlayerFeedback.ShowDiagnosticFloatingText(
                    ColorToken.Combat($"High Noon +{adjustment}% critical damage"),
                    attacker,
                    false);
            }

            return adjustment;
        }

        private static int GetTargetStatusCriticalDamageAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.CriticalDamageTargetStatusCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.CriticalDamageTargetStatusPercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

        private static int GetSkillCriticalDamagePercentAdjustment(uint attacker, SkillType skillType)
        {
            var adjustment = IsWeaponSkillType(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.WeaponCriticalDamagePercentAdjustment)
                : 0;
            return adjustment + (skillType switch
            {
                SkillType.Staff => Stat.GetStatAdjustment(attacker, StatType.StaffCriticalDamagePercentAdjustment),
                _ => 0
            });
        }

        public static int GetSkillCriticalRatePercentAdjustment(uint attacker, SkillType skillType)
        {
            var adjustment = skillType switch
            {
                SkillType.Staff => Stat.GetStatAdjustment(attacker, StatType.StaffCriticalRatePercentAdjustment),
                _ => 0
            };

            if (IsWeaponSkillType(skillType))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.WeaponCriticalRatePercentAdjustment);

            adjustment += GetLowHPCriticalRateAdjustment(attacker);
            return adjustment;
        }

        public static int GetAutoAttackCriticalRateAdjustment(
            uint attacker,
            uint defender,
            int cycleCriticalRateAdjustment)
        {
            return GetTargetStatusCriticalRateAdjustment(attacker, defender) +
                   cycleCriticalRateAdjustment;
        }

        public static int ApplyRangedAttackDefenseIgnore(uint attacker, int defense, SkillType skillType)
        {
            return ApplyDefenseIgnore(defense, GetRangedAttackDefenseIgnorePercentAdjustment(attacker, skillType));
        }

        public static int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType)
        {
            if (attack <= 0)
                return attack;

            var adjustment = 0;

            if (skillType == SkillType.Vibroblade &&
                StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.AttackToBleedingTargetPercentAdjustment);
            }

            if (adjustment == 0)
                return attack;

            return Math.Max(0, attack + (int)Math.Ceiling(attack * (adjustment / 100f)));
        }

        /// <summary>
        /// Rejects immune hits, then applies outgoing ability, skill, damage-type, and target
        /// modifiers to a damage roll while enforcing the shared percentage bonus cap.
        /// </summary>
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
            bool isOrdinaryWeaponAttack = false)
        {
            targetStatusDamagePercentAdjustment = 0;

            if (damage <= 0)
                return damage;

            // An authored zero-damage ability must stay zero even when weapon damage,
            // flat bonuses or later percentage bonuses would otherwise produce a hit.
            if (isAbilityDamage && abilityDamagePercentAdjustment <= -100)
                return 0;

            if (HasDamageImmunity(defender, damageType))
                return 0;

            var damageBeforePercentStages = damage;
            damage = ApplySideAttackDamageModifier(attacker, defender, skillType, damage);
            if (!isAbilityDamage && isLandedAttack)
                damage = ApplyBackAttackDamageModifier(attacker, defender, skillType, damage);
            if (isAbilityDamage)
            {
                var isArea = Ability.IsAreaImpact(attacker, ability);
                var isSingleTarget = Ability.IsSingleTargetImpact(attacker, ability);
                damage = ApplyTwinBladeAbilityShapeDamageModifier(attacker, skillType, damage, isSingleTarget, isArea);
                damage = ApplyThrowingAbilityShapeDamageModifier(attacker, skillType, damage, isArea);
                damage = ApplySkillAreaAbilityDamageModifier(attacker, skillType, damage, isArea);
                damage = ApplyPhysicalAbilityShapeDamageModifier(attacker, damageType, damage, isSingleTarget);
                damage = ApplyAreaAbilityAfterDeflectionDamageModifier(attacker, skillType, damage, isArea);
                damage = ApplyRangedAbilityLongRangeDamageModifier(attacker, defender, skillType, damage);
            }

            damage = ApplyMimicryAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);
            damage = ApplySkillAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);
            damage = ApplyOutgoingDamageModifier(attacker, damage);
            damage = ApplyDamageTypeDealtModifiers(attacker, damage, damageType);
            damage = ApplyWeaponAndForceDamageModifier(attacker, damage, skillType, damageType);
            damage = ApplyAbilityShapeDamageModifier(attacker, damage, isAbilityDamage, ability);
            damage = ApplyThrowingSingleTargetDamageModifier(attacker, skillType, damage, isAbilityDamage, ability);
            damage = ApplyTargetLowHPDamageModifier(attacker, defender, damage);

            damage = ApplyTargetStatusDamageModifiers(
                attacker,
                defender,
                damage,
                skillType,
                damageType,
                isAbilityDamage,
                canApplyRandomFlatBonuses,
                ability,
                out var flatDamageBonus,
                out targetStatusDamagePercentAdjustment);
            // The repeated-target modifiers keep per-attacker stack state, so a swing the engine
            // later discards must not advance (or reset) their counters - the damage value itself
            // is thrown away with the swing.
            if (isLandedAttack)
            {
                damage = ApplyRepeatedTargetDamageModifier(attacker, defender, skillType, damage, isAbilityDamage, out var repeatedFlatBonus);
                flatDamageBonus += repeatedFlatBonus;
                flatDamageBonus += ApplyMeleeRepeatedTargetDamageModifier(attacker, defender, skillType, damage, isAbilityDamage) - damage;
                flatDamageBonus += ApplyRangedRepeatedTargetDamageModifier(attacker, defender, skillType, damage) - damage;
            }

            damage = ApplyPercentDamageAdjustment(damage, abilityDamagePercentAdjustment);
            damage = ApplyPercentDamageAdjustment(damage, lowHPAbilityDamagePercentAdjustment);
            damage = CapOutgoingDamageBonus(damageBeforePercentStages, damage, flatDamageBonus, MaximumDamageBonusPercent);
            if (isOrdinaryWeaponAttack && !isAbilityDamage)
                damage = ApplyOrdinaryAttackDamageMultiplier(attacker, damage);
            // Enforce this bucket's share of the combined budget before rounding or independent
            // mitigation. The final stage must never raise already-mitigated damage to recover
            // from an over-budget target reduction.
            targetStatusDamagePercentAdjustment = Math.Max(-MaximumCombinedDamageReductionPercent, targetStatusDamagePercentAdjustment);
            return ApplyPercentDamageAdjustment(damage, targetStatusDamagePercentAdjustment);
        }

        /// <summary>Applies ordinary-attack adjustments after outgoing bonuses and before mitigation.</summary>
        public static int ApplyOrdinaryAttackDamageMultiplier(uint attacker, int damage) =>
            ApplyOrdinaryAttackDamageMultiplier(damage,
                Stat.GetStatAdjustment(attacker, StatType.OrdinaryAttackDamagePercentAdjustment));

        public static int ApplyAbilityShapeDamageModifier(uint attacker, int damage, bool isAbilityDamage, AbilityDetail ability)
        {
            var adjustment = !isAbilityDamage || ability?.IsHostileAbility == true && Ability.IsSingleTargetImpact(attacker, ability)
                ? Stat.GetStatAdjustment(attacker, StatType.SingleTargetDamagePercentAdjustment)
                : ability?.IsHostileAbility == true && Ability.IsAreaImpact(attacker, ability)
                    ? Stat.GetStatAdjustment(attacker, StatType.AreaAbilityDamagePercentAdjustment)
                    : 0;
            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        /// <summary>
        /// Applies ThrowingSingleTargetDamagePercentAdjustment to Throwing auto-attacks and
        /// single-target hostile Throwing abilities, the Throwing-only form of the single-target
        /// scope used by SingleTargetDamagePercentAdjustment.
        /// </summary>
        public static int ApplyThrowingSingleTargetDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            AbilityDetail ability)
        {
            if (damage <= 0 || skillType != SkillType.Throwing)
                return damage;

            var isSingleTarget = !isAbilityDamage ||
                                 ability?.IsHostileAbility == true && Ability.IsSingleTargetImpact(attacker, ability);
            if (!isSingleTarget)
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.ThrowingSingleTargetDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : ApplyPercentDamageAdjustment(damage, adjustment);
        }

        /// <summary>
        /// Applies analyzer potency to the complete technique damage roll, including attribute
        /// scaling. All direct impacts, including custom areas and secondary arcs, pass here once.
        /// </summary>
        public static int ApplyMimicryAbilityDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            bool isAbilityDamage)
        {
            if (damage <= 0 || !isAbilityDamage || skillType != SkillType.Mimicry)
                return damage;

            var potency = Stat.GetStatAdjustment(attacker, StatType.MimicryPotencyPercent);
            return ApplyPercentDamageAdjustment(damage, potency);
        }

        public static int ApplySkillAbilityDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            bool isAbilityDamage)
        {
            if (damage <= 0 || !isAbilityDamage)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAbilityDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.SkillAbilityDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            bool isAbilityDamage = false,
            bool canApplyRandomFlatBonuses = true,
            bool isLandedAttack = true,
            AbilityDetail ability = null)
        {
            return ApplyDamageDealtModifiers(
                attacker,
                defender,
                damage,
                skillType,
                damageType,
                isAbilityDamage,
                canApplyRandomFlatBonuses,
                isLandedAttack,
                ability,
                out _);
        }

        public static int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !IsMatchingSideAttack(attacker, defender, skillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.SideAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        public static int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return IsMatchingSideAttack(attacker, defender, skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.SideAttackHitChancePercentAdjustment)
                : 0;
        }

        public static int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return IsMatchingSideAttack(attacker, defender, skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.SideAttackCriticalRatePercentAdjustment)
                : 0;
        }

        public static int ApplyBackAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !IsMatchingBackAttack(attacker, defender, skillType))
                return damage;

            ApplyBackAttackExposed(attacker, defender);

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.BackAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        // A primed back attack (Ghost Protocol) inflicts Exposed on the landed hit. Both halves of
        // the primer are consumed together so the rider fires exactly once per priming.
        private static void ApplyBackAttackExposed(uint attacker, uint defender)
        {
            var exposedPercent = Stat.GetStatAdjustment(attacker, StatType.BackAttackExposedPercent);
            var exposedDuration = Stat.GetStatAdjustment(attacker, StatType.BackAttackExposedDurationSeconds);
            if (exposedPercent <= 0 || exposedDuration <= 0)
                return;

            TemporaryStatModifier.Consume(attacker, StatType.BackAttackExposedPercent);
            TemporaryStatModifier.Consume(attacker, StatType.BackAttackExposedDurationSeconds);

            // BackAttackExposedPercent is a reduction magnitude (declared BeneficialWhenPositive and
            // guarded above as positive), but ExposedStatusEffect writes its argument straight into
            // DefensePercentAdjustment. Passing the magnitude unchanged granted the target +20%
            // Defense instead of taking it away, so it must be negated here.
            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(-exposedPercent),
                exposedDuration,
                CombatDamageType.Physical);
        }

        public static int GetBackAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return IsMatchingBackAttack(attacker, defender, skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.BackAttackCriticalRatePercentAdjustment)
                : 0;
        }

        public static int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion)
        {
            if (evasion <= 0 || !IsMatchingSideAttack(attacker, defender, skillType))
                return evasion;

            var chance = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChance);
            if (chance <= 0)
                return evasion;

            var scalingAbility = GetAbilityTypeFromStatPlusOne(
                Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                chance += Math.Max(0, GetAbilityScore(attacker, scalingAbility));
            }

            var chanceMaximum = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceMaximum);
            if (chanceMaximum > 0)
            {
                chance = Math.Min(chance, chanceMaximum);
            }

            var ignorePercent = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnorePercent);
            if (ignorePercent <= 0 || Random.D100(1) > chance)
                return evasion;

            return Math.Max(0, evasion - (int)Math.Ceiling(evasion * (Math.Min(100, ignorePercent) / 100f)));
        }

        public static int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !IsWeaponSkillType(skillType))
                return 0;

            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.RangedAutoAttackCycleCriticalRateRequiredCount);
            var criticalRate = Stat.GetStatAdjustment(attacker, StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment);
            if (requiredCount <= 0 || criticalRate <= 0)
            {
                ClearAutoAttackCycleCriticalRateTracker(attacker);
                return 0;
            }

            _autoAttackCycleCriticalCounts.TryGetValue(attacker, out var count);
            count++;
            var trackerIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.RangedAutoAttackCycleCriticalRateTrackerEffectIconType));
            StatusEffect.RemoveStatusEffect(attacker, typeof(AttackCycleTrackerStatusEffect), false);
            if (trackerIcon != EffectIconType.Invalid)
            {
                StatusEffect.ApplyStatusEffect(
                    attacker,
                    attacker,
                    new AttackCycleTrackerStatusEffect(
                        $"Attack cycle: {Math.Min(count, requiredCount)}/{requiredCount}",
                        trackerIcon),
                    count >= requiredCount ? 3f : 0f);
            }
            if (count < requiredCount)
            {
                _autoAttackCycleCriticalCounts[attacker] = count;
                return 0;
            }

            _autoAttackCycleCriticalCounts[attacker] = 0;
            PlayerFeedback.ShowDiagnosticFloatingText(
                ColorToken.Combat($"Attack +{criticalRate}% Critical Rate"),
                attacker,
                false);
            return criticalRate;
        }

        private static void ClearAutoAttackCycleCriticalRateTracker(uint creature)
        {
            _autoAttackCycleCriticalCounts.Remove(creature);
            StatusEffect.RemoveStatusEffect(creature, typeof(AttackCycleTrackerStatusEffect), false);
        }

        private static int GetLowHPCriticalRateAdjustment(uint attacker)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.LowHPCriticalRatePercentAdjustment);
            var maximumHP = GetMaxHitPoints(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(attacker) <= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        public static int GetLowHPAttackAdjustment(uint attacker)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackPercentAdjustment);
            var maximumHP = GetMaxHitPoints(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(attacker) <= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        public static int GetLowFPAttackAdjustment(uint attacker)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowFPAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.LowFPAttackPercentAdjustment);
            var maximumFP = Stat.GetMaxFP(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumFP <= 0)
                return 0;

            return Stat.GetCurrentFP(attacker) <= maximumFP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        private static int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.TargetStatusCriticalRateStatusCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetStatusCriticalRatePercentAdjustment);
            if (category == 0 || adjustment == 0 || !TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

        private static int ApplyOutgoingDamageModifier(uint attacker, int damage)
        {
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.DamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private static int ApplyWeaponAndForceDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!IsWeaponOrForceDamage(skillType, damageType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.WeaponAndForceDamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyDamageTypeDealtModifiers(uint attacker, int damage, CombatDamageType damageType)
        {
            if (damage <= 0)
                return damage;

            var adjustment = damageType == CombatDamageType.Poison
                ? Stat.GetStatAdjustment(attacker, StatType.PoisonDamageDealtPercentAdjustment)
                : 0;
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private static int ApplyTargetLowHPDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamageThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamagePercentAdjustment);

            if (threshold > 0 && adjustment != 0)
            {
                damage = ApplyTargetHPDamageAdjustment(
                    damage,
                    GetCurrentHitPoints(defender),
                    GetMaxHitPoints(defender),
                    threshold,
                    adjustment);
            }

            return ApplyTargetLowHPStatusDamageModifier(attacker, defender, damage);
        }

        private static int ApplyTargetLowHPStatusDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPStatusDamageThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPStatusDamagePercentAdjustment);
            var category = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.TargetLowHPStatusDamageStatusCategory));

            if (threshold <= 0 || adjustment == 0 || category == 0 || !TargetHasAnyStatusEffectCategory(defender, category))
                return damage;

            var maxHP = GetMaxHitPoints(defender);
            if (maxHP <= 0 || GetCurrentHitPoints(defender) > maxHP * (threshold / 100f))
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int GetNearbyStatusTargetAttackAdjustment(uint creature)
        {
            var percentPerTarget = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackPercentPerTarget);
            var radius = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackRadiusMeters);
            var maximum = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackPercentMaximum);
            var categoryValue = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackStatusCategory);
            if (percentPerTarget == 0 || radius <= 0 || categoryValue <= 0)
                return 0;

            var category = (StatusEffectCategory)categoryValue;
            var count = 0;
            var location = GetLocation(creature);
            foreach (var target in ObjectSnapshot.InShape(Shape.Sphere, radius, location, true))
            {
                if (!GetIsObjectValid(target))
                    continue;

                if (target != creature &&
                    GetIsReactionTypeHostile(target, creature) &&
                    StatusEffect.HasStatusEffectCategory(target, category))
                {
                    count++;
                }

            }

            var adjustment = count * percentPerTarget;
            return maximum > 0
                ? Math.Min(maximum, adjustment)
                : adjustment;
        }

        private static int ApplyTargetStatusDamageModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            bool isAbilityDamage,
            bool canApplyRandomFlatBonuses,
            AbilityDetail ability,
            out int flatBonus,
            out int damageTakenAdjustment)
        {
            var adjustment = 0;
            flatBonus = 0;
            damageTakenAdjustment = 0;
            adjustment += GetStatusSourceStatAdjustment(
                attacker,
                defender,
                StatType.DamageToStatusSourcePercentAdjustment);
            damageTakenAdjustment += GetStatusSourceStatAdjustment(
                defender,
                attacker,
                StatType.DamageTakenFromStatusSourcePercentAdjustment);
            damageTakenAdjustment += GetStatusSourcePartyStatAdjustment(
                defender,
                attacker,
                StatType.DamageTakenFromStatusSourcePartyPercentAdjustment);

            if (StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect)))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToSunderedTargetPercentAdjustment);

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToBleedingTargetPercentAdjustment);
                if (isAbilityDamage)
                {
                    flatBonus += GetSkillSelectedStatAdjustment(
                        attacker,
                        skillType,
                        StatType.AbilityDamageToBleedingTargetSkillType,
                        StatType.AbilityDamageToBleedingTargetBonus);
                }
            }

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Debuff))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToDebuffedTargetPercentAdjustment);

            adjustment += GetDamageToSourceAppliedStatusTargetAdjustment(attacker, defender);
            if (isAbilityDamage)
            {
                adjustment += GetAbilityDamageToSourceAppliedStatusTargetAdjustment(attacker, defender, skillType);
                flatBonus += GetAbilityDamageToSourceAppliedStatusTargetBonus(attacker, defender);
            }

            if (isAbilityDamage &&
                skillType == SkillType.Katar &&
                StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect), typeof(DisorientedStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment);
            }

            if (canApplyRandomFlatBonuses &&
                skillType == SkillType.Katar &&
                StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                var flatBonusChance = Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedTargetFlatBonusChance);
                var poisonBonus = Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedTargetFlatBonus);
                if (flatBonusChance > 0 && poisonBonus != 0 && Random.D100(1) <= flatBonusChance)
                {
                    flatBonus += poisonBonus;
                }
            }

            if (StatusEffect.HasStatusEffect(defender, typeof(WeakenedStatusEffect), typeof(HamstringStatusEffect)))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment);

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Control))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToControlTargetPercentAdjustment);

            if (isAbilityDamage)
            {
                adjustment += GetHighResourceStatAdjustment(attacker,
                    StatType.HighFPAndStaminaAbilityDamagePercentAdjustment,
                    StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent, ability);
            }

            if (skillType == SkillType.Rifle &&
                StatusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect), typeof(DazedStatusEffect), typeof(TranquilizedStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToDisorientedDazedTargetPercentAdjustment);
            }

            if (damageType.IsPhysicalDamageType())
            {
                damageTakenAdjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalDamageTakenPercentAdjustment);
            }

            if (isAbilityDamage && damageType.IsPhysicalDamageType())
                damageTakenAdjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalAbilityDamageTakenPercentAdjustment);

            if (damageType == CombatDamageType.Force)
            {
                damageTakenAdjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenPercentAdjustment);
            }

            if (damageType.IsPhysicalDamageType() && IsRangedDamageSkill(skillType))
                damageTakenAdjustment += Stat.GetStatAdjustment(defender, StatType.RangedPhysicalDamageTakenPercentAdjustment);

            if (skillType == SkillType.Throwing)
                damageTakenAdjustment += Stat.GetStatAdjustment(defender, StatType.ThrowingDamageTakenPercentAdjustment);

            if (IsRangedWeaponSkill(skillType) && IsNearbyTargetWithinDistance(attacker, defender, 8f))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.RangedDamageToNearbyTargetPercentAdjustment);

            if (skillType == SkillType.Pistol &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(DisorientedStatusEffect),
                    typeof(KnockdownStatusEffect),
                    typeof(TranquilizedStatusEffect)))
            {
                flatBonus += Stat.GetStatAdjustment(
                    attacker,
                    StatType.PistolDamageToDisorientedKnockdownOrTranquilizedTargetBonus);
            }

            if (isAbilityDamage &&
                skillType == SkillType.Staff &&
                StatusEffect.HasStatusEffect(defender, typeof(KnockdownStatusEffect), typeof(BlindStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToKnockdownOrBlindTargetPercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyTwinBladeAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isSingleTargetAbility,
            bool isAreaAbility)
        {
            if (damage <= 0 || skillType != SkillType.TwinBlade)
                return damage;

            var adjustment = 0;
            if (isSingleTargetAbility)
            {
                adjustment += Stat.GetStatAdjustment(
                    attacker,
                    StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment);
            }

            if (isAreaAbility)
            {
                adjustment += Stat.GetStatAdjustment(
                    attacker,
                    StatType.TwinBladeAreaAbilityDamagePercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyThrowingAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || skillType != SkillType.Throwing || !isAreaAbility)
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.ThrowingAreaAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyPhysicalAbilityShapeDamageModifier(
            uint attacker,
            CombatDamageType damageType,
            int damage,
            bool isSingleTargetAbility)
        {
            if (damage <= 0 || !isSingleTargetAbility || !damageType.IsPhysicalDamageType())
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplySkillAreaAbilityDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || !isAreaAbility)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAreaAbilityDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAreaAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyRangedAbilityLongRangeDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 || !IsRangedAbilityLongRangeTarget(attacker, defender, skillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.RangedAbilityLongRangeDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyAreaAbilityAfterDeflectionDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || !isAreaAbility)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AreaAbilityAfterDeflectionDamagePercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return damage;

            var window = Stat.GetStatAdjustment(attacker, StatType.AreaAbilityAfterDeflectionWindowSeconds);
            var requiredSource = Stat.GetStatTypeDeflectionSource(
                StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment);
            if (window <= 0 ||
                requiredSource == DeflectionSource.None ||
                !HasRecentDeflection(attacker, requiredSource, window))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment);
            return ApplyPercentDamageAdjustment(damage, adjustment);
        }

        private static int ApplyRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            out int flatBonus)
        {
            flatBonus = 0;
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageSkillType));
            var autoAttackOnly = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageAutoAttackOnly) > 0;
            var percentPerHit = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentPerHit);
            var maxPercent = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentMax);
            var bonusPerHit = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusPerHit);
            var maxBonus = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusMax);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageDurationSeconds);
            var hasPercentBonus = percentPerHit > 0 && maxPercent > 0;
            var hasFlatBonus = bonusPerHit > 0 && maxBonus > 0;
            if (autoAttackOnly && isAbilityDamage ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                (!hasPercentBonus && !hasFlatBonus))
            {
                _repeatedTargetDamageStates.Remove(attacker);
                return damage;
            }

            var now = Clock.UtcNow;
            if (!_repeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0 && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = 1;
            if (hasPercentBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxPercent / (float)percentPerHit));
            if (hasFlatBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));

            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _repeatedTargetDamageStates[attacker] = state;

            if (hasPercentBonus)
            {
                var adjustment = Math.Min(maxPercent, state.Stacks * percentPerHit);
                damage += (int)Math.Ceiling(damage * (adjustment / 100f));
            }

            if (hasFlatBonus)
            {
                flatBonus = Math.Min(maxBonus, state.Stacks * bonusPerHit);
            }

            return damage;
        }

        private static int ApplyMeleeRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage)
        {
            // Ability hits neither build nor break the auto-attack chain; only auto-attacks count.
            if (damage <= 0 || isAbilityDamage || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var bonusPerHit = Stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusPerHit);
            var maxBonus = Stat.GetStatAdjustment(attacker, StatType.MeleeRepeatedTargetDamageBonusMax);
            var statusEffectIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.MeleeRepeatedTargetDamageStatusEffectIcon));
            if (!IsWeaponSkillType(skillType) ||
                bonusPerHit <= 0 ||
                maxBonus <= 0)
            {
                _meleeRepeatedTargetDamageStates.Remove(attacker);
                MeleeRepeatedTargetDamageStatusEffect.Refresh(attacker, 0, statusEffectIcon);
                return damage;
            }

            if (!_meleeRepeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = Math.Max(1, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));
            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = Clock.UtcNow;
            _meleeRepeatedTargetDamageStates[attacker] = state;

            MeleeRepeatedTargetDamageStatusEffect.Refresh(attacker, state.Stacks, statusEffectIcon);
            return damage + Math.Min(maxBonus, state.Stacks * bonusPerHit);
        }

        /// <summary>
        /// Consecutive weapon hits build this independent damage stack with any weapon family.
        /// Ability hits count too, and stacks expire on their own timer.
        /// </summary>
        private static int ApplyRangedRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var bonusPerHit = Stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusPerHit);
            var maxBonus = Stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageBonusMax);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.RangedRepeatedTargetDamageDurationSeconds);
            if (bonusPerHit <= 0 ||
                maxBonus <= 0 ||
                durationSeconds <= 0)
            {
                ClearRangedRepeatedTargetDamageTracker(attacker);
                return damage;
            }

            // Non-weapon damage (device pulses, Force powers) neither builds nor breaks the
            // weapon-hit streak, so it leaves the stack untouched.
            if (!IsWeaponSkillType(skillType))
                return damage;

            var now = Clock.UtcNow;
            if (!_rangedRepeatedTargetDamageStates.TryGetValue(attacker, out var state) ||
                state.Target != defender ||
                durationSeconds > 0 && (now - state.LastHit).TotalSeconds > durationSeconds)
            {
                state = new RepeatedTargetDamageState(defender);
            }

            var maxStacks = Math.Max(1, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));
            state.Stacks = Math.Min(state.Stacks + 1, maxStacks);
            state.LastHit = now;
            _rangedRepeatedTargetDamageStates[attacker] = state;

            // Keep the stack visible and refresh its timer on every qualifying hit. The
            // floating text is deliberately emitted here, after the stack has been capped,
            // so it always reports the actual bonus that the next hit receives.
            var stackBonus = Math.Min(maxBonus, state.Stacks * bonusPerHit);
            StatusEffect.RemoveStatusEffect(attacker, typeof(SustainedFireStatusEffect), false);
            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new SustainedFireStatusEffect(state.Stacks, maxStacks, stackBonus),
                durationSeconds > 0 ? durationSeconds : 0f);
            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Sustained Fire {state.Stacks}/{maxStacks} (+{stackBonus} DMG)"),
                    attacker,
                    false);
            }

            return damage + stackBonus;
        }

        private static void ClearRangedRepeatedTargetDamageTracker(uint creature)
        {
            _rangedRepeatedTargetDamageStates.Remove(creature);
            StatusEffect.RemoveStatusEffect(creature, typeof(SustainedFireStatusEffect), false);
        }

        private static void ApplySameTargetPressureDamageEffects(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            var buildSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureBuildSkillType));
            var buildSeconds = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureBuildSeconds);
            var graceSeconds = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureGraceSeconds);
            var readyDurationSeconds = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureReadyDurationSeconds);
            var damageBonus = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus);
            if (buildSeconds <= 0 ||
                graceSeconds <= 0 ||
                readyDurationSeconds <= 0 ||
                damageBonus <= 0)
            {
                return;
            }

            var now = Clock.UtcNow;
            if (_sameTargetPressureStates.TryGetValue(attacker, out var state) &&
                state.ReadyUntil != default)
            {
                if (IsSameTargetPressureReadyStateActive(attacker, state, now))
                    return;

                ClearSameTargetPressureState(attacker);
                state = null;
            }

            if (!IsWeaponSkillType(skillType))
                return;

            if (!SkillTypeMatchesOrGlobal(skillType, buildSkillType))
            {
                if (state != null && state.Target != defender)
                    ClearSameTargetPressureState(attacker);

                return;
            }

            if (state == null || state.Target != defender)
            {
                state = CreateSameTargetPressureBuildState(defender, now);
            }
            else if ((now - state.LastBuildHitAt).TotalSeconds > graceSeconds)
            {
                state = CreateSameTargetPressureBuildState(defender, now);
            }
            else
            {
                state.LastBuildHitAt = now;
            }

            if ((now - state.StartedAt).TotalSeconds >= buildSeconds)
            {
                ReadySameTargetPressure(attacker, state, now, readyDurationSeconds);
                return;
            }

            _sameTargetPressureStates[attacker] = state;
        }

        public static int GetSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType) ||
                !TryGetReadySameTargetPressureState(attacker, target, out _))
            {
                return 0;
            }

            return Math.Max(0, Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus));
        }

        public static void ConsumeSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 ||
                GetSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType) <= 0)
            {
                return;
            }

            ClearSameTargetPressureState(attacker);

            if (GetIsPC(attacker))
            {
                var bonus = Stat.GetStatAdjustment(attacker, StatType.SameTargetPressureWeaponAbilityDamageBonus);
                FloatingTextStringOnCreature(ColorToken.Combat($"Spotter's Rhythm (+{bonus} DMG)"), attacker, false);
            }
        }

        private static SameTargetPressureState CreateSameTargetPressureBuildState(uint target, DateTime now)
        {
            return new SameTargetPressureState
            {
                Target = target,
                StartedAt = now,
                LastBuildHitAt = now,
                ReadyUntil = default
            };
        }

        private static void ReadySameTargetPressure(
            uint attacker,
            SameTargetPressureState state,
            DateTime now,
            int readyDurationSeconds)
        {
            state.ReadyUntil = now.AddSeconds(readyDurationSeconds);
            _sameTargetPressureStates[attacker] = state;

            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                typeof(SpottersRhythmStatusEffect),
                readyDurationSeconds);

            if (GetIsPC(attacker))
            {
                FloatingTextStringOnCreature(ColorToken.Combat("Spotter's Rhythm"), attacker, false);
            }
        }

        private static bool TryGetReadySameTargetPressureState(
            uint attacker,
            uint target,
            out SameTargetPressureState state)
        {
            state = null;
            if (!_sameTargetPressureStates.TryGetValue(attacker, out var existing) ||
                existing.ReadyUntil == default)
            {
                return false;
            }

            var now = Clock.UtcNow;
            if (!IsSameTargetPressureReadyStateActive(attacker, existing, now))
            {
                ClearSameTargetPressureState(attacker);
                return false;
            }

            if (existing.Target != target)
                return false;

            state = existing;
            return true;
        }

        private static bool IsSameTargetPressureReadyStateActive(
            uint attacker,
            SameTargetPressureState state,
            DateTime now)
        {
            return state.ReadyUntil > now &&
                   StatusEffect.HasStatusEffect(attacker, typeof(SpottersRhythmStatusEffect), attacker);
        }

        private static void ClearSameTargetPressureState(uint attacker)
        {
            _sameTargetPressureStates.Remove(attacker);
            StatusEffect.RemoveStatusEffect(
                attacker,
                typeof(SpottersRhythmStatusEffect),
                attacker,
                false);
        }

        public static int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense)
        {
            if (defense <= 0)
                return defense;

            var adjustment = GetStatusSourceStatAdjustment(
                defender,
                attacker,
                StatType.DefenseAgainstStatusSourcePercentAdjustment);

            return adjustment == 0
                ? defense
                : Math.Max(0, defense + (int)Math.Ceiling(defense * (adjustment / 100f)));
        }

        public static int ApplyStatusSourceAccuracyModifiers(uint attacker, uint defender, int accuracy)
        {
            if (accuracy <= 0)
                return accuracy;

            var adjustment = GetStatusSourceStatAdjustment(
                attacker,
                defender,
                StatType.AccuracyToStatusSourcePercentAdjustment);

            return adjustment == 0
                ? accuracy
                : Math.Max(1, accuracy + (int)Math.Ceiling(accuracy * (adjustment / 100f)));
        }

        private sealed class RepeatedTargetDamageState
        {
            public uint Target { get; }
            public int Stacks { get; set; }
            public DateTime LastHit { get; set; }

            public RepeatedTargetDamageState(uint target)
            {
                Target = target;
                LastHit = Clock.UtcNow;
            }
        }

        private static void ForgetDamageModifierState(uint creature)
        {
            _autoAttackCycleCriticalCounts.Remove(creature);
            _repeatedTargetDamageStates.Remove(creature);
            _meleeRepeatedTargetDamageStates.Remove(creature);
            _rangedRepeatedTargetDamageStates.Remove(creature);
            ClearSameTargetPressureState(creature);
            foreach (var pressureState in _sameTargetPressureStates.Where(x => x.Value.Target == creature).Select(x => x.Key).ToList())
            {
                ClearSameTargetPressureState(pressureState);
            }
        }
    }
}
