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
        private static readonly Dictionary<(uint, uint), int> _sameTargetHostileAbilityHitCounts = new();
        private static readonly Dictionary<(uint, uint), TargetHitSequenceState> _areaAbilityTargetHitSequences = new();

        public static int GetAbilityImpactBaseDamageBonus(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !GetIsObjectValid(activator))
                return 0;

            var bonus = 0;

            switch (skillType)
            {
                case SkillType.Lightsaber:
                    if (ability.IsAreaAbility)
                    {
                        bonus += Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseAreaDamageBonus);
                    }

                    if (ability.IsSingleTargetAbility &&
                        GetIsObjectValid(target) &&
                        StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                    {
                        bonus += Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDebuffedTargetDamageBonus);
                    }
                    break;
                case SkillType.Vibroknife when ability.IsHostileAbility:
                    var toxicCoatingRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
                    if (toxicCoatingRank > 0)
                    {
                        bonus += toxicCoatingRank >= 2 ? 22 : 10;
                    }
                    break;
                case SkillType.Staff when ability.IsHostileAbility:
                    bonus += Stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDamageBonus);
                    break;
                case SkillType.Saberstaff when ability.IsHostileAbility:
                    bonus += Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareDamageBonus);
                    break;
                case SkillType.TwinBlade when ability.IsHostileAbility &&
                    AbilityMatchesReversalCutTrigger(activator, ability):
                    bonus += TemporaryStatModifier.Consume(
                        activator,
                        StatType.TwinBladeDuelistReversalCutDamageBonus,
                        StatType.TwinBladeDuelistReversalCut);
                    break;
            }

            if (ability.IsHostileAbility)
            {
                bonus += GetFirstHostileAbilityHitDamageBonus(activator, ability);
                bonus += GetDirectDamageToStatusCategoryOrStealthBonus(activator, target);
            }

            bonus += ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(activator, skillType);

            if (ability.IsHostileAbility)
                bonus += GetHighResourceAbilityDamageBonus(activator, ability);

            return bonus;
        }

        public static int GetHighResourceAbilityDamageBonus(uint activator, AbilityDetail ability = null)
        {
            return GetHighResourceStatAdjustment(activator, StatType.HighFPAndStaminaAbilityDamageBonus,
                StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, ability);
        }

        public static int GetCostlyAbilityDamageBonus(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (!TryGetAbilityStaminaCostState(activator, ability, out var costState))
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageBonusSkillType));
            var minimumCost = Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageMinimumStaminaCost);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                costState.Cost < minimumCost)
            {
                return 0;
            }

            return Stat.GetStatAdjustment(activator, StatType.CostlyAbilityDamageBonus);
        }

        public static void ApplySuccessfulAbilityImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            bool firstHostileAbilityHitDamageBonusApplied,
            bool isFirstSuccessfulTarget,
            StatusEffectCategory appliedStatusCategories,
            bool allowsHitProcs = true,
            bool targetWasAttackingAlly = false)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || ability == null)
                return;
            if (WeaponImpactProfile.Get(activator)?.Activation.TryHitRiders(target) == false)
                return;

            Ability.RecordSuccessfulWeaponAbilityHit(activator, target, skillType);

            ApplyHostileAbilityPartyBuff(activator, ability, damage, statusApplied);

            if (firstHostileAbilityHitDamageBonusApplied)
                ApplyFirstHostileAbilityHitCount(activator, ability);
            if (allowsHitProcs && ability.IsHostileAbility && !ability.SuppressesSourceStatusStackRiders)
            {
                ApplySourceStatusStackEffects(activator, target);
            }

            ApplyHostileAbilityHitNextAutoAttackNoDelay(activator, ability);
            ApplySameTargetHostileAbilityHitEffects(activator, target, ability);
            ApplyAbilityStatusRiders(
                activator,
                target,
                ability,
                skillType,
                damage,
                statusApplied,
                primaryStatusEffect,
                additionalStatusEffects,
                isFirstSuccessfulTarget);
            ApplyStatusAppliedEffects(
                activator,
                target,
                appliedStatusCategories,
                targetWasAttackingAlly);
            ApplyAbilityTargetStatusEffects(activator, target, ability);
            ApplyRangedAbilityHitNearTargetEffects(activator, target, ability, skillType);
            ApplyCostlyAbilityHitEffects(activator, target, ability, skillType);
            ApplyAbilityDamageRiders(activator, target, ability, skillType, damageType, damage);
            ApplyAreaAbilityPulse(activator, target, ability, isFirstSuccessfulTarget);
            ApplyAreaAbilityTargetHitSequenceEffects(activator, target, ability, skillType);
        }

        private static void ApplyCostlyAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !TryGetAbilityStaminaCostState(activator, ability, out var costState))
            {
                return;
            }

            var staminaRestoreSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreSkillType));
            var statusSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusSkillType));
            var staminaRestoreMinimumCost = Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost);
            var statusMinimumCost = Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusMinimumStaminaCost);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.CostlyAbilityHitStaminaRestore);
            var exposedDuration = Stat.GetStatAdjustment(activator, StatType.CostlyAbilityExposedDurationSeconds);

            if (staminaRestore > 0 &&
                staminaRestoreMinimumCost > 0 &&
                costState.Cost >= staminaRestoreMinimumCost &&
                !costState.StaminaRestoreApplied &&
                SkillTypeMatchesOrGlobal(skillType, staminaRestoreSkillType))
            {
                RestoreAbilityHitStaminaFromStat(activator, StatType.CostlyAbilityHitStaminaRestore, staminaRestore, ability);
                costState.StaminaRestoreApplied = true;
            }

            if (exposedDuration > 0 &&
                statusMinimumCost > 0 &&
                costState.Cost >= statusMinimumCost &&
                SkillTypeMatchesOrGlobal(skillType, statusSkillType))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
            }
        }

        private static void ApplyRangedAbilityHitNearTargetEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null ||
                !ability.IsHostileAbility ||
                !IsRangedWeaponSkill(skillType) ||
                !GetIsObjectValid(target))
            {
                return;
            }

            var range = Stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetRangeMeters);
            var damageDealt = Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetDurationSeconds);
            var nameStrRef = Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef);
            var icon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectIcon));
            var cleanseTypes = GetStatusEffectCleanseTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes));
            var resistanceType = GetResistanceTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectResistanceType));
            if (range <= 0 ||
                damageDealt == 0 ||
                duration <= 0 ||
                nameStrRef <= 0 ||
                icon == EffectIconType.Invalid ||
                cleanseTypes == StatusEffectCleanseType.None ||
                resistanceType == ResistanceType.Invalid)
            {
                return;
            }

            if (GetDistanceBetween(activator, target) > range)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new DamageDealtAdjustmentStatusEffect(
                    damageDealt,
                    nameStrRef,
                    icon,
                    cleanseTypes,
                    resistanceType),
                duration,
                CombatDamageType.Physical);
        }

        private static void ApplySameTargetHostileAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var requiredCount = Stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityHitCountRequired);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityStaminaRestore);
            if (requiredCount <= 0 || staminaRestore <= 0)
                return;

            var key = (activator, target);
            _sameTargetHostileAbilityHitCounts.TryGetValue(key, out var count);
            count++;

            if (count < requiredCount)
            {
                _sameTargetHostileAbilityHitCounts[key] = count;
                return;
            }

            _sameTargetHostileAbilityHitCounts[key] = 0;
            RestoreAbilityHitStaminaFromStat(activator, StatType.SameTargetHostileAbilityStaminaRestore, staminaRestore, ability);
        }

        private static void ApplyHostileAbilityHitNextAutoAttackNoDelay(
            uint activator,
            AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var appliesToAllSkills = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills) > 0;
            var autoAttackSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelaySkillType));
            if (!appliesToAllSkills && autoAttackSkillType == SkillType.Invalid)
                return;

            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelayDurationSeconds);
            if (duration <= 0)
                return;

            if (appliesToAllSkills)
                GrantNextAutoAttackNoDelay(activator, duration);
            else
                GrantNextAutoAttackNoDelay(activator, autoAttackSkillType, duration);
        }

        private static void ApplyAbilityStatusRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            bool isFirstSuccessfulTarget)
        {
            switch (skillType)
            {
                case SkillType.HeavyVibroblade:
                    ApplyHeavyVibrobladeDefenseImpactRiders(activator, target, ability);
                    break;
                case SkillType.Force:
                    ApplyForceDarkImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
                case SkillType.Katar:
                    ApplyKatarVenomCurrentImpactRiders(activator, target);
                    break;
                case SkillType.Lightsaber:
                    ApplyLightsaberOffenseImpactRiders(activator, target, ability);
                    break;
                case SkillType.Pistol:
                    ApplyPistolSkirmisherImpactRiders(activator, target, ability);
                    break;
                case SkillType.Rifle:
                    ApplyRiflePacificationImpactRiders(activator, target);
                    break;
                case SkillType.Saberstaff:
                    ApplySaberstaffConduitImpactRiders(activator, target, ability);
                    ApplySaberstaffTempestImpactRiders(activator, target, ability);
                    break;
                case SkillType.Spear:
                    ApplySpearDamageImpactRiders(activator, target, ability);
                    ApplySpearDisablerImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
                case SkillType.Staff:
                    ApplyStaffCrusherImpactRiders(activator, target);
                    break;
                case SkillType.Throwing:
                    ApplyThrowingDeadeyeImpactRiders(activator, target, ability);
                    break;
                case SkillType.TwinBlade:
                    ApplyTwinBladeDuelistImpactRiders(activator, target, ability);
                    break;
                case SkillType.Vibroknife:
                    ApplyVibroknifeShadowImpactRiders(activator, target, ability);
                    ApplyVibroknifeSaboteurImpactRiders(activator, target, primaryStatusEffect, additionalStatusEffects);
                    break;
            }

            ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(activator, target, ability);
            ApplyGuardedHitNextSkillAbilityExposedStatus(activator, target, skillType);
        }

        private static void ApplyAbilityDamageRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage)
        {
            if (damage <= 0)
                return;

            ApplyFoggyMindResourceDrain(activator, target, ability);
            ApplyBleedingTargetAbilityBleedRefresh(activator, target, skillType);
            ApplyBleedingTargetAbilityBleedSpread(activator, target, skillType, damageType);
            ApplyBleedingTargetAbilitySplash(activator, target, skillType, damageType);
            ApplyAreaAbilityFragmentation(activator, target, ability, skillType, damageType);

            switch (skillType)
            {
                case SkillType.Katar when ability.IsSingleTargetAbility &&
                    Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentSecondStrikeDamageBonus) > 0:
                    var bonus = Stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentSecondStrikeDamageBonus);
                    ApplyTriggeredDamage(activator, target, bonus, damageType);

                    if (StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
                    {
                        StatusEffect.ApplyStatusEffect(activator, target, typeof(BleedStatusEffect), 30f, damageType);
                    }
                    break;
                case SkillType.Pistol when ability.IsSingleTargetAbility:
                    ApplyRicochetDamage(
                        activator,
                        target,
                        damageType,
                        StatType.PistolSkirmisherRicochetDamageBonus,
                        StatType.PistolSkirmisherRicochetMaximumTargets,
                        StatType.PistolSkirmisherRicochetCooldownSeconds);
                    break;
                case SkillType.Throwing:
                    if (ability.IsSingleTargetAbility)
                    {
                        ApplyRicochetDamage(
                            activator,
                            target,
                            damageType,
                            StatType.ThrowingDeadeyeRicochetDamageBonus,
                            StatType.ThrowingDeadeyeRicochetMaximumTargets);
                    }

                    if (ability.IsAreaAbility)
                    {
                        ApplyClusterStormDamage(activator, target, damageType);
                        ApplySaturationToss(activator, target);
                    }
                    break;
            }
        }

        private static void ApplyFoggyMindResourceDrain(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null ||
                !ability.IsHostileAbility ||
                !GetIsObjectValid(target) ||
                !StatusEffect.HasStatusEffect(target, typeof(FoggyMindStatusEffect)))
            {
                return;
            }

            var fpDrain = Stat.GetStatAdjustment(activator, StatType.AbilityResourceDrainFoggyMindFP);
            if (fpDrain > 0)
                ReduceFPFromStat(target, StatType.AbilityResourceDrainFoggyMindFP, fpDrain);

            var staminaDrain = Stat.GetStatAdjustment(activator, StatType.AbilityResourceDrainFoggyMindStamina);
            if (staminaDrain > 0)
                ReduceStaminaFromStat(target, StatType.AbilityResourceDrainFoggyMindStamina, staminaDrain);
        }

        private static IReadOnlyList<StatAdjustmentSource> GetAreaAbilityPulseSources(
            uint activator,
            AbilityDetail ability,
            bool isFirstSuccessfulTarget)
        {
            if (ability?.IsHostileAbility != true || !Ability.IsAreaImpact(activator, ability) || !isFirstSuccessfulTarget)
                return Array.Empty<StatAdjustmentSource>();

            return Stat.GetStatSources(activator, StatType.AreaAbilityPulseDamage);
        }

        private static void ApplyAreaAbilityPulse(
            uint activator,
            uint target,
            AbilityDetail ability,
            bool isFirstSuccessfulTarget)
        {
            var sources = GetAreaAbilityPulseSources(activator, ability, isFirstSuccessfulTarget);
            var index = 0;
            while (index < sources.Count &&
                   (sources[index][StatType.AreaAbilityPulseDamage] <= 0 || sources[index][StatType.AreaAbilityPulseRadiusMeters] <= 0))
                index++;
            if (index == sources.Count || !Ability.TryTriggerAreaAbilityPulse(activator))
                return;

            var location = GetLocation(target);
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_War_Cry), location);
            for (; index < sources.Count; index++)
            {
                var damage = sources[index][StatType.AreaAbilityPulseDamage];
                var radius = sources[index][StatType.AreaAbilityPulseRadiusMeters];
                if (damage <= 0 || radius <= 0)
                    continue;
                foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                             activator, location, radius, 0, target))
                    ApplyTriggeredDamage(activator, nearby, damage, CombatDamageType.Physical);
            }
        }

        private static void ApplyAreaAbilityFragmentation(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (ability == null || !Ability.IsAreaImpact(activator, ability) || !GetIsObjectValid(target))
                return;

            var damage = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationDamage);
            var duration = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationDurationSeconds);
            var pulse = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationPulseSeconds);
            if (damage <= 0 || duration <= 0 || pulse <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new FragmentationStatusEffect(damage, pulse),
                duration,
                damageType);
        }

        private static int GetAreaAbilityFragmentationStatAdjustment(uint activator, SkillType skillType, StatType statType)
        {
            // Persistent traits and temporary buffs can have different skill restrictions.
            // Filter each source before combining their fragmentation adjustments.
            var persistentSkill = GetSkillTypeFromStat(Stat.GetStatAdjustmentExcludingTemporaryModifiers(
                activator, StatType.AreaAbilityFragmentationSkillType));
            var temporarySkill = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                activator, StatType.AreaAbilityFragmentationSkillType));
            var persistentAdjustment = SkillTypeMatchesOrGlobal(skillType, persistentSkill)
                ? Stat.GetStatAdjustmentExcludingTemporaryModifiers(activator, statType)
                : 0;
            var temporaryAdjustment = SkillTypeMatchesOrGlobal(skillType, temporarySkill)
                ? TemporaryStatModifier.GetStatAdjustment(activator, statType)
                : 0;

            return Stat.AggregateStatAdjustment(statType, persistentAdjustment, temporaryAdjustment);
        }

        private static void ApplyRicochetDamage(
            uint activator,
            uint target,
            CombatDamageType damageType,
            StatType damageStatType,
            StatType maximumTargetsStatType,
            StatType cooldownStatType = StatType.Invalid)
        {
            var bonus = Stat.GetStatAdjustment(activator, damageStatType);
            var maximumTargets = Stat.GetStatAdjustment(activator, maximumTargetsStatType);
            if (bonus <= 0 || maximumTargets <= 0)
                return;

            if (cooldownStatType != StatType.Invalid)
            {
                var cooldown = Stat.GetStatAdjustment(activator, cooldownStatType);
                if (!TryUseStatTrigger(activator, damageStatType, cooldown))
                    return;
            }

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, maximumTargets, OBJECT_INVALID))
            {
                if (nearby == target)
                    continue;

                ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        private static void ApplyClusterStormDamage(
            uint activator,
            uint target,
            CombatDamageType damageType)
        {
            var bonus = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierClusterStormDamageBonus);
            var maximumTargets = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierClusterStormMaximumTargets);
            if (bonus <= 0 || maximumTargets <= 0 ||
                Ability.GetAbilityImpactSequence(activator)?.TryTriggerDamageRider(
                    nameof(StatType.ThrowingBombardierClusterStormDamageBonus)) != true)
                return;

            foreach (var nearby in SelectSecondaryDamageTargets(
                         AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, 0, target),
                         target, maximumTargets))
            {
                ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        private static void ApplyBleedingTargetAbilitySplash(
            uint activator, uint target, SkillType skillType, CombatDamageType damageType)
        {
            if (!StatusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Bleeding))
                return;

            foreach (var source in Ability.GetAbilityImpactStatSources(activator, StatType.BleedingTargetAbilitySplashDamage))
            {
                if (!CanTriggerBleedingTargetAbilitySplash(source, skillType, true, 1) ||
                    Ability.GetAbilityImpactSequence(activator)?.TryTriggerDamageRider(
                        source.GetModifierGroup(StatType.BleedingTargetAbilitySplashDamage)) != true ||
                    !CanTriggerBleedingTargetAbilitySplash(source, skillType, true, Random.D100(1)))
                    continue;

                foreach (var nearby in SelectSecondaryDamageTargets(
                             AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target),
                                 source[StatType.BleedingTargetAbilitySplashRadiusMeters], 0, target),
                             target, source[StatType.BleedingTargetAbilitySplashMaximumTargets]))
                    ApplyTriggeredDamage(activator, nearby, source[StatType.BleedingTargetAbilitySplashDamage], damageType, skillType);
            }
        }

        private static void ApplySaturationToss(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationToss) <= 0)
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossDurationSeconds);
            var damage = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossDamage);
            var pulse = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossPulseSeconds);
            if (duration <= 0 || damage <= 0 || pulse <= 0)
                return;

            var applied = StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new SaturationTossStatusEffect(damage, pulse),
                duration,
                CombatDamageType.Fire);
            if (!applied)
                return;

            ApplyEffectAtLocation(
                DurationType.Temporary,
                EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Fire),
                GetLocation(target),
                duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S), target);
        }

        private static void ApplyHeavyVibrobladeDefenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(activator, ability))
                return;

            var enmityBonus = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityEnmityBonus);
            if (enmityBonus > 0)
            {
                Enmity.ModifyEnmity(activator, target, enmityBonus);
            }

            if (AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType) &&
                Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityCrushingBlow) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(CrushingBlowStatusEffect), 30f, CombatDamageType.Physical);
            }
        }

        private static void ApplyForceDarkImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            if (Stat.GetStatAdjustment(activator, StatType.DarkManipulatorCollapseWill) <= 0 ||
                !AbilityAppliedAnyStatus(
                    primaryStatusEffect,
                    additionalStatusEffects,
                    typeof(NightmareField1StatusEffect),
                    typeof(EclipseOfResolve1StatusEffect)))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), 30f, CombatDamageType.Force);
            StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), 30f, CombatDamageType.Force);
        }

        private static void ApplyLightsaberOffenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var sunderDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSunderDurationSeconds);
            if (sunderDuration > 0)
            {
                ApplyLightsaberOffenseSunder(activator, target, sunderDuration);
            }

            var disorientedDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDisorientedDurationSeconds);
            if (disorientedDuration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }

            if (ability.IsSingleTargetAbility)
            {
                var disruptionDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSingleTargetForceDisruptionDurationSeconds);
                if (disruptionDuration > 0)
                {
                    StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), disruptionDuration, CombatDamageType.Force);
                }
            }

            ApplyLightsaberOffensePurify(activator, target);
        }

        private static void ApplyLightsaberOffenseSunder(uint activator, uint target, int duration)
        {
            const int DefensePenaltyPercent = 15;

            if (HasSunderPenaltyAtLeast(target, DefensePenaltyPercent))
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new SunderStatusEffect(DefensePenaltyPercent),
                duration,
                CombatDamageType.Physical);
        }

        private static void ApplyBleedingTargetAbilityBleedRefresh(uint attacker, uint defender, SkillType skillType)
        {
            if (!StatusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker))
                return;

            // Flurry Bleed and Ricochet Toss both declare the Throwing selector, so the
            // aggregated value would be a skill id that matches nothing. Only extend for the
            // sources that actually name this skill.
            var extensionSeconds = GetSkillSelectedStatAdjustment(
                attacker,
                skillType,
                StatType.AbilityDamageToBleedingTargetSkillType,
                StatType.BleedingTargetAbilityBleedDurationExtensionSeconds);
            if (extensionSeconds <= 0)
                return;

            StatusEffect.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
        }

        private static void ApplyBleedingTargetAbilityBleedSpread(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!damageType.IsPhysicalDamageType() ||
                !IsWeaponSkillType(skillType) ||
                !StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var chance = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadDurationSeconds);
            if (chance <= 0 || duration <= 0 || Random.D100(1) > chance)
                return;

            var maximumTargets = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadMaxTargets);
            maximumTargets = maximumTargets <= 0 ? 1 : maximumTargets;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         attacker,
                         GetLocation(defender),
                         5f,
                         maximumTargets,
                         defender))
            {
                StatusEffect.ApplyStatusEffect(attacker, nearby, typeof(BleedStatusEffect), duration, damageType);
            }
        }

        private static bool HasSunderPenaltyAtLeast(uint target, int defensePenaltyPercent)
        {
            return StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<SunderStatusEffect>()
                .Select(effect => Math.Abs(effect.StatGroup.Stats.GetValueOrDefault(StatType.PhysicalDefensePercentAdjustment)))
                .DefaultIfEmpty(0)
                .Max() >= defensePenaltyPercent;
        }

        private static void ApplyLightsaberOffensePurify(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.LightsaberOffensePurify) <= 0)
                return;

            var cooldown = Stat.GetStatAdjustment(activator, StatType.LightsaberOffensePurifyCooldownSeconds);
            if (!TryUseStatTrigger(activator, StatType.LightsaberOffensePurify, cooldown))
                return;

            var effect = StatusEffect.GetCreatureStatusEffects(activator)
                .GetAllEffects()
                .FirstOrDefault(IsTransferableHarmfulStatus);
            if (effect == null)
                return;

            var transferred = effect.Clone();
            StatusEffect.RemoveStatusEffect(activator, effect.GetType(), effect.Source, false);
            StatusEffect.ApplyStatusEffect(activator, target, transferred, 30f, CombatDamageType.Force);
        }

        private static void ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryTargetEnmityToSourceCategoryId))
            {
                return;
            }

            var enmity = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds);
            if (enmity <= 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new CoveringClawsStatusEffect(enmity),
                duration,
                CombatDamageType.Physical);
        }

        private static void ApplyKatarVenomCurrentImpactRiders(uint activator, uint target)
        {
            if (StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
            {
                SpreadPoisonFromTarget(activator, target);
            }
        }

        public static void ApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied)
        {
            if (!IsSuccessfulHostileAbilityImpact(ability, damage, statusApplied))
                return;

            var baseDamage = Stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffDamagePercent);
            var baseAccuracy = Stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffAccuracyPercent);
            var baseDuration = Stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffCooldownSeconds);
            var nameStrRef = Stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffNameStrRef);
            var icon = (EffectIconType)Stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffIcon);
            if ((baseDamage <= 0 && baseAccuracy <= 0) || baseDuration <= 0 || cooldown <= 0 ||
                nameStrRef <= 0 || icon == EffectIconType.Invalid)
                return;

            var sequence = Ability.GetAbilityImpactSequence(activator);
            if (sequence != null && !sequence.TryTriggerPartyBuff())
                return;
            if (!TryUseStatTrigger(activator, StatType.HostileAbilityPartyBuffDamagePercent, cooldown))
                return;

            var damagePercent = AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseDamage,
                Stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffMaximumDamagePercent));
            var accuracyPercent = AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseAccuracy,
                Stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffMaximumAccuracyPercent));
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyLeadershipCommandDurationBonus(activator, baseDuration);

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, activator, true, radius))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly,
                    new HostileAbilityPartyBuffStatusEffect(damagePercent, accuracyPercent, nameStrRef, icon), duration);
            }
        }

        private static void ApplyPistolSkirmisherImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var disorientedDuration = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherDisorientedDurationSeconds);
            if (disorientedDuration > 0 && (ability.IsAreaAbility || ability.MaxRange <= 5f))
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }
        }

        private static void ApplyRiflePacificationImpactRiders(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.RiflePacificationNeutralizingShot) > 0)
            {
                StatusEffect.RemoveFirstBeneficialCombatStatusEffect(target, false);
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), 30f, ResistanceType.Mind);
            }

            if (Stat.GetStatAdjustment(activator, StatType.RiflePacificationOverwatch) > 0)
            {
                ActionInterruption.ClearActions(target);
                StatusEffect.ApplyStatusEffect(activator, target, new FoggyMindStatusEffect(2), 30f, ResistanceType.Mind);
            }

            var pinningRank = Stat.GetStatAdjustment(activator, StatType.RiflePacificationPinningFireRank);
            if (pinningRank >= 2)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(KnockdownStatusEffect), 6f, ResistanceType.Trauma);
            }
            else if (pinningRank == 1)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), 30f, ResistanceType.Mind);
            }
        }

        private static void ApplySaberstaffConduitImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility ||
                Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitAreaConduitFlare) <= 0)
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        private static void ApplySaberstaffTempestImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsAreaAbility ||
                Stat.GetStatAdjustment(activator, StatType.SaberstaffTempestForceGyre) <= 0)
            {
                return;
            }

            var duration = Stat.GetStatAdjustment(activator, StatType.SaberstaffTempestForceGyreDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        private static void ApplySpearDamageImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (Stat.GetStatAdjustment(activator, StatType.SpearDamageBreachStrike) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(BreachStatusEffect), 30f, CombatDamageType.Physical);
            }

            if (ability.IsAreaAbility && Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefense) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(CrippledDefenseStatusEffect), 45f, CombatDamageType.Physical);
            }
        }

        private static void ApplySpearDisablerImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var appliesDisruption = AbilityAppliedAnyStatus(
                primaryStatusEffect,
                additionalStatusEffects,
                typeof(ForceDisruptionStatusEffect));
            var appliesSuppression = AbilityAppliedAnyStatus(
                primaryStatusEffect,
                additionalStatusEffects,
                typeof(ForceSuppressionStatusEffect),
                typeof(DisruptionFieldStatusEffect),
                typeof(ForceDisruptionStatusEffect));

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForceNullification) > 0 && appliesDisruption)
            {
                StatusEffect.ApplyStatusEffect(activator, target, new ForceDisruptionStatusEffect(), 30f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0 && appliesSuppression)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0 && appliesDisruption)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }
        }

        public static void ApplySpearDisablerSuppressionRiders(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target))
                return;

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }

            if (Stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }
        }

        private static void ApplyStaffCrusherImpactRiders(uint activator, uint target)
        {
            var duration = Stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDazedDurationSeconds);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        private static void ApplyThrowingDeadeyeImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                Stat.GetStatAdjustment(activator, StatType.ThrowingDeadeyeMarkingToss) <= 0)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(MarkingTossStatusEffect), 30f, CombatDamageType.Physical);
        }

        private static void ApplyTwinBladeDuelistImpactRiders(uint activator, uint target, AbilityDetail ability)
        {
            if (!AbilityMatchesReversalCutTrigger(activator, ability))
            {
                return;
            }

            var duration = TemporaryStatModifier.Consume(
                activator,
                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds,
                StatType.TwinBladeDuelistReversalCut);
            if (duration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        private static void ApplyVibroknifeShadowImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                Stat.GetStatAdjustment(activator, StatType.VibroknifeShadowMarkedForDeath) <= 0)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(MarkedForDeathStatusEffect), 30f, CombatDamageType.Physical);
        }

        private static void ApplyVibroknifeSaboteurImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var toxicCoatingRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
            if (toxicCoatingRank > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ToxinStatusEffect), 30f, CombatDamageType.Poison);
            }

            var sapRank = Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurSapVitalityRank);
            if (sapRank <= 0 ||
                !AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, StatusEffectCategory.Debuff) ||
                !TryUseStatTrigger(target, StatType.VibroknifeSaboteurSapVitalityRank, 6))
            {
                return;
            }

            ReduceStaminaFromStat(target, StatType.VibroknifeSaboteurSapVitalityRank, sapRank);
        }

        private static void ApplyStatusAppliedEffects(
            uint activator,
            uint target,
            StatusEffectCategory appliedStatusCategories,
            bool targetWasAttackingAlly)
        {
            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedRequiredCategory));
            if (requiredCategory == 0 ||
                (appliedStatusCategories & requiredCategory) == 0)
            {
                return;
            }

            var skillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilitySkillType));
            var damageBonus = Stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityDamageBonus);
            var criticalRate = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment);
            var window = Stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityWindowSeconds);
            GrantNextSkillAbilityBonuses(activator, skillType, damageBonus, criticalRate, window);

            var nextAttackDamage = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextAttackDamageBonus);
            var nextAttackWindow = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextAttackWindowSeconds);
            if (nextAttackWindow > 0)
            {
                var wasArmed = TemporaryStatModifier.GetStatAdjustment(
                    activator,
                    StatType.NextAttackStatusAppliedDMGBonus,
                    StatType.NextAttackStatusAppliedDMGBonus) != 0;
                ShowReadyCue(activator, wasArmed, nextAttackDamage, ReadyCue.Damage("Charged Blows", nextAttackDamage));
            }

            GrantStatusAppliedNextAttackDamageBonus(activator, nextAttackDamage, nextAttackWindow);

            ApplyStatusAppliedSelfEffects(activator);
            ApplyStatusAppliedTargetEffects(activator, target);
            if (targetWasAttackingAlly)
                ApplyStatusAppliedTargetAttackingAllyEnmity(activator, target);
        }

        /// <summary>
        /// Whether the target is attacking one of the activator's allies, checked only when the activator
        /// has a rider that needs it.
        /// </summary>
        public static bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment) <= 0)
                return false;

            return IsAttackingAllyOf(target, activator);
        }

        /// <summary>
        /// Whether the attacker is attacking a party member of the creature other than the creature itself.
        /// Uses the attacker's current attack target, or, while it is casting or moving, the creature its
        /// enmity table has it attacking.
        /// </summary>
        public static bool IsAttackingAllyOf(uint attacker, uint creature)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(creature))
                return false;

            var attackTarget = GetAttackTarget(attacker);
            if (!GetIsObjectValid(attackTarget))
                attackTarget = Enmity.GetHighestEnmityAttackTarget(attacker);

            return GetIsObjectValid(attackTarget) &&
                   attackTarget != creature &&
                   Party.IsInParty(creature, attackTarget);
        }

        private static void ApplyStatusAppliedTargetAttackingAllyEnmity(uint activator, uint target)
        {
            var enmity = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds);
            if (enmity <= 0 || duration <= 0 || !GetIsObjectValid(target))
                return;

            // Challenge is the shared "more Enmity toward its source" status. Never replace a stronger one
            // the activator already put on this target, such as Guardian's Challenge.
            var existing = StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .Where(effect => effect is ChallengeEnmityStatusEffect &&
                                 effect.Source == activator &&
                                 !effect.IsFlaggedForRemoval)
                .Select(effect => effect.StatGroup.Stats.GetValueOrDefault(StatType.EnmityToStatusSourcePercentAdjustment))
                .DefaultIfEmpty(0)
                .Max();
            if (existing > enmity)
                return;

            StatusEffect.ApplyStatusEffect(activator, target, new ChallengeEnmityStatusEffect(enmity), duration);
        }

        private static void ApplyStatusAppliedSelfEffects(uint activator)
        {
            var duration = Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDurationSeconds);
            if (duration <= 0)
                return;

            ReplaceTemporaryStat(
                activator,
                Stat.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection),
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfAttackDeflection),
                duration,
                StatType.StatusAppliedSelfAttackDeflection);
            ReplaceTemporaryStat(
                activator,
                StatType.PhysicalDefensePercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfDefensePercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.EvasionPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEvasionPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEvasionPercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.ForceAttackPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfForceAttackPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfForceAttackPercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.AttackDelayReductionPercent,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfHastePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfHastePercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.EnmityPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEnmityPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEnmityPercentAdjustment);

            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfStaminaRestore);
            if (staminaRestore > 0)
                RestoreAbilityHitStaminaFromStat(activator, StatType.StatusAppliedSelfStaminaRestore, staminaRestore);
        }

        private static void ApplyStatusAppliedTargetEffects(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetDurationSeconds);
            if (duration <= 0)
                return;

            ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment);
            ReplaceTemporaryStat(
                target,
                StatType.AccuracyPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetAccuracyPercentAdjustment),
                duration,
                StatType.StatusAppliedTargetAccuracyPercentAdjustment);
        }

        private static void ReplaceTemporaryStat(
            uint target,
            StatType statType,
            int amount,
            int durationSeconds,
            StatType group)
        {
            if (statType == StatType.Invalid || amount == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(target, statType, amount, durationSeconds, group);
        }

        private static void ApplyAbilityTargetStatusEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility || !GetIsObjectValid(target))
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusRequiredCategory));
            if (requiredCategory == 0 || !StatusEffect.HasStatusEffectCategory(target, requiredCategory))
                return;

            var physicalDefense = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityTargetStatusDurationSeconds);
            ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                physicalDefense,
                duration,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
        }

        public static void ApplyStatusAppliedTargetStaminaDrain(
            uint activator,
            uint target,
            StatusEffectCategory appliedCategories)
        {
            if (!GetIsObjectValid(activator) ||
                !GetIsObjectValid(target) ||
                activator == target)
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetStaminaDrainRequiredCategory));
            var staminaDrain = Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetStaminaDrain);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetStaminaDrainCooldownSeconds);
            if (requiredCategory == 0 ||
                staminaDrain <= 0 ||
                cooldown <= 0 ||
                (appliedCategories & requiredCategory) == 0 ||
                !TryUseStatTrigger(activator, StatType.StatusAppliedTargetStaminaDrain, cooldown))
            {
                return;
            }

            var staminaBefore = Stat.GetCurrentStamina(target);
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(target, StatType.StatusAppliedTargetStaminaDrain);
            Stat.ReduceStamina(target, staminaDrain);
            var staminaDrained = Math.Max(0, staminaBefore - Stat.GetCurrentStamina(target));
            FloatingTextStringOnCreature(
                ColorToken.Combat($"-{staminaDrained} STM"),
                target,
                false);
        }

        private static void ApplyAreaAbilityTargetHitSequenceEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !Ability.IsAreaImpact(activator, ability) || !GetIsObjectValid(target))
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityTargetHitSequenceSkillType));
            var requiredCount = Stat.GetStatAdjustment(activator, StatType.AreaAbilityTargetHitSequenceCountRequired);
            var windowSeconds = Stat.GetStatAdjustment(activator, StatType.AreaAbilityTargetHitSequenceWindowSeconds);
            var exposedDuration = Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds);
            if (!SkillTypeMatches(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                windowSeconds <= 0 ||
                exposedDuration <= 0)
            {
                return;
            }

            var key = (activator, target);
            var now = Clock.UtcNow;
            var count = 1;
            if (_areaAbilityTargetHitSequences.TryGetValue(key, out var state) &&
                (now - state.LastHit).TotalSeconds <= windowSeconds)
            {
                count = state.Count + 1;
            }

            if (count >= requiredCount)
            {
                _areaAbilityTargetHitSequences.Remove(key);
                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
                return;
            }

            _areaAbilityTargetHitSequences[key] = new TargetHitSequenceState
            {
                Count = count,
                LastHit = now
            };
        }

        private static void ApplyGuardedHitNextSkillAbilityExposedStatus(uint activator, uint target, SkillType skillType)
        {
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return;

            var duration = TemporaryStatModifier.Consume(
                activator,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            if (duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), duration, CombatDamageType.Physical);
        }

        public static void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || summary == null || summary.ImpactedTargetCount <= 0)
                return;

            ApplyAreaHitRewards(activator, summary);

            switch (summary.SkillType)
            {
                case SkillType.Throwing:
                    ApplyThrowingAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.Saberstaff:
                    ApplyAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.Spear:
                    ApplySpearAbilityImpactEffects(activator, summary);
                    break;
            }
        }

        private static void ApplyThrowingAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var staminaThreshold = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestore);
            if (staminaThreshold > 0 && staminaRestore > 0 && summary.ImpactedTargetCount >= staminaThreshold)
            {
                RestoreAbilityHitStaminaFromStat(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, staminaRestore, summary.Ability);
            }

            var attackPerTarget = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentPerTarget);
            var attackDuration = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackDurationSeconds);
            var attackMax = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentMax);
            if (attackPerTarget > 0 && attackDuration > 0 && attackMax > 0)
            {
                ApplyStackingAttackBoost(activator, attackPerTarget, attackMax, attackDuration, summary.ImpactedTargetCount);
            }
        }

        private static void ApplyAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var restoreThreshold = Stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsResourceRestoreThreshold);
            var fpRestore = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFPRestore);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.AreaAbilityStaminaRestore);
            var restoreCooldown = Stat.GetStatAdjustment(activator, StatType.AreaAbilityResourceRestoreCooldownSeconds);
            if (restoreThreshold > 0 &&
                summary.ImpactedTargetCount >= restoreThreshold &&
                TryUseStatTrigger(activator, StatType.AreaAbilityFPRestore, restoreCooldown))
            {
                var restoredFP = fpRestore > 0
                    ? RestoreFPFromStat(activator, StatType.AreaAbilityFPRestore, fpRestore)
                    : 0;
                if (restoredFP > 0)
                    ApplyAbilityRestoredFPEffects(activator);

                var restoredStamina = staminaRestore > 0
                    ? RestoreAbilityHitStaminaFromStat(activator, StatType.AreaAbilityStaminaRestore, staminaRestore, summary.Ability)
                    : 0;

                if (restoredFP > 0 && restoredStamina > 0)
                    ApplyAbilityRestoredBothResourcesEffects(activator);
            }

            var buffThreshold = Stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsBuffThreshold);
            if (buffThreshold <= 0 || summary.ImpactedTargetCount < buffThreshold)
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.AreaAbilityBuffDurationSeconds);
            if (duration <= 0)
                return;

            var haste = Stat.GetStatAdjustment(activator, StatType.AreaAbilityHastePercentAdjustment);
            if (haste != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    duration,
                    StatType.AttackDelayReductionPercent);
            }

            var deflection = Stat.GetStatAdjustment(activator, StatType.AreaAbilityAttackDeflection);
            if (deflection != 0)
            {
                var source = Stat.GetStatTypeDeflectionSource(StatType.AreaAbilityAttackDeflection);
                var deflectionStatType = Stat.GetGrantedDeflectionStatType(StatType.AreaAbilityAttackDeflection);
                if (deflectionStatType == StatType.Invalid)
                    return;

                TemporaryStatModifier.Replace(
                    activator,
                    deflectionStatType,
                    deflection,
                    duration,
                    StatType.AreaAbilityAttackDeflection);
                ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
            }
        }

        private static void ApplySpearAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var minimumTargets = Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseMinimumTargets);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseStaminaRestore);
            if (minimumTargets > 0 &&
                staminaRestore > 0 &&
                summary.ImpactedTargetCount >= minimumTargets)
            {
                RestoreAbilityHitStaminaFromStat(activator, StatType.SpearDamageCripplingDefenseStaminaRestore, staminaRestore, summary.Ability);
            }
        }

        private static void ApplyAreaHitRewards(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            ApplyAreaHitHaste(activator, summary.ImpactedTargetCount);
            foreach (var source in Stat.GetStatSources(activator, StatType.AreaHitStaminaRestorePerTarget))
            {
                var perTarget = source[StatType.AreaHitStaminaRestorePerTarget];
                var maximum = source[StatType.AreaHitStaminaRestoreMaximum];
                if (perTarget > 0 && maximum > 0)
                    RestoreAbilityHitStaminaFromStat(activator, StatType.AreaHitStaminaRestorePerTarget,
                        Math.Min(maximum, perTarget * summary.ImpactedTargetCount), summary.Ability);
            }
        }

        public static void ApplyAreaHitHaste(uint activator, int impactedTargetCount)
        {
            foreach (var source in Stat.GetStatSources(activator, StatType.AreaAbilityHastePerStack))
            {
                var threshold = source[StatType.AreaAbilityHasteStackMinimumTargets];
                if (threshold <= 0 || impactedTargetCount < threshold)
                    continue;

                var stacks = source[StatType.AreaAbilityHasteStacksPerAdditionalTarget] > 0
                    ? impactedTargetCount - threshold + 1
                    : 1;
                TemporaryStatModifier.AddCapped(activator, StatType.AttackDelayReductionPercent,
                    source[StatType.AreaAbilityHastePerStack],
                    source[StatType.AreaAbilityHasteStackDurationSeconds],
                    source[StatType.AreaAbilityHasteStackMaximumPercent],
                    source.GetModifierGroup(StatType.AreaAbilityHastePerStack), stacks);
            }
        }

        private static void ApplyStackingAttackBoost(
            uint activator,
            int attackPercent,
            int maxAttackPercent,
            int durationSeconds,
            int requestedStacks)
        {
            TemporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attackPercent,
                durationSeconds,
                maxAttackPercent,
                StatType.ThrowingAreaAbilityAttackPercentPerTarget,
                requestedStacks);
        }

        private static void ForgetImpactState(uint creature)
        {
            foreach (var key in _sameTargetHostileAbilityHitCounts.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _sameTargetHostileAbilityHitCounts.Remove(key);
            }
            foreach (var key in _areaAbilityTargetHitSequences.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _areaAbilityTargetHitSequences.Remove(key);
            }
        }
    }
}
