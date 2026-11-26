using System;
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
    /// The weapon family riders of a successful ability hit: what the heavy vibroblade, Force dark, katar, lightsaber, pistol, rifle,
    /// saberstaff, spear, staff, throwing, twin blade and vibroknife perks add to the target, the Spear Disabler suppression riders,
    /// and the perk category enmity and guarded hit statuses the ability hit applies. The orchestrator in <see cref="CombatAbilityImpactService"/> picks the family.
    /// </summary>
    public class CombatAbilityWeaponRidersService : ICombatAbilityWeaponRidersService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatDefeatedEnemyEffectsService _defeatedEnemyEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IEnmityChangeService _enmityChange;

        public CombatAbilityWeaponRidersService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatDefeatedEnemyEffectsService defeatedEnemyEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IEnmityChangeService enmityChange)
        {
            _queries = queries;
            _resources = resources;
            _defeatedEnemyEffects = defeatedEnemyEffects;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _enmityChange = enmityChange;
        }

        public void ApplyHeavyVibrobladeDefenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!_queries.AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(activator, ability))
                return;

            var enmityBonus = _stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityEnmityBonus);
            if (enmityBonus > 0)
            {
                _enmityChange.ModifyEnmity(activator, target, enmityBonus);
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(CrushingBlowStatusEffect), 30f, CombatDamageType.Physical);
            }
        }

        public void ApplyForceDarkImpactRiders(
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

            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), 30f, CombatDamageType.Force);
            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), 30f, CombatDamageType.Force);
        }

        public void ApplyLightsaberOffenseImpactRiders(
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }

            if (ability.IsSingleTargetAbility)
            {
                var disruptionDuration = _stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSingleTargetForceDisruptionDurationSeconds);
                if (disruptionDuration > 0)
                {
                    _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), disruptionDuration, CombatDamageType.Force);
                }
            }

            ApplyLightsaberOffensePurify(activator, target);
        }

        private void ApplyLightsaberOffenseSunder(uint activator, uint target, int duration)
        {
            const int DefensePenaltyPercent = 15;

            if (HasSunderPenaltyAtLeast(target, DefensePenaltyPercent))
                return;

            _statusEffectApplication.ApplyStatusEffect(
                activator,
                target,
                new SunderStatusEffect(DefensePenaltyPercent),
                duration,
                CombatDamageType.Physical);
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
            _statusEffectApplication.ApplyStatusEffect(activator, target, transferred, 30f, CombatDamageType.Force);
        }

        public void ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(
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

            _statusEffectApplication.ApplyStatusEffect(
                activator,
                target,
                new CoveringClawsStatusEffect(enmity),
                duration,
                CombatDamageType.Physical);
        }

        public void ApplyKatarVenomCurrentImpactRiders(uint activator, uint target)
        {
            if (_statusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)))
            {
                _defeatedEnemyEffects.SpreadPoisonFromTarget(activator, target);
            }
        }

        public void ApplyPistolSkirmisherImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var disorientedDuration = _stat.GetStatAdjustment(activator, StatType.PistolSkirmisherDisorientedDurationSeconds);
            if (disorientedDuration > 0 && (ability.IsAreaAbility || ability.MaxRange <= 5f))
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }
        }

        public void ApplyRiflePacificationImpactRiders(uint activator, uint target)
        {
            if (_stat.GetStatAdjustment(activator, StatType.RiflePacificationNeutralizingShot) > 0)
            {
                _statusEffect.RemoveFirstBeneficialCombatStatusEffect(target, false);
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), 30f, ResistanceType.Mind);
            }

            if (_stat.GetStatAdjustment(activator, StatType.RiflePacificationOverwatch) > 0)
            {
                ActionInterruption.ClearActions(target);
                _statusEffectApplication.ApplyStatusEffect(activator, target, new FoggyMindStatusEffect(2), 30f, ResistanceType.Mind);
            }

            var pinningRank = _stat.GetStatAdjustment(activator, StatType.RiflePacificationPinningFireRank);
            if (pinningRank >= 2)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(KnockdownStatusEffect), 6f, ResistanceType.Trauma);
            }
            else if (pinningRank == 1)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), 30f, ResistanceType.Mind);
            }
        }

        public void ApplySaberstaffConduitImpactRiders(
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        public void ApplySaberstaffTempestImpactRiders(
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        public void ApplySpearDamageImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (_stat.GetStatAdjustment(activator, StatType.SpearDamageBreachStrike) > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(BreachStatusEffect), 30f, CombatDamageType.Physical);
            }

            if (ability.IsAreaAbility && _stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefense) > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(CrippledDefenseStatusEffect), 45f, CombatDamageType.Physical);
            }
        }

        public void ApplySpearDisablerImpactRiders(
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, new ForceDisruptionStatusEffect(), 30f, CombatDamageType.Force);
            }

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0 && appliesSuppression)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0 && appliesDisruption)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }
        }

        public void ApplySpearDisablerSuppressionRiders(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target))
                return;

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerFractureStrike) > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(FracturedFocusStatusEffect), 30f, CombatDamageType.Force);
            }

            if (_stat.GetStatAdjustment(activator, StatType.SpearDisablerForcebane) > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForcebaneStatusEffect), 45f, CombatDamageType.Force);
            }
        }

        public void ApplyStaffCrusherImpactRiders(uint activator, uint target)
        {
            var duration = _stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDazedDurationSeconds);
            if (duration > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        public void ApplyThrowingDeadeyeImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                _stat.GetStatAdjustment(activator, StatType.ThrowingDeadeyeMarkingToss) <= 0)
            {
                return;
            }

            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(MarkingTossStatusEffect), 30f, CombatDamageType.Physical);
        }

        public void ApplyTwinBladeDuelistImpactRiders(uint activator, uint target, AbilityDetail ability)
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
            }
        }

        public void ApplyVibroknifeShadowImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsSingleTargetAbility ||
                _stat.GetStatAdjustment(activator, StatType.VibroknifeShadowMarkedForDeath) <= 0)
            {
                return;
            }

            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(MarkedForDeathStatusEffect), 30f, CombatDamageType.Physical);
        }

        public void ApplyVibroknifeSaboteurImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var toxicCoatingRank = _stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurToxicCoatingRank);
            if (toxicCoatingRank > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ToxinStatusEffect), 30f, CombatDamageType.Poison);
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

        public void ApplyGuardedHitNextSkillAbilityExposedStatus(uint activator, uint target, SkillType skillType)
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

            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), duration, CombatDamageType.Physical);
        }
    }
}
