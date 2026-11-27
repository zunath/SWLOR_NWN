using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
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
    /// The area abilities and the damage riders of a hit: the Foggy Mind drain, the area pulse, the fragmentation, the ricochet, cluster
    /// storm and saturation toss, the bleeding target splash, bleed refresh and bleed spread, and the area ability hit sequence. The area
    /// ability hit sequences are this instance's own table.
    /// </summary>
    public class CombatAbilityAreaImpactService : ICombatAbilityAreaImpactService
    {
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
        private readonly ICombatTriggeredDamageService _triggeredDamage;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly IStatusEffectDurationService _statusEffectDuration;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityImpactState _impactState;

        public CombatAbilityAreaImpactService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatTriggeredDamageService triggeredDamage,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            IStatusEffectDurationService statusEffectDuration,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityImpactState impactState)
        {
            _queries = queries;
            _resources = resources;
            _triggeredDamage = triggeredDamage;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _statusEffectDuration = statusEffectDuration;
            _temporaryStatModifier = temporaryStatModifier;
            _impactState = impactState;
        }

        public void ApplyAbilityDamageRiders(
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
                    _triggeredDamage.ApplyTriggeredDamage(activator, target, bonus, damageType);

                    if (_statusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
                    {
                        _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(BleedStatusEffect), 30f, damageType);
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
            if (ability?.IsHostileAbility != true || !_impactState.IsAreaImpact(activator, ability) || !isFirstSuccessfulTarget)
                return Array.Empty<StatAdjustmentSource>();

            return _stat.GetStatSources(activator, StatType.AreaAbilityPulseDamage);
        }

        public void ApplyAreaAbilityPulse(
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
            if (index == sources.Count || !_impactState.TryTriggerAreaAbilityPulse(activator))
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
                    _triggeredDamage.ApplyTriggeredDamage(activator, nearby, damage, CombatDamageType.Physical);
            }
        }

        private void ApplyAreaAbilityFragmentation(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (ability == null || !_impactState.IsAreaImpact(activator, ability) || !GetIsObjectValid(target))
                return;

            var damage = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationDamage);
            var duration = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationDurationSeconds);
            var pulse = GetAreaAbilityFragmentationStatAdjustment(activator, skillType, StatType.AreaAbilityFragmentationPulseSeconds);
            if (damage <= 0 || duration <= 0 || pulse <= 0)
                return;

            _statusEffectApplication.ApplyStatusEffect(
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

                _triggeredDamage.ApplyTriggeredDamage(activator, nearby, bonus, damageType);
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
                _impactState.GetAbilityImpactSequence(activator)?.TryTriggerDamageRider(
                    nameof(StatType.ThrowingBombardierClusterStormDamageBonus)) != true)
                return;

            foreach (var nearby in SelectSecondaryDamageTargets(
                         AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, 0, target),
                         target, maximumTargets))
            {
                _triggeredDamage.ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        private void ApplyBleedingTargetAbilitySplash(
            uint activator, uint target, SkillType skillType, CombatDamageType damageType)
        {
            if (!_statusEffect.HasStatusEffectCategory(target, StatusEffectCategory.Bleeding))
                return;

            foreach (var source in _impactState.GetAbilityImpactStatSources(activator, StatType.BleedingTargetAbilitySplashDamage, _stat))
            {
                if (!CanTriggerBleedingTargetAbilitySplash(source, skillType, true, 1) ||
                    _impactState.GetAbilityImpactSequence(activator)?.TryTriggerDamageRider(
                        source.GetModifierGroup(StatType.BleedingTargetAbilitySplashDamage)) != true ||
                    !CanTriggerBleedingTargetAbilitySplash(source, skillType, true, Random.D100(1)))
                    continue;

                foreach (var nearby in SelectSecondaryDamageTargets(
                             AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target),
                                 source[StatType.BleedingTargetAbilitySplashRadiusMeters], 0, target),
                             target, source[StatType.BleedingTargetAbilitySplashMaximumTargets]))
                    _triggeredDamage.ApplyTriggeredDamage(activator, nearby, source[StatType.BleedingTargetAbilitySplashDamage], damageType, skillType);
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

            var applied = _statusEffectApplication.ApplyStatusEffect(
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

            _statusEffectDuration.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
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
                _statusEffectApplication.ApplyStatusEffect(attacker, nearby, typeof(BleedStatusEffect), duration, damageType);
            }
        }

        public void ApplyAreaAbilityTargetHitSequenceEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !_impactState.IsAreaImpact(activator, ability) || !GetIsObjectValid(target))
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
                _statusEffectApplication.ApplyStatusEffect(
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

        public void ForgetAreaAbilityTargetHitSequences(uint creature)
        {
            foreach (var key in _areaAbilityTargetHitSequences.Keys.Where(x => x.Item1 == creature || x.Item2 == creature).ToList())
            {
                _areaAbilityTargetHitSequences.Remove(key);
            }
        }

        /// <inheritdoc/>
        public void ForgetCreatureState(uint creature, CombatForgetPhase phase)
        {
            switch (phase)
            {
                case CombatForgetPhase.AbilityImpact:
                    ForgetAreaAbilityTargetHitSequences(creature);
                    break;
            }
        }
    }
}
