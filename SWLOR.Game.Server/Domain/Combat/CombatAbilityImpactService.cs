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
    /// The ability impact part of <c>Combat</c>: the riders of a successful ability hit on its target (in the order
    /// <see cref="ApplySuccessfulAbilityImpactRiders"/> runs them), the Spear Disabler suppression riders and the status-applied
    /// stamina drain. The same-target hit counts and the area ability hit sequences are tables of the hit riders and area impact
    /// parts, which <see cref="ForgetImpactState"/> clears through them.
    /// </summary>
    public class CombatAbilityImpactService : ICombatAbilityImpactService, IStatusEffectAppliedListener
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatAbilityHitRidersService _hitRiders;
        private readonly ICombatAbilityAreaImpactService _areaImpact;
        private readonly ICombatAutoAttackEffectsService _autoAttackEffects;
        private readonly ICombatDefeatedEnemyEffectsService _defeatedEnemyEffects;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IAbilityImpactState _impactState;
        private readonly IEnmityService _enmity;
        private readonly IPartyService _party;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IPairedRangedTraitsService _pairedRangedTraits;

        public CombatAbilityImpactService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatAttackTimingService attackTiming,
            ICombatActivityService combatActivity,
            ICombatAbilityHitRidersService hitRiders,
            ICombatAbilityAreaImpactService areaImpact,
            ICombatAutoAttackEffectsService autoAttackEffects,
            ICombatDefeatedEnemyEffectsService defeatedEnemyEffects,
            IStatService stat,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ITemporaryStatModifierService temporaryStatModifier,
            IAbilityImpactState impactState,
            IEnmityService enmity,
            IPartyService party,
            ICombatRecorderService combatRecorder,
            IPairedRangedTraitsService pairedRangedTraits)
        {
            _queries = queries;
            _resources = resources;
            _attackTiming = attackTiming;
            _combatActivity = combatActivity;
            _hitRiders = hitRiders;
            _areaImpact = areaImpact;
            _autoAttackEffects = autoAttackEffects;
            _defeatedEnemyEffects = defeatedEnemyEffects;
            _stat = stat;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _temporaryStatModifier = temporaryStatModifier;
            _impactState = impactState;
            _enmity = enmity;
            _party = party;
            _combatRecorder = combatRecorder;
            _pairedRangedTraits = pairedRangedTraits;
        }

        /// <summary>
        /// Gives the status effects this service as a listener of their applied notices. A reload runs boot handlers that rerun
        /// before it adopts the carried status effects, so the list is full before the first notice.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void RegisterStatusEffectListener()
        {
            _statusEffect.RegisterAppliedListener(this);
        }

        /// <summary>A status effect was applied or refreshed: the target loses the stamina the source's stat drains.</summary>
        public void OnStatusApplied(uint source, uint creature, StatusEffectCategory categories) =>
            ApplyStatusAppliedTargetStaminaDrain(source, creature, categories);

        /// <summary>A hostile debuff was applied or refreshed: the source's party buff for a hostile ability applies.</summary>
        public void OnHostileDebuffApplied(uint source, AbilityDetail ability) =>
            ApplyHostileAbilityPartyBuff(source, ability, 0, true);

        /// <summary>Nothing to do when an application completes.</summary>
        public void OnApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability)
        {
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

            _pairedRangedTraits.RecordSuccessfulWeaponAbilityHit(activator, target, skillType);

            ApplyHostileAbilityPartyBuff(activator, ability, damage, statusApplied);

            if (firstHostileAbilityHitDamageBonusApplied)
                _combatActivity.ApplyFirstHostileAbilityHitCount(activator, ability);
            if (allowsHitProcs && ability.IsHostileAbility && !ability.SuppressesSourceStatusStackRiders)
            {
                _autoAttackEffects.ApplySourceStatusStackEffects(activator, target);
            }

            _hitRiders.ApplyHostileAbilityHitNextAutoAttackNoDelay(activator, ability);
            _hitRiders.ApplySameTargetHostileAbilityHitEffects(activator, target, ability);
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
            _hitRiders.ApplyRangedAbilityHitNearTargetEffects(activator, target, ability, skillType);
            _hitRiders.ApplyCostlyAbilityHitEffects(activator, target, ability, skillType);
            _areaImpact.ApplyAbilityDamageRiders(activator, target, ability, skillType, damageType, damage);
            _areaImpact.ApplyAreaAbilityPulse(activator, target, ability, isFirstSuccessfulTarget);
            _areaImpact.ApplyAreaAbilityTargetHitSequenceEffects(activator, target, ability, skillType);
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(CrushingBlowStatusEffect), 30f, CombatDamageType.Physical);
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

            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), 30f, CombatDamageType.Force);
            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), 30f, CombatDamageType.Force);
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

            _statusEffectApplication.ApplyStatusEffect(
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
                _defeatedEnemyEffects.SpreadPoisonFromTarget(activator, target);
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

            var sequence = _impactState.GetAbilityImpactSequence(activator);
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
                _statusEffectApplication.ApplyStatusEffect(activator, friendly,
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }
        }

        private void ApplyRiflePacificationImpactRiders(uint activator, uint target)
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), duration, CombatDamageType.Force);
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), duration, CombatDamageType.Force);
            }
        }

        private void ApplySpearDamageImpactRiders(
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

        private void ApplyStaffCrusherImpactRiders(uint activator, uint target)
        {
            var duration = _stat.GetStatAdjustment(activator, StatType.StaffCrusherFinisherDazedDurationSeconds);
            if (duration > 0)
            {
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
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

            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(MarkingTossStatusEffect), 30f, CombatDamageType.Physical);
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
                _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(DazedStatusEffect), duration, ResistanceType.Mind);
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

            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(MarkedForDeathStatusEffect), 30f, CombatDamageType.Physical);
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

            _statusEffectApplication.ApplyStatusEffect(activator, target, new ChallengeEnmityStatusEffect(enmity), duration);
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

            _statusEffectApplication.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), duration, CombatDamageType.Physical);
        }

        public void ForgetImpactState(uint creature)
        {
            _hitRiders.ForgetSameTargetHitState(creature);
            _areaImpact.ForgetAreaAbilityTargetHitSequences(creature);
        }
    }
}
