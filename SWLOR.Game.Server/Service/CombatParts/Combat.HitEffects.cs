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
        private static readonly Dictionary<uint, CriticalHitSequenceState> _criticalHitSequenceStates = new();
        private static readonly Dictionary<uint, int> _autoAttackCycleCounts = new();
        private static readonly Dictionary<(uint, uint, StatusEffectCategory), int> _sourceStatusAutoAttackCycleCounts = new();
        private static readonly Dictionary<uint, int> _meleeAutoAttackCycleCounts = new();

        private sealed class CriticalHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
        }

        public static int ApplyAutoAttackDamageModifiers(uint attacker, uint defender, int damage, SkillType skillType = SkillType.Invalid)
        {
            if (damage <= 0)
                return damage;

            damage += TemporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);

            damage += CalculateAutoAttackProcDamage(
                Stat.GetStatSources(attacker, StatType.AutoAttackDamageBonus), () => Random.D100(1));

            damage += ConsumeNextSkillAutoAttackDamageBonus(attacker, skillType);

            var nextAutoAttackBonus = TemporaryStatModifier.Consume(
                attacker,
                StatType.NextAutoAttackDamageBonus,
                StatType.NextAutoAttackDamageBonus);
            if (nextAutoAttackBonus != 0)
            {
                damage += nextAutoAttackBonus;
            }

            var guaranteedStaminaRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackHitStaminaRestore);
            if (guaranteedStaminaRestore > 0)
            {
                RestoreAbilityHitStaminaFromStat(attacker, StatType.AutoAttackHitStaminaRestore, guaranteedStaminaRestore);
            }

            var staminaRestoreChance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackStaminaRestore);
            if (staminaRestoreChance > 0 && staminaRestore > 0 && Random.D100(1) <= staminaRestoreChance)
            {
                RestoreAbilityHitStaminaFromStat(attacker, StatType.AutoAttackStaminaRestore, staminaRestore);
            }

            var fpRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackFPRestore);
            var fpRestoreCooldown = Stat.GetStatAdjustment(attacker, StatType.AutoAttackFPRestoreCooldownSeconds);
            if (fpRestore > 0 && TryUseStatTrigger(attacker, StatType.AutoAttackFPRestore, fpRestoreCooldown))
            {
                RestoreFPFromStat(attacker, StatType.AutoAttackFPRestore, fpRestore);
            }

            var skillFpRestore = Stat.GetStatAdjustment(attacker, StatType.SkillAutoAttackFPRestore);
            var skillFpRestoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAutoAttackFPRestoreSkillType));
            if (skillFpRestore > 0 && SkillTypeMatches(skillType, skillFpRestoreSkillType))
            {
                RestoreFPFromStat(attacker, StatType.SkillAutoAttackFPRestore, skillFpRestore);
            }

            ApplyAutoAttackMasterResourceRestore(attacker);

            var accuracyPenaltyChance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentChance);
            var accuracyPenalty = Stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustment);
            var accuracyPenaltyDuration = Stat.GetStatAdjustment(attacker, StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds);
            if (accuracyPenaltyChance > 0 &&
                accuracyPenalty != 0 &&
                accuracyPenaltyDuration > 0 &&
                Random.D100(1) <= accuracyPenaltyChance)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.AccuracyPercentAdjustment,
                    accuracyPenalty,
                    accuracyPenaltyDuration,
                    StatType.AutoAttackTargetAccuracyPercentAdjustment);
            }

            damage += GetDirectDamageToStatusCategoryOrStealthBonus(attacker, defender);
            ApplyAutoAttackHamstringEffect(attacker, defender, skillType, CombatDamageType.Physical);
            ApplyAutoAttackSunderedTargetFPRestore(attacker, defender);
            ApplySourceStatusStackEffects(attacker, defender);
            ApplyAutoAttackCycleDamage(attacker, defender, skillType);
            ApplySourceStatusAutoAttackCycleDamage(attacker, defender, skillType);

            return damage;
        }

        public static void ConsumeSuppressedAutoAttackDamageBonuses(uint attacker, SkillType skillType)
        {
            TemporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);
            ConsumeNextSkillAutoAttackDamageBonus(attacker, skillType);
            TemporaryStatModifier.Consume(
                attacker,
                StatType.NextAutoAttackDamageBonus,
                StatType.NextAutoAttackDamageBonus);
        }

        private static void ApplyAutoAttackMasterResourceRestore(uint attacker)
        {
            var master = GetMaster(attacker);
            if (!GetIsObjectValid(master))
                return;

            var staminaRestoreChance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterStaminaRestore);
            if (staminaRestoreChance > 0 && staminaRestore > 0 && Random.D100(1) <= staminaRestoreChance)
            {
                RestoreStaminaFromStat(master, StatType.AutoAttackMasterStaminaRestore, staminaRestore);
            }

            var fpRestoreChance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterFPRestoreChance);
            var fpRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackMasterFPRestore);
            if (fpRestoreChance > 0 && fpRestore > 0 && Random.D100(1) <= fpRestoreChance)
            {
                RestoreFPFromStat(master, StatType.AutoAttackMasterFPRestore, fpRestore);
            }
        }

        private static void ApplyAutoAttackHamstringEffect(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.AutoAttackHamstringSkillType));
            var duration = Stat.GetStatAdjustment(attacker, StatType.AutoAttackHamstringDurationSeconds);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                typeof(HamstringStatusEffect),
                duration,
                damageType);
        }

        private static int ConsumeNextSkillAutoAttackDamageBonus(uint attacker, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                attacker,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                StatType.NextSkillAutoAttackDamageBonusSkillType));
            if (storedSkillType != skillType)
                return 0;

            var bonus = TemporaryStatModifier.Consume(
                attacker,
                StatType.NextSkillAutoAttackDamageBonus,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
            TemporaryStatModifier.Consume(
                attacker,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                StatType.NextSkillAutoAttackDamageBonusSkillType);

            return bonus;
        }

        /// <summary>
        /// Advances the auto-attack cycle counter for cycle perks with no radius (Follow-Through) and,
        /// on the attack that completes the cycle, returns the cycle bonus as DMG so it feeds the
        /// standard attack damage formula and scales with the attacker's stats. Radius-based cycle
        /// perks (Edge Rhythm) hit a nearby enemy instead and are handled after the damage roll by
        /// ApplyAutoAttackCycleDamage.
        /// </summary>
        public static int ConsumeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return 0;

            var damageBonus = ConsumeMeleeAutoAttackCycleDamageBonus(attacker, skillType);
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamageSkillType));
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRequiredCount);
            var cycleDamage = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamage);
            var radius = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRadiusMeters);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                cycleDamage <= 0 ||
                radius > 0)
                return damageBonus;

            _autoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _autoAttackCycleCounts[attacker] = count;
                return damageBonus;
            }

            _autoAttackCycleCounts[attacker] = 0;
            return damageBonus + cycleDamage;
        }

        private static int ConsumeMeleeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType)
        {
            if (!IsWeaponSkillType(skillType))
                return 0;

            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.MeleeAutoAttackCycleRequiredCount);
            var cycleDamage = Stat.GetStatAdjustment(attacker, StatType.MeleeAutoAttackCycleDamage);
            if (requiredCount <= 0 || cycleDamage <= 0)
                return 0;

            _meleeAutoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _meleeAutoAttackCycleCounts[attacker] = count;
                return 0;
            }

            _meleeAutoAttackCycleCounts[attacker] = 0;
            return cycleDamage;
        }

        private static void ApplyAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return;

            // No declared skill means every auto-attack advances the cycle - "every third
            // auto-attack" wording carries no weapon qualifier.
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamageSkillType));
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRequiredCount);
            var cycleDamage = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleDamage);
            var radius = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleRadiusMeters);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                cycleDamage <= 0 ||
                radius <= 0)
                return;

            _autoAttackCycleCounts.TryGetValue(attacker, out var count);
            count++;
            if (count < requiredCount)
            {
                _autoAttackCycleCounts[attacker] = count;
                return;
            }

            _autoAttackCycleCounts[attacker] = 0;
            var target = radius > 0
                ? GetNearestHostileCreatureWithinRange(attacker, defender, radius, defender)
                : defender;
            if (!GetIsObjectValid(target))
                return;

            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(attacker, StatType.AutoAttackCycleDamage);
            ApplyTriggeredDamage(
                attacker,
                target,
                cycleDamage,
                CombatDamageType.Physical,
                skillType);
        }

        private static void ApplySourceStatusAutoAttackCycleDamage(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
            {
                return;
            }

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleSkillType));
            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleRequiredCategory));
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.SourceStatusAutoAttackCycleRequiredCount);
            var damage = Stat.GetStatAdjustment(attacker, StatType.SourceStatusAutoAttackCycleDamage);
            var damageType = GetCombatDamageTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusAutoAttackCycleDamageType));
            var key = (attacker, defender, requiredCategory);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                requiredCategory == 0 ||
                requiredCount <= 0 ||
                damage <= 0 ||
                damageType == CombatDamageType.Invalid ||
                !TargetHasSourceAppliedStatusCategory(defender, attacker, requiredCategory))
            {
                _sourceStatusAutoAttackCycleCounts.Remove(key);
                return;
            }

            _sourceStatusAutoAttackCycleCounts.TryGetValue(key, out var count);
            count++;
            if (count < requiredCount)
            {
                _sourceStatusAutoAttackCycleCounts[key] = count;
                return;
            }

            _sourceStatusAutoAttackCycleCounts[key] = 0;
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(attacker, StatType.SourceStatusAutoAttackCycleDamage);
            ApplyTriggeredDamage(attacker, defender, damage, damageType, skillType);
        }

        private static void ApplyAutoAttackSplashEffects(uint attacker, uint defender, SkillType skillType)
        {
            if (!GetIsObjectValid(defender) || !GetIsReactionTypeHostile(defender, attacker))
                return;

            foreach (var source in Stat.GetStatSources(attacker, StatType.AutoAttackSplashDamage))
            {
                if (!CanTriggerAutoAttackSplash(source, skillType) || Random.D100(1) > source[StatType.AutoAttackSplashChance])
                    continue;

                var origin = GetLocation(defender);
                var damage = source[StatType.AutoAttackSplashDamage];
                var targets = SelectAutoAttackSplashSecondaryTargets(
                    AbilityTargeting.GetHostileTargetsNearLocation(attacker, origin,
                        source[StatType.AutoAttackSplashRadiusMeters], maxTargets: 0),
                    defender, source[StatType.AutoAttackSplashMaximumTargets]);
                // Auto-attack riders run inside GetDamageRoll. Dispatching another damage event
                // here would re-enter the native attack and overwrite its shared combat context.
                DelayCommand(0.0f, () =>
                {
                    if (!GetIsObjectValid(attacker))
                        return;

                    using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(attacker, StatType.AutoAttackSplashDamage);
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Smoke_Puff), origin);
                    if (GetIsObjectValid(defender) && !GetIsDead(defender))
                        ApplyTriggeredDamage(attacker, defender, damage, CombatDamageType.Physical, skillType);
                    foreach (var target in targets)
                    {
                        if (GetIsObjectValid(target) && !GetIsDead(target))
                            ApplyTriggeredDamage(attacker, target, damage, CombatDamageType.Physical, skillType);
                    }
                });
            }
        }

        public static void ApplyDamageDealtEffects(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            bool isAbilityDamage = false,
            bool allowsHitProcs = true)
        {
            if (damage <= 0)
                return;

            var appliesDirectDamageEffects = deliveryType == CombatDamageDeliveryType.Direct;
            if (appliesDirectDamageEffects)
                CapstoneProof.RecordBeastDamage(attacker, defender);

            if (appliesDirectDamageEffects && attacker != defender &&
                GetIsObjectValid(attacker) && GetIsObjectValid(defender) && GetIsReactionTypeHostile(attacker, defender) &&
                PairedRangedTraits.HasDirectHostileDamageTrait(defender) &&
                HasPositiveDirectDamageAfterNativeMitigation(attacker, defender, damage, damageType, isAbilityDamage, skillType))
                PairedRangedTraits.RecordDirectHostileDamage(defender);

            TrackCombatActivity(attacker);
            TrackRecentDamageTarget(attacker, defender);

            if (!appliesDirectDamageEffects)
                return;
            if (isAbilityDamage && WeaponImpactProfile.Get(attacker)?.Activation.TryDamageEffects(defender) == false)
                return;

            if (!isAbilityDamage)
                ApplyAutoAttackSplashEffects(attacker, defender, skillType);

            // The combat-entry tracker resets this rider's consumed state before the first landed
            // direct attack. Missed casts and incoming hostile actions keep that state alive without
            // consuming it, so Venatic Recovery cannot trigger twice during one prolonged engagement.
            ApplyFirstCombatAttackStaminaRestore(attacker);

            ApplySameTargetPressureDamageEffects(attacker, defender, skillType);
            ApplySideAttackDamageEffects(attacker, defender, skillType, damage);
            ApplyDamageDealtStaminaRestore(attacker, skillType);
            ApplyDamageDealtAttackDelayReduction(attacker, skillType);
            ApplyPredatorsMarkEffects(attacker, defender, isAbilityDamage);
            if (!isAbilityDamage)
                ApplyAutoAttackSuppressionStack(attacker, defender, skillType, damageType);

            ApplyDamageDealtForceErosionEffect(attacker, defender, deliveryType);
            ApplyDamageDealtHamstringEffect(attacker, defender, skillType, damageType);
            if (allowsHitProcs)
                ApplyDamageDealtMimicryTraitProcs(attacker, defender);
            ApplyNextDamageDealtBleedEffect(attacker, defender, damageType);
            ApplyRangedHitSuppressionStack(attacker, defender, skillType, damageType);
            ApplyBleedingTargetStaminaRestore(attacker, defender, skillType, isAbilityDamage);
            ApplyToxicRushDamageDealtEffects(attacker, defender, deliveryType);
            ApplyHeavyVibrobladeDefenseDamageRecovery(attacker, damage);
            ApplyFrenzySlashHasteRefresh(attacker);

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
            }

            if (damageType.IsPhysicalDamageType())
            {
                hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.PhysicalDamageDealtHPPercentRestore);
                if (hpRestorePercent > 0)
                {
                    ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
                }
            }

            ApplyLowHPDamageDealtHPRestore(attacker, damage);
        }

        private static void ApplyHeavyVibrobladeDefenseDamageRecovery(uint attacker, int damage)
        {
            if (Stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow) <= 0)
                return;

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore);
            if (hpRestorePercent <= 0)
                return;

            ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
        }

        private static void ApplyPredatorsMarkEffects(
            uint attacker,
            uint defender,
            bool isAbilityDamage)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetIsDead(attacker) ||
                GetIsDead(defender))
            {
                return;
            }

            if (StatusEffect.HasStatusEffect(defender, typeof(PredatorsMark1StatusEffect), attacker))
            {
                ApplyPredatorsMarkFollowUp(attacker);
                return;
            }

            if (!isAbilityDamage)
                return;

            var damageTakenFromBeastPercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkDamageTakenFromBeastPercent);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkDurationSeconds);
            if (damageTakenFromBeastPercent <= 0 || durationSeconds <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new PredatorsMark1StatusEffect(damageTakenFromBeastPercent),
                durationSeconds,
                ResistanceType.Trauma);
        }

        private static void ApplyPredatorsMarkFollowUp(uint attacker)
        {
            var hastePercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkHastePercentPerStack);
            var abilityHitChancePercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkAbilityHitChancePercentPerStack);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkFollowUpDurationSeconds);
            var maximumStacks = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkFollowUpMaximumStacks);

            if (durationSeconds <= 0 || maximumStacks <= 0)
                return;

            if (hastePercent > 0)
            {
                TemporaryStatModifier.AddCapped(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    durationSeconds,
                    hastePercent * maximumStacks,
                    StatType.PredatorsMarkHastePercentPerStack,
                    1);
            }

            if (abilityHitChancePercent <= 0)
                return;

            TemporaryStatModifier.AddCapped(
                attacker,
                StatType.AbilityHitChancePercentAdjustment,
                abilityHitChancePercent,
                durationSeconds,
                abilityHitChancePercent * maximumStacks,
                StatType.PredatorsMarkAbilityHitChancePercentPerStack,
                1);
            TemporaryStatModifier.Replace(
                attacker,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                (int)SkillType.BeastMastery,
                durationSeconds,
                StatType.PredatorsMarkAbilityHitChancePercentPerStack);
        }

        private static void ApplyLowHPDamageDealtHPRestore(uint attacker, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPDamageDealtHPRestoreThresholdPercent);
            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.LowHPDamageDealtHPPercentRestore);
            if (threshold <= 0 || hpRestorePercent <= 0)
                return;

            var maxHP = GetMaxHitPoints(attacker);
            if (maxHP <= 0 || GetCurrentHitPoints(attacker) >= maxHP * (threshold / 100f))
                return;

            ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent);
        }

        private static void ApplyBleedingTargetStaminaRestore(
            uint attacker,
            uint defender,
            SkillType skillType,
            bool isAbilityDamage)
        {
            if (!GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
                return;

            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                SkillType.Invalid,
                StatType.DamageDealtBleedingTargetStaminaRestoreChance,
                StatType.DamageDealtBleedingTargetStaminaRestore);

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillDamageBleedingTargetStaminaRestoreSkillType));
            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                isAbilityDamage ? requiredSkillType : SkillType.Invalid,
                StatType.SkillDamageBleedingTargetStaminaRestoreChance,
                StatType.SkillDamageBleedingTargetStaminaRestore,
                StatType.SkillDamageBleedingTargetStaminaRestoreCooldownSeconds);

            if (!isAbilityDamage)
                return;

            requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAbilityBleedingTargetStaminaRestoreSkillType));
            ApplyBleedingTargetStaminaRestoreChannel(
                attacker,
                skillType,
                requiredSkillType,
                StatType.SkillAbilityBleedingTargetStaminaRestoreChance,
                StatType.SkillAbilityBleedingTargetStaminaRestore,
                StatType.SkillAbilityBleedingTargetStaminaRestoreCooldownSeconds);
        }

        private static void ApplyBleedingTargetStaminaRestoreChannel(
            uint attacker,
            SkillType skillType,
            SkillType requiredSkillType,
            StatType chanceStat,
            StatType restoreStat,
            StatType cooldownStat = StatType.Invalid)
        {
            var chance = Stat.GetStatAdjustment(attacker, chanceStat);
            var staminaRestore = Stat.GetStatAdjustment(attacker, restoreStat);
            if (chance <= 0 ||
                staminaRestore <= 0 ||
                !SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                Random.D100(1) > chance)
            {
                return;
            }

            var cooldown = cooldownStat == StatType.Invalid
                ? 0
                : Stat.GetStatAdjustment(attacker, cooldownStat);
            if (!TryUseStatTrigger(attacker, restoreStat, cooldown))
                return;

            RestoreAbilityHitStaminaFromStat(attacker, restoreStat, staminaRestore);
        }

        private static void ApplyDamageDealtForceErosionEffect(
            uint attacker,
            uint defender,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct)
                return;

            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionDurationSeconds);
            if (duration <= 0)
                return;

            var fpLossPerTick = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionFPLossPerTick);
            var staminaLossPerTick = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionStaminaLossPerTick);
            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ForceErosionStatusEffect(fpLossPerTick, staminaLossPerTick),
                duration,
                CombatDamageType.Physical);
        }

        private static void ApplyDamageDealtHamstringEffect(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtHamstringSkillType));
            var chance = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHamstringChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHamstringDurationSeconds);

            if (chance <= 0 ||
                duration <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                Random.D100(1) > chance)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                typeof(HamstringStatusEffect),
                duration,
                damageType);
        }

        /// <summary>
        /// On-hit proc traits mimicked from enemies. Each equipped proc trait grants a
        /// DamageDealt&lt;Effect&gt;Chance stat (via its trait status effect); on a landed direct hit
        /// each chance is rolled independently and, on success, applies the matching status effect
        /// for a fixed duration. Mirrors <see cref="ApplyDamageDealtHamstringEffect"/> but is not
        /// weapon-skill gated, since the analyzer replicates the trait regardless of armament.
        /// </summary>
        private static readonly (StatType Chance, Type Effect, CombatDamageType Damage, float Duration, StatType DurationOverride)[] MimicryTraitProcs =
        {
            (StatType.DamageDealtBleedChance, typeof(BleedStatusEffect), CombatDamageType.Physical, 12f, StatType.Invalid),
            (StatType.DamageDealtFreezingChance, typeof(FreezingStatusEffect), CombatDamageType.Ice, 6f, StatType.Invalid),
            (StatType.DamageDealtShockChance, typeof(ShockStatusEffect), CombatDamageType.Electrical, 10f, StatType.Invalid),
            (StatType.DamageDealtSunderChance, typeof(SunderStatusEffect), CombatDamageType.Physical, 14f, StatType.Invalid),
            (StatType.DamageDealtHemorrhageChance, typeof(HemorrhageStatusEffect), CombatDamageType.Physical, 12f, StatType.Invalid),
            (StatType.DamageDealtPoisonChance, typeof(PoisonStatusEffect), CombatDamageType.Poison, 12f, StatType.DamageDealtPoisonDurationSeconds),
        };

        private static void ApplyDamageDealtMimicryTraitProcs(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(defender))
                return;

            foreach (var proc in MimicryTraitProcs)
            {
                var chance = Stat.GetStatAdjustment(attacker, proc.Chance);
                if (chance <= 0 || Random.D100(1) > chance)
                    continue;

                var durationOverride = proc.DurationOverride == StatType.Invalid
                    ? 0
                    : Stat.GetStatAdjustment(attacker, proc.DurationOverride);
                var duration = durationOverride > 0 ? durationOverride : proc.Duration;

                StatusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    proc.Effect,
                    duration,
                    proc.Damage);
            }
        }

        private static void ApplySideAttackDamageEffects(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !IsMatchingSideAttack(attacker, defender, skillType))
                return;

            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.SideAttackStaminaRestore);
            var staminaCooldown = Stat.GetStatAdjustment(attacker, StatType.SideAttackStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 && TryUseStatTrigger(attacker, StatType.SideAttackStaminaRestore, staminaCooldown))
            {
                RestoreAbilityHitStaminaFromStat(attacker, StatType.SideAttackStaminaRestore, staminaRestore);
            }

            var delayReduction = Stat.GetStatAdjustment(attacker, StatType.SideAttackDelayReductionPercent);
            var duration = Stat.GetStatAdjustment(attacker, StatType.SideAttackDelayReductionDurationSeconds);
            if (delayReduction != 0 && duration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    delayReduction,
                    duration,
                StatType.SideAttackDelayReductionPercent);
            }
        }

        private static void ApplyDamageDealtStaminaRestore(uint attacker, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtStaminaRestoreSkillType));
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.DamageDealtStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.DamageDealtStaminaRestoreCooldownSeconds);

            if (staminaRestore <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType) ||
                !TryUseStatTrigger(attacker, StatType.DamageDealtStaminaRestore, cooldown))
            {
                return;
            }

            RestoreAbilityHitStaminaFromStat(attacker, StatType.DamageDealtStaminaRestore, staminaRestore);
        }

        private static void ApplyDamageDealtAttackDelayReduction(uint attacker, SkillType skillType)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtAttackDelayReductionSkillType));
            var delayReduction = Stat.GetStatAdjustment(attacker, StatType.DamageDealtAttackDelayReductionPercent);
            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds);

            if (delayReduction == 0 ||
                duration <= 0 ||
                !SkillTypeMatches(skillType, requiredSkillType))
            {
                return;
            }

            TemporaryStatModifier.Replace(
                attacker,
                StatType.AttackDelayReductionPercent,
                delayReduction,
                duration,
                StatType.DamageDealtAttackDelayReductionPercent);
        }

        public static void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid)
        {
            if (criticalRating <= 0)
                return;

            // Limited Haste is earned by the critical result itself, even when mitigation reduces
            // that critical's damage to zero. Damage-derived critical riders remain below.
            ApplyCriticalHitLimitedHaste(attacker, skillType);

            if (damage <= 0)
                return;

            if (GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender) > 0)
            {
                FloatingTextStringOnCreature(ColorToken.Combat("Weak Points"), attacker, false);
            }

            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestore);
            var staminaRestoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreSkillType));
            var staminaRestoreCooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 &&
                SkillTypeMatches(skillType, staminaRestoreSkillType) &&
                TryUseStatTrigger(attacker, StatType.CriticalStaminaRestore, staminaRestoreCooldown))
            {
                RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalStaminaRestore, staminaRestore);
            }

            ApplyCriticalNextAbilityDamageBonus(attacker, skillType);
            ApplyCriticalNextSkillAbilityDefenseIgnore(attacker, skillType);
            ApplyCriticalNextAutoAttackNoDelay(attacker, skillType);
            ApplyCriticalSideAttackStaminaRestore(attacker, defender);
            ApplyCriticalBleedingStatusDurationExtension(attacker, defender);
            ApplyCriticalHitSequenceEffects(attacker);
            ApplyCriticalHitSelfEffects(attacker);

            var poisonedTargetStaminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalPoisonedTargetStaminaRestore);
            if (poisonedTargetStaminaRestore > 0 && StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalPoisonedTargetStaminaRestore, poisonedTargetStaminaRestore);
            }

            var markedTargetStaminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalMarkedTargetStaminaRestore);
            if (markedTargetStaminaRestore > 0 && StatusEffect.HasStatusEffect(defender, typeof(MarkingTossStatusEffect), attacker))
            {
                RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalMarkedTargetStaminaRestore, markedTargetStaminaRestore);
            }

            var targetFPLossPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetFPLossPercentOfDamage);
            if (targetFPLossPercent > 0)
            {
                var fpLoss = GameMath.PercentOf(damage, targetFPLossPercent);
                ReduceFPFromStat(defender, StatType.CriticalTargetFPLossPercentOfDamage, fpLoss);
            }

            var targetStaminaLossPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetStaminaLossPercentOfDamage);
            if (targetStaminaLossPercent > 0)
            {
                var staminaLoss = GameMath.PercentOf(damage, targetStaminaLossPercent);
                ReduceStaminaFromStat(defender, StatType.CriticalTargetStaminaLossPercentOfDamage, staminaLoss);
            }

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestore);
            var hpRestoreCooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds);
            if (hpRestorePercent > 0 &&
                TryUseStatTrigger(attacker, StatType.CriticalHPPercentOfDamageRestore, hpRestoreCooldown))
            {
                ApplyDamageDerivedHealing(attacker, damage, hpRestorePercent,
                    passiveHealingCooldownSeconds: hpRestoreCooldown);
            }

            var accuracyPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyPercentAdjustment);
            var accuracyDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyDurationSeconds);
            if (accuracyPercent != 0 && accuracyDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AccuracyPercentAdjustment,
                    accuracyPercent,
                    accuracyDuration,
                    StatType.CriticalAccuracyPercentAdjustment);
            }

            var targetEvasionPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionPercentAdjustment);
            var targetEvasionDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionDurationSeconds);
            if (targetEvasionPercent != 0 && targetEvasionDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    targetEvasionPercent,
                    targetEvasionDuration,
                    StatType.CriticalTargetEvasionPercentAdjustment);
            }

            var targetDefensePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefenseDurationSeconds);
            if (skillType == SkillType.Staff)
            {
                targetDefensePercent += Stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefensePercentAdjustment);
                targetDefenseDuration = Math.Max(
                    targetDefenseDuration,
                    Stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefenseDurationSeconds));
            }
            if (targetDefensePercent != 0 && targetDefenseDuration > 0)
            {
                StatusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    new ExposedStatusEffect(targetDefensePercent),
                    targetDefenseDuration,
                    CombatDamageType.Physical);
            }

            if (isSingleTargetImpact)
            {
                ApplySingleTargetCriticalTargetDefenseEffect(attacker, defender);
            }
        }

        private static void ApplyCriticalHitSelfEffects(uint attacker)
        {
            var evasion = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionPercentAdjustment);
            var evasionDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionDurationSeconds);
            if (evasion != 0 && evasionDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    evasionDuration,
                    StatType.CriticalHitSelfEvasionPercentAdjustment);
            }

            var haste = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHastePercentAdjustment);
            var hasteDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHasteDurationSeconds);
            if (haste != 0 && hasteDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    hasteDuration,
                    StatType.CriticalHitSelfHastePercentAdjustment);
            }
        }

        public static void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType));
            var criticalRate = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment);
            var maximum = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds);
            var rangedOnly = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateRangedOnly) > 0;
            if (!MatchesNextSkillAbilityCriticalRateSkill(skillType, requiredSkillType, rangedOnly) ||
                criticalRate <= 0 ||
                maximum <= 0 ||
                duration <= 0)
            {
                return;
            }

            TemporaryStatModifier.Replace(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                (int)requiredSkillType,
                duration,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                rangedOnly ? 1 : 0,
                duration,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            TemporaryStatModifier.AddCapped(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                criticalRate,
                duration,
                maximum,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                1,
                refreshExistingStacks: true);

            var currentTotal = TemporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            if (currentTotal > 0)
            {
                var trackerIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                    activator,
                    StatType.NonCriticalAbilityNextSkillAbilityCriticalRateTrackerEffectIconType));
                StatusEffect.RemoveStatusEffect(
                    activator,
                    typeof(CriticalRateStackTrackerStatusEffect),
                    false);
                if (trackerIcon != EffectIconType.Invalid)
                {
                    StatusEffect.ApplyStatusEffect(
                        activator,
                        activator,
                        new CriticalRateStackTrackerStatusEffect(
                            $"Next ranged ability: +{currentTotal}% Critical Rate",
                            trackerIcon),
                        duration);
                }
                FloatingTextStringOnCreature(
                    ColorToken.Combat($"Next ranged ability +{currentTotal}% Critical Rate"),
                    activator,
                    false);
            }
        }

        private static void ApplyCriticalBleedingStatusDurationExtension(uint attacker, uint defender)
        {
            var extensionSeconds = Stat.GetStatAdjustment(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds);
            if (extensionSeconds <= 0 ||
                !GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker) &&
                !StatusEffect.HasStatusEffect(defender, typeof(HemorrhageStatusEffect), attacker))
            {
                return;
            }

            var cooldownSeconds = Stat.GetStatAdjustment(
                attacker,
                StatType.CriticalBleedingStatusDurationExtensionCooldownSeconds);
            if (!TryUseStatTrigger(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds, cooldownSeconds))
                return;

            StatusEffect.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
            StatusEffect.ExtendStatusEffectDuration(defender, typeof(HemorrhageStatusEffect), attacker, extensionSeconds);
        }

        private static void ApplyCriticalHitSequenceEffects(uint attacker)
        {
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceCountRequired);
            var windowSeconds = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceWindowSeconds);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceStaminaRestore);
            if (requiredCount <= 0 || windowSeconds <= 0 || staminaRestore <= 0)
                return;

            var now = Clock.UtcNow;
            var count = 1;
            if (_criticalHitSequenceStates.TryGetValue(attacker, out var state) &&
                (now - state.LastHit).TotalSeconds <= windowSeconds)
            {
                count = state.Count + 1;
            }

            if (count >= requiredCount)
            {
                _criticalHitSequenceStates.Remove(attacker);
                RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalHitSequenceStaminaRestore, staminaRestore);
                return;
            }

            _criticalHitSequenceStates[attacker] = new CriticalHitSequenceState
            {
                Count = count,
                LastHit = now
            };
        }

        private static void ApplyCriticalNextSkillAbilityDefenseIgnore(uint attacker, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return;

            var defenseIgnore = Stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment);
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds);
            if (defenseIgnore <= 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                duration,
                StatType.NextSkillAbilitySkillType);

            TemporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                defenseIgnore,
                duration,
                StatType.NextSkillAbilitySkillType);

            // The newest grant decides which abilities may spend the shared next-ability bonuses.
            TemporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilityRequiredCategory,
                0,
                duration,
                StatType.NextSkillAbilitySkillType);
        }

        private static void ApplyCriticalNextAbilityDamageBonus(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType));
            if (!SkillTypeMatches(skillType, triggerSkillType))
                return;

            var perkType = GetPerkTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusPerkType));
            var bonus = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonus);
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusCooldownSeconds);
            if (perkType == PerkType.Invalid || bonus == 0 || duration <= 0)
                return;

            if (TryUseStatTrigger(attacker, StatType.CriticalNextAbilityDamageBonus, cooldown))
            {
                GrantNextAbilityDamageBonus(attacker, perkType, bonus, duration);
            }
        }

        private static void ApplyCriticalHitLimitedHaste(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteTriggerSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, triggerSkillType))
                return;

            var hastePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHastePercentAdjustment);
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteDurationSeconds);
            var attackCount = Stat.GetStatAdjustment(attacker, StatType.CriticalHitLimitedHasteAttackCount);
            var statusEffectIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.CriticalHitLimitedHasteStatusEffectIcon));
            if (hastePercent <= 0 ||
                duration <= 0 ||
                attackCount <= 0 ||
                statusEffectIcon == EffectIconType.Invalid)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new LimitedHasteStatusEffect(
                    hastePercent,
                    attackCount,
                    SkillType.Invalid,
                    statusEffectIcon,
                    Ability.GetActiveAbilityImpactSummary(attacker)),
                duration);
        }

        private static void ApplyCriticalNextAutoAttackNoDelay(uint attacker, SkillType skillType)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, triggerSkillType))
                return;

            var noDelaySkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType));
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayCooldownSeconds);
            if (duration <= 0)
                return;

            if (TryUseStatTrigger(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType, cooldown))
            {
                if (noDelaySkillType == SkillType.Invalid)
                    GrantNextAutoAttackNoDelay(attacker, duration);
                else
                    GrantNextAutoAttackNoDelay(attacker, noDelaySkillType, duration);
            }
        }

        private static void ApplyCriticalSideAttackStaminaRestore(uint attacker, uint defender)
        {
            var chance = Stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestore);
            if (chance <= 0 || staminaRestore <= 0 || !IsAttackerBesideTarget(attacker, defender))
                return;

            if (Random.D100(1) <= chance)
            {
                RestoreAbilityHitStaminaFromStat(attacker, StatType.CriticalSideAttackStaminaRestore, staminaRestore);
            }
        }

        private static void ApplySingleTargetCriticalTargetDefenseEffect(uint attacker, uint defender)
        {
            var targetDefensePercent = Stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = Stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefenseDurationSeconds);
            if (targetDefensePercent == 0 || targetDefenseDuration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(targetDefensePercent),
                targetDefenseDuration,
                CombatDamageType.Physical);
        }

        private static bool HasPositiveDirectDamageAfterNativeMitigation(uint attacker, uint defender,
            int damage, CombatDamageType damageType, bool isAbilityDamage, SkillType skillType)
        {
            // Physical native rolls already passed these stages. Scripted abilities and
            // elemental rolls receive native mitigation when their damage effect is applied.
            if (!isAbilityDamage && damageType.IsPhysicalDamageType()) return damage > 0;
            var source = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker)?.AsNWSCreature();
            var target = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(defender)?.AsNWSObject();
            if (source == null || target == null) return false;
            var flags = (uint)damageType.GetNativeDamageType();
            var combatDamage = isAbilityDamage ? 0 : 1;
            var ranged = IsRangedDamageSkill(skillType) ? 1 : 0;
            // The fourth argument is the engine's bSimulation flag: these probes read mitigation without
            // consuming a finite damage-reduction or resistance budget (the real roll passes 0).
            const int simulation = 1;
            damage = target.DoDamageImmunity(source, damage, flags, simulation, combatDamage);
            if (damage > 0)
                damage = target.DoDamageResistance(source, damage, flags, simulation, combatDamage,
                    damageType.IsPhysicalDamageType() ? 1 : 0, ranged);
            if (damage > 0 && damageType.IsPhysicalDamageType())
                damage = target.DoDamageReduction(source, damage, 0, simulation, combatDamage, ranged);
            return damage > 0;
        }

        public static void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
                return;

            using var recorderAttribution = CombatRecorder.BeginAttribution(creature, CombatRecordSourceKind.OnKill);

            if (GetIsObjectValid(defeated))
            {
                ApplyPoisonedDefeatedEnemySpread(defeated, creature);
                ApplyDefeatedBleedingEnemySpread(creature, defeated);
            }

            var staminaRestore = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(creature, staminaRestore);
            }

            var fpRestore = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyFPRestore);
            if (fpRestore > 0)
            {
                Stat.RestoreFP(creature, fpRestore);
            }

            var hpRestorePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                HealFromMaxHP(creature, hpRestorePercent);
            }

            var attackPercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackPercentAdjustment);
            var attackDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DefeatedEnemyAttackPercentAdjustment);
            }

            var hastePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionPercent);
            var hasteDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyAttackDelayReductionDurationSeconds);
            if (hastePercent != 0 && hasteDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    hasteDuration,
                    StatType.DefeatedEnemyAttackDelayReductionPercent);
            }

            var allyDefensePercent = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            var allyDefenseDuration = Stat.GetStatAdjustment(creature, StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds);
            if (allyDefensePercent != 0 && allyDefenseDuration > 0)
            {
                ApplyDefeatedEnemyNearbyAllyDefense(creature, allyDefensePercent, allyDefenseDuration);
            }

            ApplyHitPointSpendDefeatedEnemyEffects(creature);
        }

        private static void ApplyHitPointSpendDefeatedEnemyEffects(uint creature)
        {
            if (Stat.GetStatAdjustment(creature, StatType.HeavyVibrobladeOffenseSoulAscension) <= 0)
                return;

            var marker = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.HeavyVibrobladeOffenseSoulAscension,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            if (marker <= 0)
                return;

            StatusEffect.ApplyStatusEffect(creature, creature, typeof(SoulAscensionStatusEffect), 30f);
        }

        private static void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated)
        {
            const float RecentDamageWindowSeconds = 6f;

            if (!GetIsObjectValid(defeated))
                return;

            var now = Clock.UtcNow;
            var recentDamagers = _recentDamageTargets
                .Where(x => x.Key.Item2 == defeated)
                .ToList();

            foreach (var ((source, target), lastDamaged) in recentDamagers)
            {
                if ((now - lastDamaged).TotalSeconds > RecentDamageWindowSeconds)
                {
                    _recentDamageTargets.Remove((source, target));
                    continue;
                }

                ApplyCruelMomentumEffect(source);
            }
        }

        private static void ApplyCruelMomentumEffect(uint creature)
        {
            if (!GetIsObjectValid(creature) ||
                GetIsDead(creature) ||
                Stat.GetStatAdjustment(creature, StatType.CruelMomentum) <= 0 ||
                !TryUseStatTrigger(creature, StatType.CruelMomentum, 10))
            {
                return;
            }

            RestoreFPFromStat(creature, StatType.CruelMomentum, 2);
            StatusEffect.ApplyStatusEffect(creature, creature, typeof(CruelMomentumStatusEffect), 30f);
        }

        private static void ApplyDefeatedEnemyNearbyAllyDefense(
            uint creature,
            int physicalDefensePercent,
            int durationSeconds)
        {
            const float Range = 5f;

            foreach (var member in Party.GetAllPartyMembersWithinRange(creature, Range))
            {
                if (member == creature)
                    continue;

                TemporaryStatModifier.Replace(
                    member,
                    StatType.PhysicalDefensePercentAdjustment,
                    physicalDefensePercent,
                    durationSeconds,
                StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment);
            }
        }

        private static void ApplyPoisonedDefeatedEnemySpread(uint defeated, uint fallbackSource)
        {
            var poisonEffects = StatusEffect.GetCreatureStatusEffects(defeated)
                .GetAllEffects()
                .Where(effect => effect is PoisonStatusEffect)
                .ToList();
            if (poisonEffects.Count <= 0)
                return;

            foreach (var poisonEffect in poisonEffects)
            {
                var source = GetIsObjectValid(poisonEffect.Source)
                    ? poisonEffect.Source
                    : fallbackSource;
                var radius = Stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadRadiusMeters);
                var duration = Stat.GetStatAdjustment(source, StatType.PoisonedDefeatedEnemySpreadDurationSeconds);
                if (radius <= 0 || duration <= 0)
                    continue;

                var target = GetNearestHostileCreatureWithinRange(source, defeated, radius, defeated);
                if (!GetIsObjectValid(target))
                    continue;

                StatusEffect.ApplyStatusEffect(source, target, typeof(PoisonStatusEffect), duration, CombatDamageType.Physical);
                return;
            }
        }

        private static void ApplyDefeatedBleedingEnemySpread(uint creature, uint defeated)
        {
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(defeated) ||
                !StatusEffect.HasStatusEffectCategory(defeated, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(creature, StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds);
            if (duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         creature,
                         GetLocation(defeated),
                         5f,
                         3,
                         defeated))
            {
                StatusEffect.ApplyStatusEffect(creature, nearby, typeof(BleedStatusEffect), duration, CombatDamageType.Physical);
            }
        }

        private static void ApplyNextDamageDealtBleedEffect(
            uint attacker,
            uint defender,
            CombatDamageType damageType)
        {
            var duration = TemporaryStatModifier.Consume(
                attacker,
                StatType.NextDamageDealtBleedDurationSeconds,
                StatType.NextDamageDealtBleedDurationSeconds);
            if (duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(attacker, defender, typeof(BleedStatusEffect), duration, damageType);
        }

        private static void ApplySourceStatusStackEffects(uint attacker, uint defender)
        {
            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusStackRequiredCategory));
            var appliedCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SourceStatusStackAppliedCategory));
            var maximumStacks = Stat.GetStatAdjustment(attacker, StatType.SourceStatusStackMaximum);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.SourceStatusStackDurationSeconds);
            if (requiredCategory == 0 ||
                appliedCategory == 0 ||
                maximumStacks <= 0 ||
                durationSeconds <= 0 ||
                !TargetHasSourceAppliedStatusCategory(defender, attacker, requiredCategory))
            {
                return;
            }

            var statusEffectType = GetSourceStatusStackEffectType(appliedCategory);
            if (statusEffectType == null)
                return;

            if (statusEffectType == typeof(InfectionStatusEffect))
            {
                ApplyInfectionStack(attacker, defender, maximumStacks, durationSeconds);
            }
        }

        private static void ApplyInfectionStack(
            uint attacker,
            uint defender,
            int maximumStacks,
            int durationSeconds)
        {
            var existing = StatusEffect.GetStatusEffect(defender, typeof(InfectionStatusEffect), attacker) as InfectionStatusEffect;
            if (existing == null)
            {
                StatusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    new InfectionStatusEffect(1),
                    durationSeconds,
                    CombatDamageType.Poison);
                return;
            }

            existing.AddStack(maximumStacks);
            StatusEffect.RefreshStatusEffectDuration(
                defender,
                typeof(InfectionStatusEffect),
                attacker,
                durationSeconds,
                sourceDamageType: CombatDamageType.Poison);
        }

        private static void ApplyFrenzySlashHasteRefresh(uint attacker)
        {
            var duration = Stat.GetStatAdjustment(attacker, StatType.FrenzySlashHasteRefreshDurationSeconds);
            if (duration <= 0)
                return;

            TemporaryStatModifier.Refresh(
                attacker,
                StatType.AttackDelayReductionPercent,
                duration,
                StatType.AttackDelayReductionPercent);
        }

        private static void ApplyToxicRushDamageDealtEffects(
            uint attacker,
            uint defender,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType == CombatDamageDeliveryType.DamageOverTime)
                return;

            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
                return;

            var haste = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushHastePercentPerStack);
            var attack = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushAttackPercentPerStack);
            var maxStacks = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushMaximumStacks);
            var duration = Stat.GetStatAdjustment(attacker, StatType.KatarToxicRushDurationSeconds);
            if (maxStacks <= 0 || duration <= 0 || (haste <= 0 && attack <= 0))
                return;

            var currentStacks = StatusEffect.GetStatusEffect<ToxicRushStatusEffect>(attacker)?.Stacks ?? 0;
            var stacks = Math.Min(maxStacks, currentStacks + 1);
            StatusEffect.ApplyStatusEffect(
                attacker,
                attacker,
                new ToxicRushStatusEffect(stacks, haste, attack),
                duration);

            if (stacks >= maxStacks)
            {
                RestoreAbilityHitStaminaFromStat(attacker, StatType.KatarToxicRushMaximumStacks, 2);
            }
        }

        private static void SpreadPoisonFromTarget(uint activator, uint target)
        {
            var radius = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadRadiusMeters);
            var duration = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentPoisonSpreadDurationSeconds);
            if (radius <= 0 || duration <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), radius, 0, target))
            {
                StatusEffect.ApplyStatusEffect(activator, nearby, typeof(PoisonStatusEffect), duration, CombatDamageType.Poison);
            }
        }

        public static int ApplyTriggeredDamage(
            uint activator,
            uint target,
            int damage,
            CombatDamageType damageType,
            SkillType skillType = SkillType.Invalid,
            bool typedLeadershipReductionAlreadyApplied = false,
            int? targetStatusDamagePercentAdjustment = null,
            bool outgoingModifiersAlreadyApplied = false)
        {
            if (damage <= 0)
                return 0;

            if (!outgoingModifiersAlreadyApplied)
                damage = ApplyDamageTypeDealtModifiers(activator, damage, damageType);
            var typedTargetAdjustment = damageType.IsPhysicalDamageType()
                ? Stat.GetStatAdjustment(target, StatType.PhysicalDamageTakenPercentAdjustment)
                : damageType == CombatDamageType.Force
                    ? Stat.GetStatAdjustment(target, StatType.ForceDamageTakenPercentAdjustment)
                    : 0;
            var targetDamage = ApplyTriggeredDamageTargetAdjustment(
                damage, typedTargetAdjustment, targetStatusDamagePercentAdjustment);
            damage = targetDamage.Damage;
            damage = Resistance.ApplyResistanceToDamage(target, damageType, damage);
            if (damage <= 0)
                return 0;

            damage = ApplyDamageTakenModifiers(
                target,
                damage,
                activator,
                damageType,
                deliveryType: CombatDamageDeliveryType.Triggered,
                targetStatusDamagePercentAdjustment: targetDamage.Adjustment,
                typedLeadershipReductionAlreadyApplied: typedLeadershipReductionAlreadyApplied);
            if (damage <= 0)
                return 0;

            var effectDamageType = damageType.GetNWScriptDamageType();
            if (!Ability.TryQueueTrackedDamageEffect(activator, target, damage, effectDamageType))
            {
                AssignCommand(
                    activator,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(damage, effectDamageType),
                        target));
            }

            ApplyDamageDealtEffects(activator, target, damage, skillType, damageType, CombatDamageDeliveryType.Triggered);
            StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered);
            return damage;
        }

        private static void ApplyAutoAttackSunderedTargetFPRestore(uint attacker, uint defender)
        {
            var fpRestore = Stat.GetStatAdjustment(attacker, StatType.AutoAttackSunderedTargetFPRestore);
            if (fpRestore <= 0 ||
                !GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect)))
            {
                return;
            }

            RestoreFPFromStat(attacker, StatType.AutoAttackSunderedTargetFPRestore, fpRestore);
        }

        private static void ForgetHitEffectState(uint creature)
        {
            _criticalHitSequenceStates.Remove(creature);
            _autoAttackCycleCounts.Remove(creature);
            foreach (var key in _sourceStatusAutoAttackCycleCounts.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _sourceStatusAutoAttackCycleCounts.Remove(key);
            }
            _meleeAutoAttackCycleCounts.Remove(creature);
        }
    }
}
