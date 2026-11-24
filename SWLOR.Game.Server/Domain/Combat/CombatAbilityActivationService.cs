using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
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
        private readonly ICombatDamageTakenEffectsService _damageTakenEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityImpactState _impactState;
        private readonly IEnmityService _enmity;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ICombatAbilityCostEffectsService _costEffects;
        private readonly ICombatAbilityDefenseEffectsService _defenseEffects;

        public CombatAbilityActivationService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatActivityService combatActivity,
            ICombatDamageTakenEffectsService damageTakenEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityImpactState impactState,
            IEnmityService enmity,
            ICombatRecorderService combatRecorder,
            ICombatAbilityCostEffectsService costEffects,
            ICombatAbilityDefenseEffectsService defenseEffects)
        {
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _combatActivity = combatActivity;
            _damageTakenEffects = damageTakenEffects;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _impactState = impactState;
            _enmity = enmity;
            _combatRecorder = combatRecorder;
            _costEffects = costEffects;
            _defenseEffects = defenseEffects;
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

            _costEffects.ApplyAbilityUsedRecastReduction(activator, ability);
            _costEffects.ApplyAbilityUsedNextSkillAutoAttackDamageBonus(activator, ability);
            _costEffects.ApplyAbilityUsedNextSkillFPCostAdjustment(activator, ability);
            _costEffects.ApplyAbilityUsedMasterAbilityHitChance(activator);
            _costEffects.ApplyForceFPCostActivatedEffects(activator, ability);

            var skillType = ResolveActivatedAbilitySkillType(activator, ability, summary);
            var isSingleTargetAbility = summary.IsSingleTargetAbility ||
                summary.SkillType == SkillType.Invalid &&
                ability.IsHostileAbility &&
                ability.IsSingleTargetAbility;

            _defenseEffects.ApplyAbilityUsedSkillEvasion(activator, ability);
            _defenseEffects.ApplyHostileAbilityUsedEvasion(activator, ability, skillType);
            _defenseEffects.ApplyCostlyAbilityUsedEvasion(activator, ability, skillType);
            _defenseEffects.ApplyAbilityUsedSkillRangedDeflection(activator, ability);
            ApplyAbilityUsedMovementSpeed(activator, ability, skillType);
            _defenseEffects.ApplyAbilityUsedSkillAttackDeflection(activator, ability);
            _defenseEffects.ApplyAbilityUsedPerkCategoryAttackDeflection(activator, ability);
            _defenseEffects.ApplySingleTargetAbilityUsedAttackDeflection(activator, ability, isSingleTargetAbility);
            var isAreaAbility = _impactState.IsAreaImpact(summary, ability);
            _defenseEffects.ApplyAreaAbilityUsedEvasion(activator, ability, skillType, isAreaAbility);
            ApplyAreaAbilityUsedRewards(activator, ability, isAreaAbility);
            ApplyHostileAbilityForceAttack(activator, ability);
            ApplyHostileAbilityFPSpendForceAttack(activator, ability);
            _defenseEffects.ApplyAbilityUsedNearbyAllyDefense(activator);
            _defenseEffects.ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(activator, ability);
            _defenseEffects.ApplyAbilityUsedPerkCategorySelfDefense(activator, ability);
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
                _damageTakenEffects.ApplyAbilityGrantedAttackDeflectionEffects(activator,
                    _stat.GetStatTypeDeflectionSource(StatType.AreaAbilityUsedAttackDeflection));

            var restore = _stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedFPRestore);
            if (restore > 0 && _resources.RestoreFPFromStat(activator, StatType.AreaAbilityUsedFPRestore, restore) > 0)
                _resources.ApplyAbilityRestoredFPEffects(activator);
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

            _statusEffectApplication.ApplyStatusEffect(
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
                _statusEffectApplication.ApplyStatusEffect(
                    activator,
                    activator,
                    new ButchersTempoStatusEffect(currentTotal),
                    duration);
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
            _statusEffectApplication.ApplyStatusEffect(activator, activator, new GuardiansResolveStatusEffect(shieldAmount, poolGeneration), duration);
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
            _statusEffectApplication.ApplyStatusEffect(
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

            _statusEffectApplication.ApplyStatusEffect(activator, activator, new CenteringStatusEffect(accuracy), duration);

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

        public void ForgetActivationState(uint creature)
        {
            _hostileAbilitySequenceStates.Remove(creature);
        }
    }
}
