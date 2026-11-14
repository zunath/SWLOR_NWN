using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability impact part of <c>Combat</c>: the base damage bonuses of an ability impact, the riders of a successful
    /// ability hit on its target (in the order <see cref="ApplySuccessfulAbilityImpactRiders"/> runs them), the Spear
    /// Disabler suppression riders, the status-applied stamina drain, and the effects of a finished impact by the number of
    /// targets it hit. The same-target hit counts and the area ability hit sequences are this instance's own tables.
    /// </summary>
    public class CombatAbilityImpactService : ICombatAbilityImpactService
    {
        private readonly Dictionary<(uint, uint), int> _sameTargetHostileAbilityHitCounts = new();
        private readonly Dictionary<(uint, uint), TargetHitSequenceState> _areaAbilityTargetHitSequences = new();

        // The area ability hit sequences; CombatActivityService has its own copy for the First Strike stacks.
        private sealed class TargetHitSequenceState
        {
            public int Count { get; init; }
            public DateTime LastHit { get; init; }
            public DateTime? RechargeAvailableAt { get; init; }
        }

        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatHitEffectsService _hitEffects;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityService _ability;
        private readonly IEnmityService _enmity;
        private readonly IPartyService _party;
        private readonly ICombatRecorderService _combatRecorder;

        public CombatAbilityImpactService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatActivityService combatActivity,
            ICombatHitEffectsService hitEffects,
            ICombatDamageTakenService damageTaken,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityService ability,
            IEnmityService enmity,
            IPartyService party,
            ICombatRecorderService combatRecorder)
        {
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _combatActivity = combatActivity;
            _hitEffects = hitEffects;
            _damageTaken = damageTaken;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _ability = ability;
            _enmity = enmity;
            _party = party;
            _combatRecorder = combatRecorder;
        }

        public int GetAbilityImpactBaseDamageBonus(
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
                        bonus += _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseAreaDamageBonus);
                    }

                    if (ability.IsSingleTargetAbility &&
                        GetIsObjectValid(target) &&
                        _statusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Debuff))
                    {
                        bonus += _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDebuffedTargetDamageBonus);
                    }
                    break;
                case SkillType.Vibroknife when ability.IsHostileAbility:
                    var toxicCoatingRank = _stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
                    if (toxicCoatingRank > 0)
                    {
                        bonus += toxicCoatingRank >= 2 ? 22 : 10;
                    }
                    break;
                case SkillType.Staff when ability.IsHostileAbility:
                    bonus += _stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDamageBonus);
                    break;
                case SkillType.Saberstaff when ability.IsHostileAbility:
                    bonus += _stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareDamageBonus);
                    break;
                case SkillType.TwinBlade when ability.IsHostileAbility &&
                    _queries.AbilityMatchesReversalCutTrigger(activator, ability):
                    bonus += _temporaryStatModifier.Consume(
                        activator,
                        StatType.TwinBladeDuelistReversalCutDamageBonus,
                        StatType.TwinBladeDuelistReversalCut);
                    break;
            }

            if (ability.IsHostileAbility)
            {
                bonus += _combatActivity.GetFirstHostileAbilityHitDamageBonus(activator, ability);
                bonus += _combatActivity.GetDirectDamageToStatusCategoryOrStealthBonus(activator, target);
            }

            bonus += _damageTaken.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(activator, skillType);

            if (ability.IsHostileAbility)
                bonus += GetHighResourceAbilityDamageBonus(activator, ability);

            return bonus;
        }

        public int GetHighResourceAbilityDamageBonus(uint activator, AbilityDetail ability = null)
        {
            return _resources.GetHighResourceStatAdjustment(activator, StatType.HighFPAndStaminaAbilityDamageBonus,
                StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, ability);
        }

        public int GetCostlyAbilityDamageBonus(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (!_resources.TryGetAbilityStaminaCostState(activator, ability, out var costState))
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageBonusSkillType));
            var minimumCost = _stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityDamageMinimumStaminaCost);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                costState.Cost < minimumCost)
            {
                return 0;
            }

            return _stat.GetStatAdjustment(activator, StatType.CostlyAbilityDamageBonus);
        }

        public void ApplySuccessfulAbilityImpactRiders(
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

            _ability.RecordSuccessfulWeaponAbilityHit(activator, target, skillType);

            ApplyHostileAbilityPartyBuff(activator, ability, damage, statusApplied);

            if (firstHostileAbilityHitDamageBonusApplied)
                _combatActivity.ApplyFirstHostileAbilityHitCount(activator, ability);
            if (allowsHitProcs && ability.IsHostileAbility && !ability.SuppressesSourceStatusStackRiders)
            {
                _hitEffects.ApplySourceStatusStackEffects(activator, target);
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

        private void ApplyCostlyAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !_resources.TryGetAbilityStaminaCostState(activator, ability, out var costState))
            {
                return;
            }

            var staminaRestoreSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreSkillType));
            var statusSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusSkillType));
            var staminaRestoreMinimumCost = _stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost);
            var statusMinimumCost = _stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityStatusMinimumStaminaCost);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.CostlyAbilityHitStaminaRestore);
            var exposedDuration = _stat.GetStatAdjustment(activator, StatType.CostlyAbilityExposedDurationSeconds);

            if (staminaRestore > 0 &&
                staminaRestoreMinimumCost > 0 &&
                costState.Cost >= staminaRestoreMinimumCost &&
                !costState.StaminaRestoreApplied &&
                SkillTypeMatchesOrGlobal(skillType, staminaRestoreSkillType))
            {
                _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.CostlyAbilityHitStaminaRestore, staminaRestore, ability);
                costState.StaminaRestoreApplied = true;
            }

            if (exposedDuration > 0 &&
                statusMinimumCost > 0 &&
                costState.Cost >= statusMinimumCost &&
                SkillTypeMatchesOrGlobal(skillType, statusSkillType))
            {
                _statusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
            }
        }

        private void ApplyRangedAbilityHitNearTargetEffects(
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

            var range = _stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetRangeMeters);
            var damageDealt = _stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.RangedAbilityHitNearTargetDurationSeconds);
            var nameStrRef = _stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef);
            var icon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectIcon));
            var cleanseTypes = GetStatusEffectCleanseTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes));
            var resistanceType = GetResistanceTypeFromStat(_stat.GetStatAdjustment(
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

            _statusEffect.ApplyStatusEffect(
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

        private void ApplySameTargetHostileAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var requiredCount = _stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityHitCountRequired);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.SameTargetHostileAbilityStaminaRestore);
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
            _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.SameTargetHostileAbilityStaminaRestore, staminaRestore, ability);
        }

        private void ApplyHostileAbilityHitNextAutoAttackNoDelay(
            uint activator,
            AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var appliesToAllSkills = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills) > 0;
            var autoAttackSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelaySkillType));
            if (!appliesToAllSkills && autoAttackSkillType == SkillType.Invalid)
                return;

            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityHitNextAutoAttackNoDelayDurationSeconds);
            if (duration <= 0)
                return;

            if (appliesToAllSkills)
                _attackTiming.GrantNextAutoAttackNoDelay(activator, duration);
            else
                _attackTiming.GrantNextAutoAttackNoDelay(activator, autoAttackSkillType, duration);
        }

        private void ApplyAbilityStatusRiders(
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

        private void ApplyAbilityDamageRiders(
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
                    _stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentSecondStrikeDamageBonus) > 0:
                    var bonus = _stat.GetStatAdjustment(activator, StatType.KatarVenomCurrentSecondStrikeDamageBonus);
                    _hitEffects.ApplyTriggeredDamage(activator, target, bonus, damageType);

                    if (_statusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
                    {
                        _statusEffect.ApplyStatusEffect(activator, target, typeof(BleedStatusEffect), 30f, damageType);
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

        private void ApplyFoggyMindResourceDrain(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null ||
                !ability.IsHostileAbility ||
                !GetIsObjectValid(target) ||
                !_statusEffect.HasStatusEffect(target, typeof(FoggyMindStatusEffect)))
            {
                return;
            }

            var fpDrain = _stat.GetStatAdjustment(activator, StatType.AbilityResourceDrainFoggyMindFP);
            if (fpDrain > 0)
                _resources.ReduceFPFromStat(target, StatType.AbilityResourceDrainFoggyMindFP, fpDrain);

            var staminaDrain = _stat.GetStatAdjustment(activator, StatType.AbilityResourceDrainFoggyMindStamina);
            if (staminaDrain > 0)
                _resources.ReduceStaminaFromStat(target, StatType.AbilityResourceDrainFoggyMindStamina, staminaDrain);
        }

        private IReadOnlyList<StatAdjustmentSource> GetAreaAbilityPulseSources(
            uint activator,
            AbilityDetail ability,
            bool isFirstSuccessfulTarget)
        {
            if (ability?.IsHostileAbility != true || !_ability.IsAreaImpact(activator, ability) || !isFirstSuccessfulTarget)
                return Array.Empty<StatAdjustmentSource>();

            return _stat.GetStatSources(activator, StatType.AreaAbilityPulseDamage);
        }

        private void ApplyAreaAbilityPulse(
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
            if (index == sources.Count || !_ability.TryTriggerAreaAbilityPulse(activator))
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
                    _hitEffects.ApplyTriggeredDamage(activator, nearby, damage, CombatDamageType.Physical);
            }
        }

        private void ApplyAreaAbilityFragmentation(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (ability == null || !_ability.IsAreaImpact(activator, ability) || !GetIsObjectValid(target))
                return;

            var damage = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationDamage);
            var duration = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationDurationSeconds);
            var pulse = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationPulseSeconds);
            if (damage <= 0 || duration <= 0 || pulse <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                activator,
                target,
                new FragmentationStatusEffect(damage, pulse),
                duration,
                damageType);
        }

        private int GetAreaAbilityFragmentationStatAdjustment(uint activator, SkillType skillType, StatType statType)
        {
            // Persistent traits and temporary buffs can have different skill restrictions.
            // Filter each source before combining their fragmentation adjustments.
            var persistentSkill = GetSkillTypeFromStat(_stat.GetStatAdjustmentExcludingTemporaryModifiers(
                activator, StatType.AreaAbilityFragmentationSkillType));
            var temporarySkill = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                activator, StatType.AreaAbilityFragmentationSkillType));
            var persistentAdjustment = SkillTypeMatchesOrGlobal(skillType, persistentSkill)
                ? _stat.GetStatAdjustmentExcludingTemporaryModifiers(activator, statType)
                : 0;
            var temporaryAdjustment = SkillTypeMatchesOrGlobal(skillType, temporarySkill)
                ? _temporaryStatModifier.GetStatAdjustment(activator, statType)
                : 0;

            return _stat.AggregateStatAdjustment(statType, persistentAdjustment, temporaryAdjustment);
        }

        private void ApplyRicochetDamage(
            uint activator,
            uint target,
            CombatDamageType damageType,
            StatType damageStatType,
            StatType maximumTargetsStatType,
            StatType cooldownStatType = StatType.Invalid)
        {
            var bonus = _stat.GetStatAdjustment(activator, damageStatType);
            var maximumTargets = _stat.GetStatAdjustment(activator, maximumTargetsStatType);
            if (bonus <= 0 || maximumTargets <= 0)
                return;

            if (cooldownStatType != StatType.Invalid)
            {
                var cooldown = _stat.GetStatAdjustment(activator, cooldownStatType);
                if (!_resources.TryUseStatTrigger(activator, damageStatType, cooldown))
                    return;
            }

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, maximumTargets, OBJECT_INVALID))
            {
                if (nearby == target)
                    continue;

                _hitEffects.ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        private void ApplyClusterStormDamage(
            uint activator,
            uint target,
            CombatDamageType damageType)
        {
            var bonus = _stat.GetStatAdjustment(activator, StatType.ThrowingBombardierClusterStormDamageBonus);
            var maximumTargets = _stat.GetStatAdjustment(activator, StatType.ThrowingBombardierClusterStormMaximumTargets);
            if (bonus <= 0 || maximumTargets <= 0 ||
                _ability.GetAbilityImpactSequence(activator)?.TryTriggerDamageRider(
                    nameof(StatType.ThrowingBombardierClusterStormDamageBonus)) != true)
                return;

            foreach (var nearby in SelectSecondaryDamageTargets(
                         AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, 0, target),
                         target, maximumTargets))
            {
                _hitEffects.ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        private void ApplyBleedingTargetAbilitySplash(
            uint activator, uint target, SkillType skillType, CombatDamageType damageType)
        {
            if (!_statusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Bleeding))
                return;

            foreach (var source in _ability.GetAbilityImpactStatSources(activator, StatType.BleedingTargetAbilitySplashDamage))
            {
                if (!CanTriggerBleedingTargetAbilitySplash(source, skillType, true, 1) ||
                    _ability.GetAbilityImpactSequence(activator)?.TryTriggerDamageRider(
                        source.GetModifierGroup(StatType.BleedingTargetAbilitySplashDamage)) != true ||
                    !CanTriggerBleedingTargetAbilitySplash(source, skillType, true, Random.D100(1)))
                    continue;

                foreach (var nearby in SelectSecondaryDamageTargets(
                             AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target),
                                 source[StatType.BleedingTargetAbilitySplashRadiusMeters], 0, target),
                             target, source[StatType.BleedingTargetAbilitySplashMaximumTargets]))
                    _hitEffects.ApplyTriggeredDamage(activator, nearby, source[StatType.BleedingTargetAbilitySplashDamage], damageType, skillType);
            }
        }

        private void ApplySaturationToss(uint activator, uint target)
        {
            if (_stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationToss) <= 0)
                return;

            var duration = _stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossDurationSeconds);
            var damage = _stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossDamage);
            var pulse = _stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossPulseSeconds);
            if (duration <= 0 || damage <= 0 || pulse <= 0)
                return;

            var applied = _statusEffect.ApplyStatusEffect(
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

        private void ApplyHeavyVibrobladeDefenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(activator, ability))
                return;

            var enmityBonus = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityEnmityBonus);
            if (enmityBonus > 0)
            {
                _enmity.ModifyEnmity(activator, target, enmityBonus);
            }

            if (_queries.AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType) &&
                _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityCrushingBlow) > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(CrushingBlowStatusEffect), 30f, CombatDamageType.Physical);
            }
        }

        private void ApplyForceDarkImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            if (_stat.GetStatAdjustment(activator, StatType.DarkManipulatorCollapseWill) <= 0 ||
                !_queries.AbilityAppliedAnyStatus(
                    primaryStatusEffect,
                    additionalStatusEffects,
                    typeof(NightmareField1StatusEffect),
                    typeof(EclipseOfResolve1StatusEffect)))
            {
                return;
            }

            _statusEffect.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), 30f, CombatDamageType.Force);
            _statusEffect.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), 30f, CombatDamageType.Force);
        }

        private void ApplyLightsaberOffenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var sunderDuration = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSunderDurationSeconds);
            if (sunderDuration > 0)
            {
                ApplyLightsaberOffenseSunder(activator, target, sunderDuration);
            }

            var disorientedDuration = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDisorientedDurationSeconds);
            if (disorientedDuration > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }

            if (ability.IsSingleTargetAbility)
            {
                var disruptionDuration = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSingleTargetForceDisruptionDurationSeconds);
                if (disruptionDuration > 0)
                {
                    _statusEffect.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), disruptionDuration, CombatDamageType.Force);
                }
            }

            ApplyLightsaberOffensePurify(activator, target);
        }

        private void ApplyLightsaberOffenseSunder(uint activator, uint target, int duration)
        {
            const int DefensePenaltyPercent = 15;

            if (HasSunderPenaltyAtLeast(target, DefensePenaltyPercent))
                return;

            _statusEffect.ApplyStatusEffect(
                activator,
                target,
                new SunderStatusEffect(DefensePenaltyPercent),
                duration,
                CombatDamageType.Physical);
        }

        private void ApplyBleedingTargetAbilityBleedRefresh(uint attacker, uint defender, SkillType skillType)
        {
            if (!_statusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker))
                return;

            // Flurry Bleed and Ricochet Toss both declare the Throwing selector, so the
            // aggregated value would be a skill id that matches nothing. Only extend for the
            // sources that actually name this skill.
            var extensionSeconds = _queries.GetSkillSelectedStatAdjustment(
                attacker,
                skillType,
                StatType.AbilityDamageToBleedingTargetSkillType,
                StatType.BleedingTargetAbilityBleedDurationExtensionSeconds);
            if (extensionSeconds <= 0)
                return;

            _statusEffect.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
        }

        private void ApplyBleedingTargetAbilityBleedSpread(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!damageType.IsPhysicalDamageType() ||
                !IsWeaponSkillType(skillType) ||
                !_statusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var chance = _stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadChance);
            var duration = _stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadDurationSeconds);
            if (chance <= 0 || duration <= 0 || Random.D100(1) > chance)
                return;

            var maximumTargets = _stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadMaxTargets);
            maximumTargets = maximumTargets <= 0 ? 1 : maximumTargets;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         attacker,
                         GetLocation(defender),
                         5f,
                         maximumTargets,
                         defender))
            {
                _statusEffect.ApplyStatusEffect(attacker, nearby, typeof(BleedStatusEffect), duration, damageType);
            }
        }

        private bool HasSunderPenaltyAtLeast(uint target, int defensePenaltyPercent)
        {
            return _statusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<SunderStatusEffect>()
                .Select(effect => Math.Abs(effect.StatGroup.Stats.GetValueOrDefault(StatType.PhysicalDefensePercentAdjustment)))
                .DefaultIfEmpty(0)
                .Max() >= defensePenaltyPercent;
        }

        private void ApplyLightsaberOffensePurify(uint activator, uint target)
        {
            if (_stat.GetStatAdjustment(activator, StatType.LightsaberOffensePurify) <= 0)
                return;

            var cooldown = _stat.GetStatAdjustment(activator, StatType.LightsaberOffensePurifyCooldownSeconds);
            if (!_resources.TryUseStatTrigger(activator, StatType.LightsaberOffensePurify, cooldown))
                return;

            var effect = _statusEffect.GetCreatureStatusEffects(activator)
                .GetAllEffects()
                .FirstOrDefault(IsTransferableHarmfulStatus);
            if (effect == null)
                return;

            var transferred = effect.Clone();
            _statusEffect.RemoveStatusEffect(activator, effect.GetType(), effect.Source, false);
            _statusEffect.ApplyStatusEffect(activator, target, transferred, 30f, CombatDamageType.Force);
        }

        private void ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryTargetEnmityToSourceCategoryId))
            {
                return;
            }

            var enmity = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds);
            if (enmity <= 0 || duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                activator,
                target,
                new CoveringClawsStatusEffect(enmity),
                duration,
                CombatDamageType.Physical);
        }

        private void ApplyKatarVenomCurrentImpactRiders(uint activator, uint target)
        {
            if (_statusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
            {
                _hitEffects.SpreadPoisonFromTarget(activator, target);
            }
        }

        public void ApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied)
        {
            if (!IsSuccessfulHostileAbilityImpact(ability, damage, statusApplied))
                return;

            var baseDamage = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffDamagePercent);
            var baseAccuracy = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffAccuracyPercent);
            var baseDuration = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffCooldownSeconds);
            var nameStrRef = _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffNameStrRef);
            var icon = (EffectIconType)_stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffIcon);
            if ((baseDamage <= 0 && baseAccuracy <= 0) || baseDuration <= 0 || cooldown <= 0 ||
                nameStrRef <= 0 || icon == EffectIconType.Invalid)
                return;

            var sequence = _ability.GetAbilityImpactSequence(activator);
            if (sequence != null && !sequence.TryTriggerPartyBuff())
                return;
            if (!_resources.TryUseStatTrigger(activator, StatType.HostileAbilityPartyBuffDamagePercent, cooldown))
                return;

            var damagePercent = AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseDamage,
                _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffMaximumDamagePercent));
            var accuracyPercent = AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseAccuracy,
                _stat.GetStatAdjustment(activator, StatType.HostileAbilityPartyBuffMaximumAccuracyPercent));
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = LeadershipAbilityEffects.ApplyLeadershipCommandDurationBonus(activator, baseDuration);

            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, activator, true, radius))
            {
                _statusEffect.ApplyStatusEffect(activator, friendly,
                    new HostileAbilityPartyBuffStatusEffect(damagePercent, accuracyPercent, nameStrRef, icon), duration);
            }
        }

        private void ApplyPistolSkirmisherImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var disorientedDuration = _stat.GetStatAdjustment(activator, StatType.PistolSkirmisherDisorientedDurationSeconds);
            if (disorientedDuration > 0 && (ability.IsAreaAbility || ability.MaxRange <= 5f))
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }
        }

        private void ApplyRiflePacificationImpactRiders(uint activator, uint target)
        {
            if (_stat.GetStatAdjustment(activator, StatType.RiflePacificationNeutralizingShot) > 0)
            {
                _statusEffect.RemoveFirstBeneficialCombatStatusEffect(target, false);
                _statusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), 30f, ResistanceType.Mind);
            }

            if (_stat.GetStatAdjustment(activator, StatType.RiflePacificationOverwatch) > 0)
            {
                ActionInterruption.ClearActions(target);
                _statusEffect.ApplyStatusEffect(activator, target, new FoggyMindStatusEffect(2), 30f, ResistanceType.Mind);
            }

            var pinningRank = _stat.GetStatAdjustment(activator, StatType.RiflePacificationPinningFireRank);
            if (pinningRank >= 2)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(KnockdownStatusEffect), 6f, ResistanceType.Trauma);
            }
            else if (pinningRank == 1)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), 30f, ResistanceType.Mind);
            }
        }

        private void ApplySaberstaffConduitImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility ||
                _stat.GetStatAdjustment(activator, StatType.SaberstaffConduitAreaConduitFlare) <= 0)
            {
                return;
            }

            var duration = _stat.GetStatAdjustment(activator, StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds);
            if (duration > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        private void ApplySaberstaffTempestImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsAreaAbility ||
                _stat.GetStatAdjustment(activator, StatType.SaberstaffTempestForceGyre) <= 0)
            {
                return;
            }

            var duration = _stat.GetStatAdjustment(activator, StatType.SaberstaffTempestForceGyreDurationSeconds);
            if (duration > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        private void ApplySpearDamageImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (_stat.GetStatAdjustment(activator, StatType.SpearDamageBreachStrike) > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(BreachStatusEffect), 30f, CombatDamageType.Physical);
            }

            if (ability.IsAreaAbility && _stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefense) > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(CrippledDefenseStatusEffect), 45f, CombatDamageType.Physical);
            }
        }

        private void ApplySpearDisablerImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var appliesDisruption = _queries.AbilityAppliedAnyStatus(
                primaryStatusEffect,
                additionalStatusEffects,
                typeof(ForceDisruptionStatusEffect));
            var appliesSuppression = _queries.AbilityAppliedAnyStatus(
                primaryStatusEffect,
                additionalStatusEffects,
                typeof(ForceSuppressionStatusEffect),
                typeof(DisruptionFieldStatusEffect),
                typeof(ForceDisruptionStatusEffect));

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerForceNullification) > 0 && appliesDisruption)
            {
                _statusEffect.ApplyStatusEffect(activator, target, new ForceDisruptionStatusEffect(), 30f, CombatDamageType.Force);
            }

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0 && appliesSuppression)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0 && appliesDisruption)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }
        }

        public void ApplySpearDisablerSuppressionRiders(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target))
                return;

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }
        }

        private void ApplyStaffCrusherImpactRiders(uint activator, uint target)
        {
            var duration = _stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDazedDurationSeconds);
            if (duration > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        private void ApplyThrowingDeadeyeImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                _stat.GetStatAdjustment(activator, StatType.ThrowingDeadeyeMarkingToss) <= 0)
            {
                return;
            }

            _statusEffect.ApplyStatusEffect(activator, target, typeof(MarkingTossStatusEffect), 30f, CombatDamageType.Physical);
        }

        private void ApplyTwinBladeDuelistImpactRiders(uint activator, uint target, AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesReversalCutTrigger(activator, ability))
            {
                return;
            }

            var duration = _temporaryStatModifier.Consume(
                activator,
                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds,
                StatType.TwinBladeDuelistReversalCut);
            if (duration > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        private void ApplyVibroknifeShadowImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                _stat.GetStatAdjustment(activator, StatType.VibroknifeShadowMarkedForDeath) <= 0)
            {
                return;
            }

            _statusEffect.ApplyStatusEffect(activator, target, typeof(MarkedForDeathStatusEffect), 30f, CombatDamageType.Physical);
        }

        private void ApplyVibroknifeSaboteurImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var toxicCoatingRank = _stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
            if (toxicCoatingRank > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, target, typeof(ToxinStatusEffect), 30f, CombatDamageType.Poison);
            }

            var sapRank = _stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurSapVitalityRank);
            if (sapRank <= 0 ||
                !_queries.AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, StatusEffectCategory.Debuff) ||
                !_resources.TryUseStatTrigger(target, StatType.VibroknifeSaboteurSapVitalityRank, 6))
            {
                return;
            }

            _resources.ReduceStaminaFromStat(target, StatType.VibroknifeSaboteurSapVitalityRank, sapRank);
        }

        private void ApplyStatusAppliedEffects(
            uint activator,
            uint target,
            StatusEffectCategory appliedStatusCategories,
            bool targetWasAttackingAlly)
        {
            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedRequiredCategory));
            if (requiredCategory == 0 ||
                (appliedStatusCategories & requiredCategory) == 0)
            {
                return;
            }

            var skillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilitySkillType));
            var damageBonus = _stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityDamageBonus);
            var criticalRate = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment);
            var window = _stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityWindowSeconds);
            _attackTiming.GrantNextSkillAbilityBonuses(activator, skillType, damageBonus, criticalRate, window);

            var nextAttackDamage = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextAttackDamageBonus);
            var nextAttackWindow = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextAttackWindowSeconds);
            if (nextAttackWindow > 0)
            {
                var wasArmed = _temporaryStatModifier.GetStatAdjustment(
                    activator,
                    StatType.NextAttackStatusAppliedDMGBonus,
                    StatType.NextAttackStatusAppliedDMGBonus) != 0;
                _attackTiming.ShowReadyCue(activator, wasArmed, nextAttackDamage, ReadyCue.Damage("Charged Blows", nextAttackDamage));
            }

            _attackTiming.GrantStatusAppliedNextAttackDamageBonus(activator, nextAttackDamage, nextAttackWindow);

            ApplyStatusAppliedSelfEffects(activator);
            ApplyStatusAppliedTargetEffects(activator, target);
            if (targetWasAttackingAlly)
                ApplyStatusAppliedTargetAttackingAllyEnmity(activator, target);
        }

        /// <summary>
        /// Whether the target is attacking one of the activator's allies, checked only when the activator
        /// has a rider that needs it.
        /// </summary>
        public bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target)
        {
            if (_stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment) <= 0)
                return false;

            return IsAttackingAllyOf(target, activator);
        }

        /// <summary>
        /// Whether the attacker is attacking a party member of the creature other than the creature itself.
        /// Uses the attacker's current attack target, or, while it is casting or moving, the creature its
        /// enmity table has it attacking.
        /// </summary>
        public bool IsAttackingAllyOf(uint attacker, uint creature)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(creature))
                return false;

            var attackTarget = GetAttackTarget(attacker);
            if (!GetIsObjectValid(attackTarget))
                attackTarget = _enmity.GetHighestEnmityAttackTarget(attacker);

            return GetIsObjectValid(attackTarget) &&
                   attackTarget != creature &&
                   _party.IsInParty(creature, attackTarget);
        }

        private void ApplyStatusAppliedTargetAttackingAllyEnmity(uint activator, uint target)
        {
            var enmity = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds);
            if (enmity <= 0 || duration <= 0 || !GetIsObjectValid(target))
                return;

            // Challenge is the shared "more Enmity toward its source" status. Never replace a stronger one
            // the activator already put on this target, such as Guardian's Challenge.
            var existing = _statusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .Where(effect => effect is ChallengeEnmityStatusEffect &&
                                 effect.Source == activator &&
                                 !effect.IsFlaggedForRemoval)
                .Select(effect => effect.StatGroup.Stats.GetValueOrDefault(StatType.EnmityToStatusSourcePercentAdjustment))
                .DefaultIfEmpty(0)
                .Max();
            if (existing > enmity)
                return;

            _statusEffect.ApplyStatusEffect(activator, target, new ChallengeEnmityStatusEffect(enmity), duration);
        }

        private void ApplyStatusAppliedSelfEffects(uint activator)
        {
            var duration = _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDurationSeconds);
            if (duration <= 0)
                return;

            ReplaceTemporaryStat(
                activator,
                _stat.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection),
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfAttackDeflection),
                duration,
                StatType.StatusAppliedSelfAttackDeflection);
            ReplaceTemporaryStat(
                activator,
                StatType.PhysicalDefensePercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfDefensePercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.EvasionPercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEvasionPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEvasionPercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.ForceAttackPercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfForceAttackPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfForceAttackPercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.AttackDelayReductionPercent,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfHastePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfHastePercentAdjustment);
            ReplaceTemporaryStat(
                activator,
                StatType.EnmityPercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEnmityPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEnmityPercentAdjustment);

            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfStaminaRestore);
            if (staminaRestore > 0)
                _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.StatusAppliedSelfStaminaRestore, staminaRestore);
        }

        private void ApplyStatusAppliedTargetEffects(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            var duration = _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetDurationSeconds);
            if (duration <= 0)
                return;

            ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment);
            ReplaceTemporaryStat(
                target,
                StatType.AccuracyPercentAdjustment,
                _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetAccuracyPercentAdjustment),
                duration,
                StatType.StatusAppliedTargetAccuracyPercentAdjustment);
        }

        private void ReplaceTemporaryStat(
            uint target,
            StatType statType,
            int amount,
            int durationSeconds,
            StatType group)
        {
            if (statType == StatType.Invalid || amount == 0 || durationSeconds <= 0)
                return;

            _temporaryStatModifier.Replace(target, statType, amount, durationSeconds, group);
        }

        private void ApplyAbilityTargetStatusEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility || !GetIsObjectValid(target))
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusRequiredCategory));
            if (requiredCategory == 0 || !_statusEffect.HasStatusEffectCategory(target, requiredCategory))
                return;

            var physicalDefense = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityTargetStatusDurationSeconds);
            ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                physicalDefense,
                duration,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
        }

        public void ApplyStatusAppliedTargetStaminaDrain(
            uint activator,
            uint target,
            StatusEffectCategory appliedCategories)
        {
            if (!GetIsObjectValid(activator) ||
                !GetIsObjectValid(target) ||
                activator == target)
                return;

            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedTargetStaminaDrainRequiredCategory));
            var staminaDrain = _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetStaminaDrain);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetStaminaDrainCooldownSeconds);
            if (requiredCategory == 0 ||
                staminaDrain <= 0 ||
                cooldown <= 0 ||
                (appliedCategories & requiredCategory) == 0 ||
                !_resources.TryUseStatTrigger(activator, StatType.StatusAppliedTargetStaminaDrain, cooldown))
            {
                return;
            }

            var staminaBefore = _stat.GetCurrentStamina(target);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(target, StatType.StatusAppliedTargetStaminaDrain);
            _stat.ReduceStamina(target, staminaDrain);
            var staminaDrained = Math.Max(0, staminaBefore - _stat.GetCurrentStamina(target));
            FloatingTextStringOnCreature(
                ColorToken.Combat($"-{staminaDrained} STM"),
                target,
                false);
        }

        private void ApplyAreaAbilityTargetHitSequenceEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !_ability.IsAreaImpact(activator, ability) || !GetIsObjectValid(target))
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityTargetHitSequenceSkillType));
            var requiredCount = _stat.GetStatAdjustment(activator, StatType.AreaAbilityTargetHitSequenceCountRequired);
            var windowSeconds = _stat.GetStatAdjustment(activator, StatType.AreaAbilityTargetHitSequenceWindowSeconds);
            var exposedDuration = _stat.GetStatAdjustment(
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
                _statusEffect.ApplyStatusEffect(
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

        private void ApplyGuardedHitNextSkillAbilityExposedStatus(uint activator, uint target, SkillType skillType)
        {
            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return;

            var duration = _temporaryStatModifier.Consume(
                activator,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            if (duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), duration, CombatDamageType.Physical);
        }

        public void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
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

        private void ApplyThrowingAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var staminaThreshold = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestore);
            if (staminaThreshold > 0 && staminaRestore > 0 && summary.ImpactedTargetCount >= staminaThreshold)
            {
                _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, staminaRestore, summary.Ability);
            }

            var attackPerTarget = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentPerTarget);
            var attackDuration = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackDurationSeconds);
            var attackMax = _stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentMax);
            if (attackPerTarget > 0 && attackDuration > 0 && attackMax > 0)
            {
                ApplyStackingAttackBoost(activator, attackPerTarget, attackMax, attackDuration, summary.ImpactedTargetCount);
            }
        }

        private void ApplyAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var restoreThreshold = _stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsResourceRestoreThreshold);
            var fpRestore = _stat.GetStatAdjustment(activator, StatType.AreaAbilityFPRestore);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.AreaAbilityStaminaRestore);
            var restoreCooldown = _stat.GetStatAdjustment(activator, StatType.AreaAbilityResourceRestoreCooldownSeconds);
            if (restoreThreshold > 0 &&
                summary.ImpactedTargetCount >= restoreThreshold &&
                _resources.TryUseStatTrigger(activator, StatType.AreaAbilityFPRestore, restoreCooldown))
            {
                var restoredFP = fpRestore > 0
                    ? _resources.RestoreFPFromStat(activator, StatType.AreaAbilityFPRestore, fpRestore)
                    : 0;
                if (restoredFP > 0)
                    _resources.ApplyAbilityRestoredFPEffects(activator);

                var restoredStamina = staminaRestore > 0
                    ? _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.AreaAbilityStaminaRestore, staminaRestore, summary.Ability)
                    : 0;

                if (restoredFP > 0 && restoredStamina > 0)
                    _resources.ApplyAbilityRestoredBothResourcesEffects(activator);
            }

            var buffThreshold = _stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsBuffThreshold);
            if (buffThreshold <= 0 || summary.ImpactedTargetCount < buffThreshold)
                return;

            var duration = _stat.GetStatAdjustment(activator, StatType.AreaAbilityBuffDurationSeconds);
            if (duration <= 0)
                return;

            var haste = _stat.GetStatAdjustment(activator, StatType.AreaAbilityHastePercentAdjustment);
            if (haste != 0)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    duration,
                    StatType.AttackDelayReductionPercent);
            }

            var deflection = _stat.GetStatAdjustment(activator, StatType.AreaAbilityAttackDeflection);
            if (deflection != 0)
            {
                var source = _stat.GetStatTypeDeflectionSource(StatType.AreaAbilityAttackDeflection);
                var deflectionStatType = _stat.GetGrantedDeflectionStatType(StatType.AreaAbilityAttackDeflection);
                if (deflectionStatType == StatType.Invalid)
                    return;

                _temporaryStatModifier.Replace(
                    activator,
                    deflectionStatType,
                    deflection,
                    duration,
                    StatType.AreaAbilityAttackDeflection);
                _damageTaken.ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
            }
        }

        private void ApplySpearAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var minimumTargets = _stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseMinimumTargets);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseStaminaRestore);
            if (minimumTargets > 0 &&
                staminaRestore > 0 &&
                summary.ImpactedTargetCount >= minimumTargets)
            {
                _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.SpearDamageCripplingDefenseStaminaRestore, staminaRestore, summary.Ability);
            }
        }

        private void ApplyAreaHitRewards(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            ApplyAreaHitHaste(activator, summary.ImpactedTargetCount);
            foreach (var source in _stat.GetStatSources(activator, StatType.AreaHitStaminaRestorePerTarget))
            {
                var perTarget = source[StatType.AreaHitStaminaRestorePerTarget];
                var maximum = source[StatType.AreaHitStaminaRestoreMaximum];
                if (perTarget > 0 && maximum > 0)
                    _resources.RestoreAbilityHitStaminaFromStat(activator, StatType.AreaHitStaminaRestorePerTarget,
                        Math.Min(maximum, perTarget * summary.ImpactedTargetCount), summary.Ability);
            }
        }

        public void ApplyAreaHitHaste(uint activator, int impactedTargetCount)
        {
            foreach (var source in _stat.GetStatSources(activator, StatType.AreaAbilityHastePerStack))
            {
                var threshold = source[StatType.AreaAbilityHasteStackMinimumTargets];
                if (threshold <= 0 || impactedTargetCount < threshold)
                    continue;

                var stacks = source[StatType.AreaAbilityHasteStacksPerAdditionalTarget] > 0
                    ? impactedTargetCount - threshold + 1
                    : 1;
                _temporaryStatModifier.AddCapped(activator, StatType.AttackDelayReductionPercent,
                    source[StatType.AreaAbilityHastePerStack],
                    source[StatType.AreaAbilityHasteStackDurationSeconds],
                    source[StatType.AreaAbilityHasteStackMaximumPercent],
                    source.GetModifierGroup(StatType.AreaAbilityHastePerStack), stacks);
            }
        }

        private void ApplyStackingAttackBoost(
            uint activator,
            int attackPercent,
            int maxAttackPercent,
            int durationSeconds,
            int requestedStacks)
        {
            _temporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attackPercent,
                durationSeconds,
                maxAttackPercent,
                StatType.ThrowingAreaAbilityAttackPercentPerTarget,
                requestedStacks);
        }

        public void ForgetImpactState(uint creature)
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
