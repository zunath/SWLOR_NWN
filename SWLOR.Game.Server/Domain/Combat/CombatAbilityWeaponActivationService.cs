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
    /// The weapon family riders of an ability activation: the Heavy Vibroblade, Beast Mastery, Vibroknife, Pistol and Lightsaber
    /// riders that run for the skill the ability uses. <see cref="CombatAbilityActivationService"/> calls
    /// <see cref="ApplyAbilityActivatedRiders"/> in its fixed order; the part holds no state.
    /// </summary>
    public class CombatAbilityWeaponActivationService : ICombatAbilityWeaponActivationService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly IStatService _stat;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IEnmityChangeService _enmityChange;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ICombatAbilityOffenseEffectsService _offenseEffects;

        public CombatAbilityWeaponActivationService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            IStatService stat,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IEnmityChangeService enmityChange,
            ICombatRecorderService combatRecorder,
            ICombatAbilityOffenseEffectsService offenseEffects)
        {
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _stat = stat;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _enmityChange = enmityChange;
            _combatRecorder = combatRecorder;
            _offenseEffects = offenseEffects;
        }

        public void ApplyAbilityActivatedRiders(
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
                _offenseEffects.ApplyNextAutoAttackDamageBonus(
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
                _enmityChange.ReduceEnmity(activator, target, reduction);
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
                _enmityChange.ReduceEnmity(activator, target, enmityReduction);
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
    }
}
