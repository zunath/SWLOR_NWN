using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability activation part of <c>Combat</c>: the riders that run when a creature activates an ability (recast and
    /// next-ability grants, the evasion, deflection, movement, Force Attack and defense riders, the weapon line riders, the
    /// hostile ability sequence and resource restores) in their fixed order, the queued weapon ability use, and the riders of
    /// an HP-cost ability. The hostile ability sequence per activator is this instance's own table.
    /// </summary>
    public class CombatAbilityActivationService : ICombatAbilityActivationService
    {
        private readonly Dictionary<uint, HostileAbilitySequenceState> _hostileAbilitySequenceStates = new();

        private sealed class HostileAbilitySequenceState
        {
            public FeatType LastFeat { get; init; }
            public DateTime LastUse { get; init; }
        }

        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityService _ability;
        private readonly IPerkService _perk;
        private readonly IRecastService _recast;
        private readonly IEnmityService _enmity;
        private readonly ICombatRecorderService _combatRecorder;

        public CombatAbilityActivationService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatActivityService combatActivity,
            ICombatDamageTakenService damageTaken,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityService ability,
            IPerkService perk,
            IRecastService recast,
            IEnmityService enmity,
            ICombatRecorderService combatRecorder)
        {
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _combatActivity = combatActivity;
            _damageTaken = damageTaken;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _ability = ability;
            _perk = perk;
            _recast = recast;
            _enmity = enmity;
            _combatRecorder = combatRecorder;
        }

        public void ApplyAbilityActivatedEffects(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || ability == null || summary == null)
                return;

            ApplyAbilityUsedRecastReduction(activator, ability);
            ApplyAbilityUsedNextSkillAutoAttackDamageBonus(activator, ability);
            ApplyAbilityUsedNextSkillFPCostAdjustment(activator, ability);
            ApplyAbilityUsedMasterAbilityHitChance(activator);
            ApplyForceFPCostActivatedEffects(activator, ability);

            var skillType = ResolveActivatedAbilitySkillType(activator, ability, summary);
            var isSingleTargetAbility = summary.IsSingleTargetAbility ||
                summary.SkillType == SkillType.Invalid &&
                ability.IsHostileAbility &&
                ability.IsSingleTargetAbility;

            ApplyAbilityUsedSkillEvasion(activator, ability);
            ApplyHostileAbilityUsedEvasion(activator, ability, skillType);
            ApplyCostlyAbilityUsedEvasion(activator, ability, skillType);
            ApplyAbilityUsedSkillRangedDeflection(activator, ability);
            ApplyAbilityUsedMovementSpeed(activator, ability, skillType);
            ApplyAbilityUsedSkillAttackDeflection(activator, ability);
            ApplyAbilityUsedPerkCategoryAttackDeflection(activator, ability);
            ApplySingleTargetAbilityUsedAttackDeflection(activator, ability, isSingleTargetAbility);
            var isAreaAbility = _ability.IsAreaImpact(summary, ability);
            ApplyAreaAbilityUsedEvasion(activator, ability, skillType, isAreaAbility);
            ApplyAreaAbilityUsedRewards(activator, ability, isAreaAbility);
            ApplyHostileAbilityForceAttack(activator, ability);
            ApplyHostileAbilityFPSpendForceAttack(activator, ability);
            ApplyAbilityUsedNearbyAllyDefense(activator);
            ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(activator, ability);
            ApplyAbilityUsedPerkCategorySelfDefense(activator, ability);
            ApplyHostileAbilityUsedAttackAdjustment(activator, ability);
            ApplyAbilityActivatedRiders(activator, target, ability, skillType);
            ApplyHostileAbilitySequenceEffects(activator, feat, ability);
            ApplyHostileAbilityResourceRestoreEffects(activator, ability);

            if (ability.ActivationType != AbilityActivationType.Weapon)
                _combatActivity.TrackCombatAbilityUse(activator, ability);
        }

        public void TrackQueuedWeaponAbilityUse(uint activator, AbilityDetail ability)
        {
            _combatActivity.TrackCombatAbilityUse(activator, ability);
        }

        private SkillType ResolveActivatedAbilitySkillType(
            uint activator,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            return summary.SkillType != SkillType.Invalid
                ? summary.SkillType
                : _queries.GetAbilitySkillType(activator, ability);
        }

        private void ApplyAreaAbilityUsedRewards(uint activator, AbilityDetail ability, bool isAreaAbility)
        {
            if (ability?.IsHostileAbility != true || !isAreaAbility)
                return;

            var grantedDeflection = false;
            foreach (var source in _stat.GetStatSources(activator, StatType.AreaAbilityUsedAttackDeflection))
            {
                var deflection = source[StatType.AreaAbilityUsedAttackDeflection];
                var duration = source[StatType.AreaAbilityUsedAttackDeflectionDurationSeconds];
                if (deflection <= 0 || duration <= 0)
                    continue;

                _temporaryStatModifier.Replace(activator,
                    _stat.GetGrantedDeflectionStatType(StatType.AreaAbilityUsedAttackDeflection),
                    deflection, duration, source.GetModifierGroup(StatType.AreaAbilityUsedAttackDeflection));
                grantedDeflection = true;
            }
            if (grantedDeflection)
                _damageTaken.ApplyAbilityGrantedAttackDeflectionEffects(activator,
                    _stat.GetStatTypeDeflectionSource(StatType.AreaAbilityUsedAttackDeflection));

            var restore = _stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedFPRestore);
            if (restore > 0 && _resources.RestoreFPFromStat(activator, StatType.AreaAbilityUsedFPRestore, restore) > 0)
                _resources.ApplyAbilityRestoredFPEffects(activator);
        }

        private void ApplyAreaAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType,
            bool isAreaAbility)
        {
            if (ability?.IsHostileAbility != true || !isAreaAbility)
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return;

            var evasion = _stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedEvasionPercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedEvasionDurationSeconds);
            if (evasion == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                StatType.EvasionPercentAdjustment,
                evasion,
                duration,
                StatType.AreaAbilityUsedEvasionPercentAdjustment);
        }

        private void ApplyHostileAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return;

            ApplyAbilityUsedEvasion(
                activator,
                StatType.HostileAbilityUsedEvasionPercentAdjustment,
                StatType.HostileAbilityUsedEvasionDurationSeconds);
        }

        private void ApplyCostlyAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !_resources.TryGetAbilityStaminaCostState(activator, ability, out var costState))
            {
                return;
            }

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType));
            var minimumCost = _stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityUsedEvasionMinimumStaminaCost);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                costState.Cost < minimumCost)
            {
                return;
            }

            ApplyAbilityUsedEvasion(
                activator,
                StatType.CostlyAbilityUsedEvasionPercentAdjustment,
                StatType.CostlyAbilityUsedEvasionDurationSeconds);
        }

        private void ApplyAbilityUsedMovementSpeed(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            if (!_queries.AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    skillType,
                    StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType,
                    StatType.AbilityUsedMovementSpeedRangedOnly))
            {
                return;
            }

            var movementSpeed = _stat.GetStatAdjustment(activator, StatType.AbilityUsedMovementSpeedPercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedMovementSpeedDurationSeconds);
            if (movementSpeed == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                StatType.MovementSpeedPercentAdjustment,
                movementSpeed,
                duration,
                StatType.AbilityUsedMovementSpeedPercentAdjustment);
        }

        private void ApplyHostileAbilityForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = _stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentPerStack);
            var duration = _stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackDurationSeconds);
            var maximum = _stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentMax);
            if (forceAttack == 0 || duration <= 0 || maximum <= 0)
                return;

            var current = _statusEffect.GetStatusEffect(
                activator,
                typeof(HostileAbilityForceAttackStatusEffect)) as HostileAbilityForceAttackStatusEffect;
            var total = Math.Min(maximum, (current?.ForceAttack ?? 0) + forceAttack);

            _statusEffect.ApplyStatusEffect(
                activator,
                activator,
                new HostileAbilityForceAttackStatusEffect(total),
                duration);
        }

        /// <summary>
        /// Grants stacking Force Attack when the activated hostile ability costs at least the
        /// configured minimum FP. Drives the Lightsaber Severance "Overpower" trait.
        /// </summary>
        private void ApplyHostileAbilityFPSpendForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackPercent);
            var duration = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds);
            var maximum = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent);
            var minimumFPCost = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost);
            if (forceAttack <= 0 || duration <= 0 || maximum <= 0 || minimumFPCost <= 0)
                return;

            var fpCost = ability.Requirements
                .OfType<AbilityRequirementFP>()
                .Sum(x => x.RequiredFP);
            if (fpCost < minimumFPCost)
                return;

            _temporaryStatModifier.AddCapped(
                activator,
                StatType.ForceAttackPercentAdjustment,
                forceAttack,
                duration,
                maximum,
                StatType.HostileAbilityFPSpendForceAttackPercent,
                1,
                refreshExistingStacks: true);
        }

        private void ApplyHostileAbilityUsedAttackAdjustment(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var attack = _stat.GetStatAdjustment(activator, StatType.HostileAbilityUsedAttackPercentAdjustment);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds);
            var maximum = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum);
            if (attack <= 0 || duration <= 0 || maximum <= 0)
                return;

            _temporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attack,
                duration,
                maximum,
                StatType.HostileAbilityUsedAttackPercentAdjustment,
                1,
                refreshExistingStacks: true);

            var currentTotal = _temporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.AttackPercentAdjustment,
                StatType.HostileAbilityUsedAttackPercentAdjustment);
            if (currentTotal > 0)
            {
                _statusEffect.ApplyStatusEffect(
                    activator,
                    activator,
                    new ButchersTempoStatusEffect(currentTotal),
                    duration);
            }
        }

        private void ApplyAbilityUsedNearbyAllyDefense(uint activator)
        {
            var defense = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
            var forceDefense = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds);
            if (duration <= 0 || defense == 0 && forceDefense == 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f, false))
            {
                if (defense != 0)
                {
                    _temporaryStatModifier.Replace(
                        friendly,
                        StatType.PhysicalDefensePercentAdjustment,
                        defense,
                        duration,
                        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
                }

                if (forceDefense != 0)
                {
                    _temporaryStatModifier.Replace(
                        friendly,
                        StatType.ForceDefensePercentAdjustment,
                        forceDefense,
                        duration,
                        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
                }
            }
        }

        private void ApplyAbilityActivatedRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null)
                return;

            switch (skillType)
            {
                case SkillType.HeavyVibroblade:
                    ApplyHeavyVibrobladeActivatedEffects(activator, target, ability);
                    break;
                case SkillType.BeastMastery:
                    ApplyBeastBalancedAbilityRecovery(activator, ability);
                    break;
                case SkillType.Vibroknife:
                    ApplyVibroknifeShadowActivatedEffects(activator, ability);
                    break;
                case SkillType.Pistol:
                    ApplyPistolSkirmisherActivatedEffects(activator, target, ability);
                    break;
                case SkillType.Lightsaber:
                    ApplyLightsaberOffenseActivatedEffects(activator, target);
                    ApplyLightsaberDefenseActivatedEffects(activator);
                    ApplyLightsaberWardActivatedEffects(activator, ability);
                    break;
            }
        }

        private void ApplyGuardiansResolve(uint activator)
        {
            var shieldPercent = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent);
            var duration = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds);
            if (shieldPercent <= 0 || duration <= 0 || !_resources.TryUseStatTrigger(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, cooldown))
                return;

            // The pool can be smaller than requested under the temporary HP cap or at the engine's HP limit.
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(
                activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent);
            var shieldAmount = TemporaryHitPointEffects.ApplyFlat(
                activator,
                TemporaryHitPointEffectKey.GuardiansResolve,
                GameMath.PercentOf(GetMaxHitPoints(activator), shieldPercent),
                duration);
            if (shieldAmount <= 0)
                return;

            var poolGeneration = TemporaryHitPointEffects.GetGrantGeneration(activator, TemporaryHitPointEffectKey.GuardiansResolve);
            _statusEffect.ApplyStatusEffect(activator, activator, new GuardiansResolveStatusEffect(shieldAmount, poolGeneration), duration);
        }

        private void ApplyHeavyVibrobladeActivatedEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (_queries.AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType))
            {
                ApplyNextAutoAttackDamageBonus(
                    activator,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
            }

            if (_queries.AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType))
            {
                ApplyGuardiansResolve(activator);
            }

        }

        private void ApplyBeastBalancedAbilityRecovery(uint activator, AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.BeastBalancedAbilityStaminaRestoreCategoryId))
            {
                return;
            }

            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.BeastBalancedAbilityStaminaRestore);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.BeastBalancedAbilityStaminaRestoreCooldownSeconds);
            if (staminaRestore <= 0 || !_resources.TryUseStatTrigger(activator, StatType.BeastBalancedAbilityStaminaRestore, cooldown))
                return;

            _resources.RestoreStaminaFromStat(activator, StatType.BeastBalancedAbilityStaminaRestore, staminaRestore);

            var master = GetMaster(activator);
            if (GetIsObjectValid(master))
            {
                _resources.RestoreStaminaFromStat(master, StatType.BeastBalancedAbilityStaminaRestore, staminaRestore);
            }
        }

        private void ApplyVibroknifeShadowActivatedEffects(
            uint activator,
            AbilityDetail ability)
        {
            var rank = _stat.GetStatAdjustment(activator, StatType.VibroknifeShadowEvasiveCombatRank);
            if (rank <= 0 || !ability.IsAreaAbility && !ability.IsSingleTargetAbility)
                return;

            var evasion = rank >= 2 ? 20 : 10;
            var enmity = rank >= 2 ? -25 : -15;
            _statusEffect.ApplyStatusEffect(
                activator,
                activator,
                new EvasiveCombatStatusEffect(-15, evasion, enmity),
                8f);
        }

        private void ApplyPistolSkirmisherActivatedEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            var duration = _stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds);
            if (duration <= 0 || !ability.IsAreaAbility && !ability.IsSingleTargetAbility)
                return;

            var evasion = _stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent);
            if (evasion != 0)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    duration,
                    StatType.PistolSkirmisherEvasiveAbilityEvasionPercent);
            }

            var nextDamage = _stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus);
            if (nextDamage > 0)
            {
                _attackTiming.GrantNextSkillAbilityBonuses(activator, SkillType.Pistol, nextDamage, 0, duration);
            }

            var reduction = _stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent);
            if (reduction > 0 && GetIsObjectValid(target))
            {
                _enmity.ReduceEnmity(activator, target, reduction);
            }
        }

        private void ApplyLightsaberOffenseActivatedEffects(uint activator, uint target)
        {
            ApplyLightsaberOffenseCentering(activator, target);
            ApplyLightsaberOffenseSecondWind(activator);
        }

        private void ApplyLightsaberOffenseCentering(uint activator, uint target)
        {
            var accuracy = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringAccuracyPercent);
            var duration = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringDurationSeconds);
            var cooldown = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringCooldownSeconds);
            if (accuracy <= 0 ||
                duration <= 0 ||
                !_resources.TryUseStatTrigger(activator, StatType.LightsaberOffenseCenteringAccuracyPercent, cooldown))
            {
                return;
            }

            _statusEffect.ApplyStatusEffect(activator, activator, new CenteringStatusEffect(accuracy), duration);

            var enmityReduction = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent);
            if (enmityReduction > 0 && GetIsObjectValid(target))
            {
                _enmity.ReduceEnmity(activator, target, enmityReduction);
            }
        }

        private void ApplyLightsaberOffenseSecondWind(uint activator)
        {
            var thresholdPercent = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindThresholdPercent);
            var basePercent = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent);
            if (thresholdPercent <= 0 || basePercent <= 0)
                return;

            var maximumStamina = _stat.GetMaxStamina(activator);
            if (maximumStamina <= 0 ||
                _stat.GetCurrentStamina(activator) > maximumStamina * (thresholdPercent / 100f))
            {
                return;
            }

            var cooldown = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindCooldownSeconds);
            if (!_resources.TryUseStatTrigger(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = GetAbilityTypeFromStatPlusOne(_stat.GetStatAdjustment(
                activator,
                StatType.LightsaberOffenseSecondWindScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                percent += Math.Max(0, GetAbilityScore(activator, scalingAbility));
            }

            var maximumPercent = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent);
            if (maximumPercent > 0)
            {
                percent = Math.Min(percent, maximumPercent);
            }

            _resources.RestoreStaminaFromStat(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent,
                GameMath.PercentOf(maximumStamina, percent));
        }

        private void ApplyLightsaberDefenseActivatedEffects(uint activator)
        {
            var sourceStatType = StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection;
            var attackDeflection = _stat.GetStatAdjustment(activator, sourceStatType);
            var deflectionStatType = _stat.GetGrantedDeflectionStatType(sourceStatType);
            if (deflectionStatType == StatType.Invalid || attackDeflection <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f, false))
            {
                _temporaryStatModifier.Replace(
                    friendly,
                    deflectionStatType,
                    attackDeflection,
                    12f,
                    sourceStatType);
            }
        }

        private void ApplyLightsaberWardActivatedEffects(
            uint activator,
            AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.WardAbilityDefenseCategoryId))
            {
                return;
            }

            var defense = _stat.GetStatAdjustment(activator, StatType.WardAbilityDefensePercentAdjustment);
            var forceDefense = _stat.GetStatAdjustment(activator, StatType.WardAbilityForceDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.WardAbilityDefenseDurationSeconds);
            if (duration <= 0 || defense == 0 && forceDefense == 0)
                return;

            if (defense != 0)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.PhysicalDefensePercentAdjustment,
                    defense,
                    duration,
                    StatType.WardAbilityDefensePercentAdjustment);
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.WardTargetPhysicalDefensePercentAdjustment,
                    defense,
                    duration,
                    StatType.WardAbilityDefensePercentAdjustment);
            }

            if (forceDefense != 0)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.WardAbilityForceDefensePercentAdjustment);
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.WardTargetForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.WardAbilityForceDefensePercentAdjustment);
            }
        }

        private void ApplyAbilityUsedPerkCategorySelfDefense(
            uint activator,
            AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategorySelfDefenseCategoryId))
            {
                return;
            }

            var evasion = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment);
            var defense = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment);
            var forceDefense = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfForceDefensePercentAdjustment);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds);
            if (duration <= 0 || evasion == 0 && defense == 0 && forceDefense == 0)
                return;

            var cooldown = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefenseCooldownSeconds);
            if (!_resources.TryUseStatTrigger(
                    activator,
                    StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment,
                    cooldown))
            {
                return;
            }

            _statusEffect.ApplyStatusEffect(
                activator,
                activator,
                new GuardingStepStatusEffect(evasion, defense, forceDefense),
                duration);
        }

        private void ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(
            uint activator,
            AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId))
            {
                return;
            }

            var attackDeflection = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var deflectionStatType = _stat.GetGrantedDeflectionStatType(
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds);
            var selfEnmity = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionSelfEnmityPercentAdjustment);
            if (deflectionStatType == StatType.Invalid || attackDeflection <= 0 || duration <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f))
            {
                _statusEffect.ApplyStatusEffect(
                    activator,
                    friendly,
                    new SentinelGuardStatusEffect(attackDeflection, selfEnmity, deflectionStatType),
                    duration);
            }
        }

        /// <param name="hitPointsSpent">HP actually paid. Nothing triggers when no HP was paid.</param>
        /// <param name="hitPointCost">Full HP cost before the payment was clipped at 1 HP; 0 when it was paid in full.</param>
        public void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent, int hitPointCost = 0)
        {
            if (hitPointsSpent <= 0)
                return;

            var window = Math.Max(1, _stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds));

            if (_stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice) > 0)
            {
                _statusEffect.ApplyStatusEffect(activator, activator, typeof(SoulSacrificeStatusEffect), 30f);
            }

            if (_stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseSoulAscension) > 0)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.HeavyVibrobladeOffenseSoulAscension,
                    1,
                    window,
                    StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            }

            ApplyHitPointSpendStaminaRestore(activator, hitPointsSpent, hitPointCost);
            ApplyHitPointSpendTemporaryHitPoints(activator, hitPointsSpent);
        }

        private void ApplyHitPointSpendTemporaryHitPoints(uint activator, int hitPointsSpent)
        {
            if (hitPointsSpent <= 0)
                return;

            var percent = _stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP);
            var duration = _stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPDurationSeconds);
            if (percent <= 0 || duration <= 0)
                return;

            var temporaryHP = GameMath.PercentOf(hitPointsSpent, percent);
            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP);
            TemporaryHitPointEffects.ApplyFlat(activator, "HIT_POINT_SPEND", temporaryHP, duration);
        }

        private void ApplyHitPointSpendStaminaRestore(uint activator, int hitPointsSpent, int hitPointCost)
        {
            var basePercent = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent);
            if (basePercent <= 0)
                return;

            var cooldown = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds);
            if (!_resources.TryUseStatTrigger(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = GetAbilityTypeFromStatPlusOne(_stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                percent += Math.Max(0, GetAbilityScore(activator, scalingAbility));
            }

            var maximum = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent);
            if (maximum > 0)
            {
                percent = Math.Min(maximum, percent);
            }

            _resources.RestoreStaminaFromStat(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent,
                CalculateHitPointSpendStaminaRestore(_stat.GetMaxStamina(activator), percent, hitPointsSpent, hitPointCost));
        }

        private void ApplyForceFPCostActivatedEffects(uint activator, AbilityDetail ability)
        {
            if (!_ability.IsForcePower(activator, ability) ||
                !ability.Requirements.OfType<AbilityRequirementFP>().Any(x => x.RequiredFP > 0))
            {
                return;
            }

            if (_stat.GetStatAdjustment(activator, StatType.ForcePrecognition) > 0 &&
                _resources.TryUseStatTrigger(activator, StatType.ForcePrecognition, 12))
            {
                _statusEffect.ApplyStatusEffect(activator, activator, typeof(DangerSenseStatusEffect), 30f);
            }

            if (_stat.GetStatAdjustment(activator, StatType.ForceConvergence) > 0 &&
                _resources.TryUseStatTrigger(activator, StatType.ForceConvergence, 45))
            {
                _statusEffect.ApplyStatusEffect(activator, activator, typeof(ForceConvergenceStatusEffect), 30f);
            }
        }

        private void ApplyAbilityUsedMasterAbilityHitChance(uint activator)
        {
            var master = GetMaster(activator);
            if (!GetIsObjectValid(master))
                return;

            var adjustment = _stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds);
            if (adjustment == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                master,
                StatType.AbilityHitChancePercentAdjustment,
                adjustment,
                duration,
                StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
        }

        private void ApplyAbilityUsedRecastReduction(uint activator, AbilityDetail ability)
        {
            ApplyAbilityUsedRecastReduction(activator, ability?.RecastGroup ?? RecastGroup.Invalid);
        }

        private void ApplyAbilityUsedRecastReduction(uint activator, RecastGroup activatedRecastGroup)
        {
            var triggerGroup = GetRecastGroupFromStat(_stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTriggerGroup));
            var secondaryTriggerGroup = GetRecastGroupFromStat(_stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSecondaryTriggerGroup));
            if (activatedRecastGroup == RecastGroup.Invalid ||
                activatedRecastGroup != triggerGroup &&
                activatedRecastGroup != secondaryTriggerGroup)
                return;

            var targetGroup = GetRecastGroupFromStat(_stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTargetGroup));
            var seconds = _stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSeconds);
            if (targetGroup == RecastGroup.Invalid || seconds <= 0)
                return;

            _recast.ReduceRecastDelay(activator, targetGroup, seconds);
        }

        private void ApplyAbilityUsedNextSkillAutoAttackDamageBonus(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType));
            var abilitySkillType = _queries.GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType));
            var damageBonus = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonus);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds);
            _attackTiming.GrantNextSkillAutoAttackDamageBonus(activator, targetSkillType, damageBonus, duration);
        }

        private void ApplyAbilityUsedNextSkillFPCostAdjustment(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType));
            var abilitySkillType = _queries.GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentSkillType));
            var adjustment = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustment);
            var duration = _stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustmentWindowSeconds);
            _attackTiming.GrantNextAbilityFPCostAdjustment(activator, targetSkillType, adjustment, duration);
        }

        private void ApplyAbilityUsedSkillEvasion(uint activator, AbilityDetail ability)
        {
            ApplyAbilityUsedSkillEvasionChannel(
                activator,
                ability,
                StatType.AbilityUsedEvasionPercentAdjustmentSkillType,
                StatType.AbilityUsedEvasionPercentAdjustment,
                StatType.AbilityUsedEvasionDurationSeconds);
            ApplyAbilityUsedSkillEvasionChannel(
                activator,
                ability,
                StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType,
                StatType.SecondaryAbilityUsedEvasionPercentAdjustment,
                StatType.SecondaryAbilityUsedEvasionDurationSeconds,
                StatType.SecondaryAbilityUsedEvasionRangedOnly);
        }

        private void ApplyAbilityUsedSkillEvasionChannel(
            uint activator,
            AbilityDetail ability,
            StatType skillTypeStat,
            StatType evasionStat,
            StatType durationStat,
            StatType? rangedOnlyStat = null)
        {
            if (!_queries.AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    _queries.GetAbilitySkillType(activator, ability),
                    skillTypeStat,
                    rangedOnlyStat))
            {
                return;
            }

            var evasionPercent = _stat.GetStatAdjustment(
                activator,
                evasionStat);
            var duration = _stat.GetStatAdjustment(
                activator,
                durationStat);
            if (evasionPercent == 0 || duration <= 0)
                return;

            _statusEffect.ApplyStatusEffect(
                activator,
                activator,
                new EvasiveFootworkStatusEffect(evasionPercent),
                duration);
        }

        private void ApplyAbilityUsedSkillRangedDeflection(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            if (!_queries.AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    _queries.GetAbilitySkillType(activator, ability),
                    StatType.AbilityUsedRangedDeflectionSkillType,
                    StatType.AbilityUsedRangedDeflectionRangedOnly))
            {
                return;
            }

            var deflection = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflection);
            var duration = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionDurationSeconds);
            var statusEffectIcon = GetEffectIconTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionStatusEffectIcon));
            if (deflection <= 0 || duration <= 0 || statusEffectIcon == EffectIconType.Invalid)
                return;

            if (_statusEffect.ApplyStatusEffect(
                    activator,
                    activator,
                    new RangedDeflectionStatusEffect(deflection, statusEffectIcon),
                    duration))
            {
                var source = _stat.GetStatTypeDeflectionSource(StatType.AbilityUsedRangedDeflection);
                _damageTaken.ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
            }
        }

        private void ApplySingleTargetAbilityUsedAttackDeflection(
            uint activator,
            AbilityDetail ability,
            bool isSingleTargetAbility)
        {
            if (!isSingleTargetAbility)
                return;

            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.SingleTargetAbilityAttackDeflectionSkillType));
            var abilitySkillType = _queries.GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            _damageTaken.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.SingleTargetAbilityAttackDeflection,
                StatType.SingleTargetAbilityAttackDeflectionDurationSeconds);
        }

        private void ApplyAbilityUsedSkillAttackDeflection(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var triggerSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedAttackDeflectionSkillType));
            var abilitySkillType = _queries.GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            _damageTaken.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedAttackDeflection,
                StatType.AbilityUsedAttackDeflectionDurationSeconds,
                StatType.AbilityUsedAttackDeflectionFPRestore);
        }

        private void ApplyAbilityUsedPerkCategoryAttackDeflection(uint activator, AbilityDetail ability)
        {
            var categoryValue = _stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType);
            if (!_perk.IsPerkInCategory(ability?.EffectiveLevelPerkType ?? PerkType.Invalid, categoryValue))
            {
                return;
            }

            _damageTaken.ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflection,
                StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds,
                StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore);
        }

        private void ApplyHostileAbilitySequenceEffects(
            uint activator,
            FeatType feat,
            AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null || !ability.IsHostileAbility)
                return;

            var windowSeconds = _stat.GetStatAdjustment(activator, StatType.HostileAbilitySequenceWindowSeconds);
            var bleedDuration = _stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds);
            if (windowSeconds <= 0 || bleedDuration <= 0)
            {
                _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
                {
                    LastFeat = feat,
                    LastUse = Clock.UtcNow
                };
                return;
            }

            var now = Clock.UtcNow;
            if (_hostileAbilitySequenceStates.TryGetValue(activator, out var state) &&
                state.LastFeat != feat &&
                (now - state.LastUse).TotalSeconds <= windowSeconds)
            {
                _temporaryStatModifier.Replace(
                    activator,
                    StatType.NextDamageDealtBleedDurationSeconds,
                    bleedDuration,
                    windowSeconds,
                    StatType.NextDamageDealtBleedDurationSeconds);
            }

            _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
            {
                LastFeat = feat,
                LastUse = now
            };
        }

        private void ApplyHostileAbilityResourceRestoreEffects(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var fpRestore = _stat.GetStatAdjustment(activator, StatType.HostileAbilityFPRestore);
            var staminaRestore = _stat.GetStatAdjustment(activator, StatType.HostileAbilityStaminaRestore);

            var restoredFP = fpRestore > 0
                ? _resources.RestoreFPFromStat(activator, StatType.HostileAbilityFPRestore, fpRestore)
                : 0;
            if (restoredFP > 0)
                _resources.ApplyAbilityRestoredFPEffects(activator);

            var restoredStamina = staminaRestore > 0
                ? _resources.RestoreStaminaFromStat(activator, StatType.HostileAbilityStaminaRestore, staminaRestore)
                : 0;

            if (restoredFP > 0 && restoredStamina > 0)
                _resources.ApplyAbilityRestoredBothResourcesEffects(activator);
        }

        private void ApplyNextAutoAttackDamageBonus(
            uint activator,
            StatType bonusStatType,
            StatType durationStatType)
        {
            var bonus = _stat.GetStatAdjustment(activator, bonusStatType);
            var duration = _stat.GetStatAdjustment(activator, durationStatType);
            if (bonus == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                StatType.NextAutoAttackDamageBonus,
                bonus,
                duration,
                StatType.NextAutoAttackDamageBonus);
        }

        private void ApplyAbilityUsedEvasion(
            uint activator,
            StatType evasionStatType,
            StatType durationStatType,
            StatType targetStatType = StatType.EvasionPercentAdjustment)
        {
            var evasionPercent = _stat.GetStatAdjustment(activator, evasionStatType);
            var duration = _stat.GetStatAdjustment(activator, durationStatType);
            if (evasionPercent == 0 || duration <= 0)
                return;

            _temporaryStatModifier.Replace(
                activator,
                targetStatType,
                evasionPercent,
                duration,
                evasionStatType);
        }

        public void ForgetActivationState(uint creature)
        {
            _hostileAbilitySequenceStates.Remove(creature);
        }
    }
}
