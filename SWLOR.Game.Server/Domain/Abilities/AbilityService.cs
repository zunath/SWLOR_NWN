using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Abilities: the lifecycle of each impact while it resolves, and the combat impacts content applies. The catalog, the
    /// impact state, the activation and target checks, the auras, the temporary immunities, the activation state
    /// <c>UsePerkFeat</c> runs on, the impact messages, Dark Force, the damage delivery, the status application and the damage
    /// inputs and stages are parts of their own (Readmes/MaintainabilityRefactorAbilitySplit.md). The logic that used to be static in <c>Service.Ability</c> is moved here unchanged; the event handlers run on the
    /// container's instance, and static callers reach it through <c>GameServices.Ability</c>. Converted services are
    /// injected. Calls into the unconverted <c>Combat</c>, <c>Stat</c>, <c>Perk</c>, <c>WeaponDamage</c> and
    /// feature code and the definition search sit behind one-line <c>protected virtual</c> methods whose bodies are the
    /// original calls.
    /// </summary>
    public class AbilityService : IAbilityService
    {
        private readonly IStatusEffectService _statusEffect;
        private readonly IEnmityService _enmity;
        private readonly ICombatPointService _combatPoint;
        private readonly ITelegraphService _telegraph;
        private readonly IMusicService _music;
        private readonly INamedAnimationService _namedAnimation;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IMeleeTechniqueService _meleeTechnique;
        private readonly ICapstoneProofService _capstoneProof;
        private readonly IStealthService _stealth;
        private readonly IAbilityImpactState _impactState;
        private readonly ICombatImpactGeometryService _geometry;
        private readonly ICombatAbilityBonusService _abilityBonus;
        private readonly ICombatAbilityHitService _abilityHit;
        private readonly ICombatAbilityImpactService _abilityImpact;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatHitEffectsService _hitEffects;
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly IAbilityImpactMessageService _impactMessage;
        private readonly IAbilityDarkForceService _darkForce;
        private readonly IAbilityDamageDeliveryService _damageDelivery;
        private readonly IAbilityStatusApplicationService _statusApplication;
        private readonly IAbilityDamageInputService _damageInput;
        private readonly IAbilityDamageStageService _damageStage;

        public AbilityService(
            IStatusEffectService statusEffect,
            IEnmityService enmity,
            ICombatPointService combatPoint,
            ITelegraphService telegraph,
            IMusicService music,
            INamedAnimationService namedAnimation,
            ICombatRecorderService combatRecorder,
            IMeleeTechniqueService meleeTechnique,
            ICapstoneProofService capstoneProof,
            IStealthService stealth,
            IAbilityImpactState impactState,
            ICombatImpactGeometryService geometry,
            ICombatAbilityBonusService abilityBonus,
            ICombatAbilityHitService abilityHit,
            ICombatAbilityImpactService abilityImpact,
            ICombatActivityService combatActivity,
            ICombatHitEffectsService hitEffects,
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            IAbilityImpactMessageService impactMessage,
            IAbilityDarkForceService darkForce,
            IAbilityDamageDeliveryService damageDelivery,
            IAbilityStatusApplicationService statusApplication,
            IAbilityDamageInputService damageInput,
            IAbilityDamageStageService damageStage)
        {
            _statusEffect = statusEffect;
            _enmity = enmity;
            _combatPoint = combatPoint;
            _telegraph = telegraph;
            _music = music;
            _namedAnimation = namedAnimation;
            _combatRecorder = combatRecorder;
            _meleeTechnique = meleeTechnique;
            _capstoneProof = capstoneProof;
            _stealth = stealth;
            _impactState = impactState;
            _geometry = geometry;
            _abilityBonus = abilityBonus;
            _abilityHit = abilityHit;
            _abilityImpact = abilityImpact;
            _combatActivity = combatActivity;
            _hitEffects = hitEffects;
            _queries = queries;
            _resources = resources;
            _impactMessage = impactMessage;
            _darkForce = darkForce;
            _damageDelivery = damageDelivery;
            _statusApplication = statusApplication;
            _damageInput = damageInput;
            _damageStage = damageStage;
        }

        /// <summary>
        /// How long the visual-only impact flash lingers on an instant area ability, in seconds.
        /// Long enough to read the shape, short enough not to be mistaken for a pre-cast telegraph.
        /// </summary>
        public const float DefaultImpactFlashDuration = 0.3f;

        private const int HostileAbilityBaseEnmity = 100;
        private const int HostileAbilityMissEnmity = 1;

        /// <summary>
        /// Starts impact tracking and defers pending damage bonuses until a damaging payload,
        /// retaining activation-marker snapshots for the impact footprint and flash decision.
        /// </summary>
        public void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            bool countsAsAttackAttempt = true,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null,
            AbilityImpactSequence sequence = null,
            uint triggeringWeapon = OBJECT_INVALID,
            int? triggeringWeaponDamage = null,
            CombatDamageType? triggeringWeaponDamageType = null,
            WeaponAbilityTraitActivation weaponTraits = null)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            BeginAbilityImpact(activator, ability, 0, 0,
                countsAsAttackAttempt: countsAsAttackAttempt,
                activationAreaTelegraphs: activationAreaTelegraphs, sequence: sequence);
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (_meleeTechnique.IsSweepActivation(activator, ability))
                trackedImpact.RecordShape(ability.SkillType, true);
            if (weaponTraits != null)
            {
                trackedImpact.Sequence.WeaponTraits = weaponTraits;
            }
            trackedImpact.TriggeringWeaponDamage = triggeringWeaponDamage ?? (GetIsObjectValid(triggeringWeapon)
                ? GetAbilityDMG(activator, triggeringWeapon, ability.SkillType)
                : trackedImpact.TriggeringWeaponDamage);
            if (triggeringWeaponDamageType.HasValue || GetIsObjectValid(triggeringWeapon))
                trackedImpact.TriggeringWeaponDamageType = triggeringWeaponDamageType ?? GetWeaponDamageType(triggeringWeapon);
            trackedImpact.ResolveDamageBonuses = () =>
            {
                var abilitySkillType = GetAbilitySkillType(activator, ability);
                var nextAbilityDamageBonus = ConsumeNextAbilityDamageBonus(activator, ability.EffectiveLevelPerkType);
                var nextSkillAbilityBonuses = ConsumeNextSkillAbilityBonuses(
                    activator,
                    abilitySkillType,
                    ability.ResolveImpactStatusCategories?.Invoke(activator) ?? StatusEffectCategory.None);
                var persistentCriticalRate = GetPersistentNextSkillAbilityCriticalRateBonus(
                    activator,
                    abilitySkillType);
                var queuedWeaponBonuses = ability.ActivationType == AbilityActivationType.Weapon
                    ? ConsumeQueuedWeaponAbilityBonuses(activator, abilitySkillType)
                    : default;
                var guardedHitBonuses = ability.IsHostileAbility
                    ? ConsumeNextAttackGuardedHitBonuses(activator)
                    : (DMGBonus: 0, CriticalRatePercentAdjustment: 0, EnmityBonus: 0);
                var statusAppliedNextAttackDamageBonus = ability.IsHostileAbility
                    ? GetStatusAppliedNextAttackDamageBonus(activator)
                    : 0;
                trackedImpact.AddDamageBonuses(
                    nextAbilityDamageBonus +
                    nextSkillAbilityBonuses.DamageBonus +
                    guardedHitBonuses.DMGBonus +
                    statusAppliedNextAttackDamageBonus +
                    queuedWeaponBonuses.DamageBonus,
                    nextSkillAbilityBonuses.CriticalRatePercentAdjustment +
                    persistentCriticalRate +
                    queuedWeaponBonuses.CriticalRatePercentAdjustment +
                    guardedHitBonuses.CriticalRatePercentAdjustment,
                    nextSkillAbilityBonuses.DefenseIgnorePercentAdjustment,
                    guardedHitBonuses.EnmityBonus,
                    statusAppliedNextAttackDamageBonus,
                    queuedWeaponBonuses.CriticalDamagePercentAdjustment);
            };
            if (CanDealCombatImpactDamage(0, ability))
                trackedImpact.EnsureDamageBonuses();
        }

        /// <summary>
        /// Registers an impact using already-resolved bonuses and activation-marker geometry.
        /// </summary>
        private void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment = 0,
            int nextAttackEnmityBonus = 0,
            int statusAppliedNextAttackDamageBonus = 0,
            bool countsAsAttackAttempt = true,
            int nextAbilityCriticalDamagePercentAdjustment = 0,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null,
            AbilityImpactSequence sequence = null,
            AbilityImpactTracker sequenceOwner = null,
            bool resolveDamageBonusesFromOwner = false,
            int? triggeringWeaponDamage = null,
            CombatDamageType? triggeringWeaponDamageType = null)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var abilitySkillType = GetAbilitySkillType(activator, ability);
            var usesQueuedNaturalWeapon =
                ability.ActivationType == AbilityActivationType.Weapon && ability.SkillType == SkillType.BeastMastery;
            _impactState.Track(activator, new AbilityImpactTracker(
                ability,
                nextAbilityDamageBonus,
                nextAbilityCriticalRatePercentAdjustment,
                nextAbilityDefenseIgnorePercentAdjustment,
                nextAttackEnmityBonus,
                statusAppliedNextAttackDamageBonus,
                countsAsAttackAttempt,
                nextAbilityCriticalDamagePercentAdjustment,
                activationAreaTelegraphs,
                sequence)
            {
                SequenceOwner = sequenceOwner?.SequenceOwner ?? sequenceOwner,
                TriggeringWeaponDamage = triggeringWeaponDamage ?? sequenceOwner?.TriggeringWeaponDamage ??
                    _meleeTechnique.GetCapturedWeaponDamage(activator, ability) ?? GetCombatImpactWeaponDamage(
                    activator, abilitySkillType, usesQueuedNaturalWeapon, abilityImpact: true),
                // Captured with the DMG so a delayed impact cannot pair it with a weapon swapped in later.
                TriggeringWeaponDamageType = triggeringWeaponDamageType ?? sequenceOwner?.TriggeringWeaponDamageType ??
                    _meleeTechnique.GetCapturedWeaponDamageType(activator, ability) ??
                    GetCombatImpactWeaponDamageType(activator, abilitySkillType, usesQueuedNaturalWeapon)
            });
            if (resolveDamageBonusesFromOwner && sequenceOwner != null)
            {
                var impact = _impactState.GetTrackedAbilityImpact(activator);
                impact.ResolveDamageBonuses = () =>
                {
                    sequenceOwner.EnsureDamageBonuses();
                    impact.AddDamageBonuses(
                        sequenceOwner.NextAbilityDamageBonus - sequenceOwner.StatusAppliedNextAttackDamageBonus,
                        sequenceOwner.NextAbilityCriticalRatePercentAdjustment,
                        sequenceOwner.NextAbilityDefenseIgnorePercentAdjustment,
                        sequenceOwner.NextAttackEnmityBonus,
                        0,
                        sequenceOwner.NextAbilityCriticalDamagePercentAdjustment);
                };
            }
        }

        /// <summary>
        /// Retains the originating ability and cast sequence for recurring impacts without
        /// consuming another activation's pending bonuses or replacing its impact tracker.
        /// </summary>
        public Action CaptureRepeatedAbilityImpact(
            uint activator, Action impactAction, int baseDamage = 0, bool requiresLivingActivator = true)
        {
            ArgumentNullException.ThrowIfNull(impactAction);
            var applyImpact = CaptureRepeatedAbilityImpact<Action>(
                activator, action => action(), baseDamage, requiresLivingActivator);
            return () => applyImpact(impactAction);
        }

        /// <summary>
        /// Captures one cast's recurring impact context while allowing each delayed impact
        /// to retain its own target or payload. Bonuses are consumed only when an impact lands.
        /// </summary>
        public Action<T> CaptureRepeatedAbilityImpact<T>(
            uint activator, Action<T> impactAction, int baseDamage = 0, bool requiresLivingActivator = true,
            bool retainCapturedBonuses = false)
        {
            ArgumentNullException.ThrowIfNull(impactAction);
            // Scheduled damage belongs to this cast. Resolve its armed bonuses now so
            // later pulses cannot consume bonuses earned after the field was created.
            PrepareCombatImpactDamageBonuses(activator, baseDamage);
            var originatingImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (originatingImpact == null)
                return impactAction;

            var ability = originatingImpact.Ability;
            var sequence = originatingImpact.Sequence;
            var copyPartitionedBonuses = retainCapturedBonuses
                ? originatingImpact.CapturePartitionedDamageBonuses() : null;
            return payload =>
            {
                if (!GetIsObjectValid(activator) || requiresLivingActivator && GetCurrentHitPoints(activator) <= 0)
                    return;

                using var recorderAttribution = _combatRecorder.BeginAbilityAttribution(activator, ability);
                var previousImpact = _impactState.GetTrackedAbilityImpact(activator);
                BeginAbilityImpact(activator, ability, 0, 0, countsAsAttackAttempt: false, sequence: sequence,
                    triggeringWeaponDamage: originatingImpact.TriggeringWeaponDamage,
                    triggeringWeaponDamageType: originatingImpact.TriggeringWeaponDamageType);
                if (copyPartitionedBonuses != null)
                    copyPartitionedBonuses(_impactState.GetTrackedAbilityImpact(activator));
                else
                    _impactState.GetTrackedAbilityImpact(activator).CopyRepeatedDamageBonusesFrom(originatingImpact);
                var completed = false;
                try
                {
                    impactAction(payload);
                    var summary = EndAbilityImpact(activator);
                    if (!retainCapturedBonuses)
                        originatingImpact.CompleteRepeatedDamageBonusImpact(summary.ImpactedTargetCount > 0);
                    completed = true;
                    if (_combatRecorder.IsRecording)
                        _combatRecorder.RecordRecurringAbilityImpact(activator, ability, summary);
                    ApplyAbilityImpactEffects(activator, summary);
                }
                finally
                {
                    if (!completed)
                        AbortAbilityImpact(activator);
                    if (previousImpact != null)
                        _impactState.Track(activator, previousImpact);
                }
            };
        }

        public Func<int, Action<T>> CaptureRepeatedAbilityImpactBatch<T>(
            uint activator, Action<T> impactAction, int baseDamage = 0)
        {
            ArgumentNullException.ThrowIfNull(impactAction);
            PrepareCombatImpactDamageBonuses(activator, baseDamage);
            var originatingImpact = _impactState.GetTrackedAbilityImpact(activator);

            return count =>
            {
                AbilityImpactTracker batchImpact = null;
                var batch = new AbilityImpactBatch<T>(count, (payload, isFinalImpact) =>
                {
                    if (originatingImpact == null)
                    {
                        impactAction(payload);
                        return;
                    }

                    if (!GetIsObjectValid(activator) || GetCurrentHitPoints(activator) <= 0)
                        return;

                    using var recorderAttribution = _combatRecorder.BeginAbilityAttribution(activator, originatingImpact.Ability);
                    var previousImpact = _impactState.GetTrackedAbilityImpact(activator);
                    try
                    {
                        if (batchImpact == null)
                        {
                            BeginAbilityImpact(activator, originatingImpact.Ability, 0, 0,
                                countsAsAttackAttempt: false, sequence: originatingImpact.Sequence,
                                triggeringWeaponDamage: originatingImpact.TriggeringWeaponDamage,
                                triggeringWeaponDamageType: originatingImpact.TriggeringWeaponDamageType);
                            batchImpact = _impactState.GetTrackedAbilityImpact(activator);
                            batchImpact.CopyRepeatedDamageBonusesFrom(originatingImpact);
                        }
                        else
                        {
                            _impactState.Track(activator, batchImpact);
                        }

                        impactAction(payload);
                        _impactState.Remove(activator);
                        _damageDelivery.FlushDamageEffects(batchImpact, activator);
                        originatingImpact.CompleteRepeatedDamageBonusImpact(batchImpact.Summary.ImpactedTargetCount > 0);
                        if (isFinalImpact)
                        {
                            _impactState.Track(activator, batchImpact);
                            var summary = EndAbilityImpact(activator);
                            if (_combatRecorder.IsRecording)
                                _combatRecorder.RecordRecurringAbilityImpact(activator, originatingImpact.Ability, summary);
                            ApplyAbilityImpactEffects(activator, summary);
                        }
                    }
                    finally
                    {
                        _impactState.Remove(activator);
                        if (previousImpact != null)
                            _impactState.Track(activator, previousImpact);
                    }
                });
                return batch.Apply;
            };
        }

        public AbilityImpactSummary EndAbilityImpact(uint activator)
        {
            if (!_impactState.TryGetTrackedAbilityImpact(activator, out var impact))
                return new AbilityImpactSummary();

            _impactState.Remove(activator);
            _damageDelivery.FlushDamageEffects(impact, activator);
            if (impact.Ability.IsHostileAbility && impact.Summary.CriticalHitCount > 0)
            {
                RefundCriticalRangedAbilityStaminaCost(
                    activator,
                    impact.Ability);
            }

            if (impact.Ability.IsHostileAbility && impact.CountsAsAttackAttempt)
            {
                if (impact.Summary.CriticalHitCount > 0)
                {
                    ConsumePersistentNextSkillAbilityCriticalRateBonus(
                        activator,
                        impact.Summary.SkillType);
                }
                else
                {
                    ApplyNonCriticalAbilityEffects(
                        activator,
                        OBJECT_INVALID,
                        impact.Summary.SkillType);
                }

                _statusEffect.NotifyAttackAttemptStatusEffects(
                    activator,
                    impact.Summary.SkillType,
                    impact.Summary);
            }
            _impactState.SetLastCompletedAbilityImpactSummary(activator, impact.Summary);
            _meleeTechnique.CompleteActivation(activator, impact.Summary);
            return impact.Summary;
        }

        /// <summary>
        /// Discards an incomplete tracked impact without applying its queued damage effects.
        /// </summary>
        public void AbortAbilityImpact(uint activator)
        {
            if (!_impactState.TryGetTrackedAbilityImpact(activator, out var impact))
                return;

            _impactState.Remove(activator);
            Log.WriteStructured(
                LogGroup.Error,
                "Ability impact aborted for activator {Activator}. Ability: {Ability}.",
                activator,
                impact.Ability?.Name ?? "Unknown");
        }

        /// <summary>
        /// Plays the definition's finite receipt burst after a heal, buff, or other noncombat
        /// action succeeds. Call for the actual recipient while its ability impact is active.
        /// Failed actions must not call this method. Combat impacts report success internally.
        /// </summary>
        public void PlaySuccessfulImpactVisualEffect(uint activator, uint recipient)
        {
            PlaySuccessfulImpactVisualEffect(_impactState.GetTrackedAbilityImpact(activator)?.VisualEffects, recipient);
        }

        /// <summary>
        /// Captures the originating visual for a delayed noncombat pulse without changing combat
        /// tracking or bonuses. Capture once per pulse, then report its successful recipients.
        /// </summary>
        public Action<uint> CaptureSuccessfulImpactVisualEffect(uint activator)
        {
            var effect = GetSuccessfulImpactVisualEffect(activator);
            var visuals = new AbilityImpactVisualEffects(effect);
            return recipient => PlaySuccessfulImpactVisualEffect(visuals, recipient);
        }

        /// <summary>
        /// Shows an instant visual effect on <paramref name="target"/> to one player only, so feedback
        /// for a hidden creature never marks its position for anyone else.
        /// </summary>
        public void ShowVisualEffectToPlayer(uint player, uint target, VisualEffect visualEffect)
        {
            if (visualEffect == VisualEffect.None || !GetIsObjectValid(target) || !GetIsPC(player) || GetIsDM(player))
                return;

            PlayerPlugin.ApplyInstantVisualEffectToObject(player, target, visualEffect);
        }

        /// <summary>Returns the current impact's visual reference for a later independent combat event.</summary>
        public VisualEffect GetSuccessfulImpactVisualEffect(uint activator)
        {
            return _impactState.GetTrackedAbilityImpact(activator)?.Ability.SuccessfulImpactVisualEffect ?? VisualEffect.None;
        }

        private void PlaySuccessfulImpactVisualEffect(AbilityImpactVisualEffects visuals, uint recipient)
        {
            if (visuals == null || !GetIsObjectValid(recipient) || !visuals.TryRecordRecipient(recipient))
                return;

            if (_stealth.IsConcealed(recipient))
            {
                ShowVisualEffectToPlayer(recipient, recipient, visuals.Effect);
                return;
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visuals.Effect), recipient);
        }

        public void ApplyHostileAbilityEnmity(uint activator, uint target, int bonus = 0)
        {
            // Damage is credited once by the actual damage event, including resistance and absorption.
            var profile = WeaponImpactProfile.Get(activator);
            var amount = CalculateHostileAbilityEnmity(bonus, profile?.VolleyIndex ?? 0, profile?.VolleyCount ?? 1);
            _enmity.ModifyEnmity(activator, target, amount);
        }

        public int CalculateHostileAbilityEnmity(int bonus, int volleyIndex = 0, int volleyCount = 1) =>
            WeaponImpactDamagePartition.Partition(HostileAbilityBaseEnmity + Math.Max(0, bonus), volleyIndex, volleyCount);

        private void ApplyMissedHostileAbilityEnmity(uint activator, uint target)
        {
            _enmity.ModifyEnmity(activator, target, HostileAbilityMissEnmity);
        }

        /// <summary>
        /// Applies the standard hostile combat impact used by weapon and martial abilities.
        /// </summary>
        public int ApplyCombatImpact(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            bool isArea,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            Animation impactAnimation = Animation.Invalid,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool playImpactAnimation = true,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false,
            Action<uint> beforeImpact = null,
            bool isAreaImpact = false)
        {
            PrepareCombatImpactDamageBonuses(activator, baseDamage);
            var totalDamage = 0;
            _impactState.RecordAbilityImpactShape(activator, skillType, isArea || isAreaImpact);

            if (isArea)
            {
                var center = GetIsObjectValid(target) ? GetLocation(target) : targetLocation;
                var creatures = new List<uint>();

                foreach (var creature in ObjectSnapshot.InShape(Shape.Sphere, 5.0f, center, true))
                {
                    if (!GetIsObjectValid(creature))
                        continue;

                    if (_geometry.HasAbilityLineOfSight(activator, creature))
                        creatures.Add(creature);

                }

                if (areaVisualEffect != VisualEffect.None &&
                    GetIsObjectValid(GetAreaFromLocation(center)))
                {
                    VisualEffectPlayback.ApplyAtLocation(areaVisualEffect, center);
                }

                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    creatures,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus: enmityBonus,
                    beforeImpact: beforeImpact,
                    afterSuccessfulHit: afterSuccessfulHit,
                    beforeSuccessfulImpactRiders: beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment: hitChancePercentAdjustment,
                    criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                    useNPCStatScaling: useNPCStatScaling,
                    awardsCombatPoints: awardsCombatPoints,
                    effectDamageType: effectDamageType,
                    combatImpactDamageAbility: combatImpactDamageAbility,
                    resolvesHit: resolvesHit,
                    canCritical: canCritical,
                    useUnscaledDamage: useUnscaledDamage);
            }
            else if (GetIsObjectValid(target))
            {
                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    new[] { target },
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus: enmityBonus,
                    beforeImpact: beforeImpact,
                    afterSuccessfulHit: afterSuccessfulHit,
                    beforeSuccessfulImpactRiders: beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment: hitChancePercentAdjustment,
                    criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                    useNPCStatScaling: useNPCStatScaling,
                    awardsCombatPoints: awardsCombatPoints,
                    effectDamageType: effectDamageType,
                    combatImpactDamageAbility: combatImpactDamageAbility,
                    resolvesHit: resolvesHit,
                    canCritical: canCritical,
                    useUnscaledDamage: useUnscaledDamage);
            }

            if (playImpactAnimation)
                PlayCombatImpactAnimation(activator, impactAnimation);

            return totalDamage;
        }

        /// <summary>
        /// Applies a hostile combat impact after a visible telegraph resolves.
        /// </summary>
        public int ApplyTelegraphedCombatImpact(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float telegraphDuration,
            float lengthOrRadius,
            float width = 0f,
            IEnumerable<Type> additionalStatusEffects = null,
            bool centerOnActivator = false,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            Action<AbilityImpactSummary> afterImpactAction = null,
            int maxTargets = 0,
            Animation impactAnimation = Animation.Invalid,
            int enmityBonus = 0,
            Action<uint> beforeImpact = null,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool playImpactAnimation = true,
            bool alwaysApplyAreaVisualEffect = true,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool sendsNoTargetMessage = true,
            bool resolvesHit = true,
            bool canCritical = true,
            float impactFlashDuration = DefaultImpactFlashDuration,
            bool useUnscaledDamage = false,
            Action<TelegraphGeometry> onGeometryResolved = null)
        {
            PrepareCombatImpactDamageBonuses(activator, baseDamage);
            _impactState.RecordAbilityImpactShape(activator, skillType, true);
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            var backOffsetOrigin = trackedImpact?.Ability.Targeting?.Flags
                .HasFlag(AbilityTargetingFlags.BackOffsetOrigin) == true;
            var geometry = _geometry.ResolveCombatImpactGeometry(
                activator, target, targetLocation, shape, lengthOrRadius, width, centerOnActivator, backOffsetOrigin,
                trackedImpact?.ActivationAreaTelegraphs);

            // Directional visuals must use the same captured footprint as the damage.
            onGeometryResolved?.Invoke(geometry);

            if (telegraphDuration <= 0f)
            {
                // Instant-cast area abilities cannot use a pre-cast telegraph without violating the
                // Bible's "Instant" activation time, so they flash their shape at impact instead.
                // The flash is purely visual: damage below is still applied immediately. Compare
                // the actual impact geometry with the activation marker before suppressing a redraw.
                ShowAreaImpactFlash(
                    activator,
                    geometry,
                    impactFlashDuration,
                    trackedImpact?.Ability.ImpactDelay > 0f ? null : trackedImpact?.ActivationAreaTelegraphs);

                var totalDamage = ApplyCombatImpactInShape(
                    activator,
                    geometry,
                    skillType,
                    baseDamage,
                    duration,
                    statusEffect,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    areaVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    maxTargets,
                    enmityBonus,
                    beforeImpact,
                    afterSuccessfulHit,
                    beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment,
                    criticalRatePercentAdjustment,
                    useNPCStatScaling,
                    awardsCombatPoints,
                    effectDamageType,
                    alwaysApplyAreaVisualEffect,
                    combatImpactDamageAbility,
                    sendsNoTargetMessage,
                    resolvesHit,
                    canCritical,
                    useUnscaledDamage);
                if (playImpactAnimation)
                    PlayCombatImpactAnimation(activator, impactAnimation);

                if (trackedImpact != null)
                    afterImpactAction?.Invoke(trackedImpact.Summary);

                return totalDamage;
            }

            var areaVisualLocation = Location(
                geometry.Area,
                geometry.Position,
                0f);
            var deferredNextAbilityDamageBonus =
                (trackedImpact?.NextAbilityDamageBonus ?? 0) -
                (trackedImpact?.StatusAppliedNextAttackDamageBonus ?? 0);
            var action = BuildTelegraphedCombatImpactAction(
                skillType,
                baseDamage,
                duration,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory,
                shape,
                areaVisualLocation,
                trackedImpact?.Ability,
                trackedImpact,
                deferredNextAbilityDamageBonus,
                trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0,
                trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0,
                trackedImpact?.NextAttackEnmityBonus ?? 0,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                areaVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                afterImpactAction,
                maxTargets,
                enmityBonus,
                beforeImpact,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                hitChancePercentAdjustment,
                criticalRatePercentAdjustment,
                useNPCStatScaling,
                awardsCombatPoints,
                effectDamageType,
                alwaysApplyAreaVisualEffect,
                combatImpactDamageAbility,
                sendsNoTargetMessage,
                resolvesHit,
                canCritical,
                useUnscaledDamage);

            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    _telegraph.CreateSphereTelegraph(
                        activator,
                        GetPositionFromLocation(areaVisualLocation),
                        geometry.Size.X,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Cone:
                    _telegraph.CreateConeTelegraph(
                        activator,
                        geometry.Position,
                        geometry.Rotation,
                        geometry.Size.X,
                        geometry.Size.Y,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Line:
                    _telegraph.CreateLineTelegraph(
                        activator,
                        geometry.Position,
                        geometry.Rotation,
                        geometry.Size.X,
                        geometry.Size.Y,
                        telegraphDuration,
                        true,
                        action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }

            DeferAbilityStaminaCostContext(activator, trackedImpact?.Ability);

            if (playImpactAnimation)
                PlayCombatImpactAnimation(activator, impactAnimation);

            return 0;
        }

        /// <summary>
        /// Renders a short, purely visual telegraph showing the area an instant ability just struck.
        /// Unlike a pre-cast telegraph this carries no action and does not gate damage, so it gives
        /// positional feedback without changing an ability's Bible-mandated "Instant" activation time.
        /// </summary>
        private void ShowAreaImpactFlash(
            uint activator,
            TelegraphGeometry geometry,
            float flashDuration,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs)
        {
            if (flashDuration <= 0f || geometry.Size.X <= 0f)
                return;
            if (geometry.Shape == TelegraphType.None)
                return;
            if (!_telegraph.ShouldShowImpactFlash(geometry, activationAreaTelegraphs))
                return;

            _telegraph.CreateTelegraph(
                activator,
                geometry.Position,
                geometry.Shape == TelegraphType.Sphere ? 0f : geometry.Rotation,
                geometry.Size,
                flashDuration,
                true,
                geometry.Shape,
                null,
                isImpactFlash: true);
        }

        private int ApplyCombatImpactInShape(
            uint activator,
            TelegraphGeometry geometry,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            int maxTargets,
            int enmityBonus,
            Action<uint> beforeImpact,
            Action<uint> afterSuccessfulHit,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders,
            int hitChancePercentAdjustment,
            int criticalRatePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            DamageType? effectDamageType,
            bool alwaysApplyAreaVisualEffect,
            AbilityType combatImpactDamageAbility,
            bool sendsNoTargetMessage,
            bool resolvesHit,
            bool canCritical,
            bool useUnscaledDamage)
        {
            _impactState.RecordAbilityImpactShape(activator, skillType, true);

            var origin = Location(geometry.Area, geometry.Position, 0f);
            var creatures = _geometry.GetHostileCreaturesInCombatImpactShape(activator, geometry)
                .Where(creature => _geometry.HasAbilityLineOfSight(activator, creature))
                .ToList();

            if (alwaysApplyAreaVisualEffect || creatures.Count > 0)
            {
                if (areaVisualEffect != VisualEffect.None)
                {
                    VisualEffectPlayback.ApplyAtLocation(areaVisualEffect, origin);
                }
            }

            return ApplyCombatImpactToCreatures(
                activator,
                creatures,
                skillType,
                baseDamage,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                maxTargets,
                enmityBonus,
                beforeImpact,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                hitChancePercentAdjustment: hitChancePercentAdjustment,
                criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                useNPCStatScaling: useNPCStatScaling,
                awardsCombatPoints: awardsCombatPoints,
                effectDamageType: effectDamageType,
                combatImpactDamageAbility: combatImpactDamageAbility,
                sendsNoTargetMessage: sendsNoTargetMessage,
                resolvesHit: resolvesHit,
                canCritical: canCritical,
                useUnscaledDamage: useUnscaledDamage);
        }

        private ApplyTelegraphEffect BuildTelegraphedCombatImpactAction(
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatImpactAreaShape shape,
            Location areaVisualLocation,
            AbilityDetail ability,
            AbilityImpactTracker sequenceOwner,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment,
            int nextAttackEnmityBonus,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            Action<AbilityImpactSummary> afterImpactAction,
            int maxTargets,
            int enmityBonus,
            Action<uint> beforeImpact,
            Action<uint> afterSuccessfulHit,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders,
            int hitChancePercentAdjustment,
            int criticalRatePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            DamageType? effectDamageType,
            bool alwaysApplyAreaVisualEffect,
            AbilityType combatImpactDamageAbility,
            bool sendsNoTargetMessage,
            bool resolvesHit,
            bool canCritical,
            bool useUnscaledDamage)
        {
            var resolveDamageBonusesFromOwner = sequenceOwner?.ResolveDamageBonuses != null;
            return (creator, creatures) =>
            {
                var impactStarted = false;
                var impactEnded = false;
                try
                {
                    if (!GetIsObjectValid(creator) || GetCurrentHitPoints(creator) <= 0)
                        return;

                    var hostileCreatures = creatures
                        .Where(creature =>
                            GetIsObjectValid(creature) &&
                            GetIsReactionTypeHostile(creature, creator) &&
                            _geometry.HasAbilityLineOfSight(creator, creature))
                        .ToList();

                    if (maxTargets > 0)
                    {
                        var impactPosition = GetPositionFromLocation(areaVisualLocation);
                        hostileCreatures = CombatImpactShapeGeometry
                            .TakeClosestToOrigin(hostileCreatures, impactPosition, GetPosition, maxTargets)
                            .ToList();
                    }

                    if (hostileCreatures.Count <= 0)
                    {
                        if (alwaysApplyAreaVisualEffect && areaVisualEffect != VisualEffect.None)
                        {
                            VisualEffectPlayback.ApplyAtLocation(areaVisualEffect, areaVisualLocation);
                        }

                        if (sendsNoTargetMessage)
                            _impactMessage.SendCombatImpactNoTargetsMessage(creator, ability);
                        return;
                    }

                    if (ability != null)
                    {
                        var statusAppliedNextAttackDamageBonus = ability.IsHostileAbility
                            ? GetStatusAppliedNextAttackDamageBonus(creator)
                            : 0;
                        BeginAbilityImpact(
                            creator,
                            ability,
                            nextAbilityDamageBonus + statusAppliedNextAttackDamageBonus,
                            nextAbilityCriticalRatePercentAdjustment,
                            nextAbilityDefenseIgnorePercentAdjustment,
                            nextAttackEnmityBonus,
                            statusAppliedNextAttackDamageBonus,
                            countsAsAttackAttempt: false,
                            sequenceOwner: sequenceOwner,
                            resolveDamageBonusesFromOwner: resolveDamageBonusesFromOwner);
                        impactStarted = true;
                        _impactState.RecordAbilityImpactShape(creator, skillType, true);
                    }

                    if (areaVisualEffect != VisualEffect.None)
                    {
                        VisualEffectPlayback.ApplyAtLocation(areaVisualEffect, areaVisualLocation);
                    }

                    ApplyCombatImpactToCreatures(
                        creator,
                        hostileCreatures,
                        skillType,
                        baseDamage,
                        statusEffect,
                        duration,
                        additionalStatusEffects,
                        statusEffectFactory,
                        damageType,
                        statusResistanceType,
                        targetVisualEffect,
                        damagePercentAdjustment,
                        baseDamageAdjustment,
                        maxTargets,
                        enmityBonus,
                        beforeImpact,
                        afterSuccessfulHit,
                        beforeSuccessfulImpactRiders,
                        hitChancePercentAdjustment: hitChancePercentAdjustment,
                        criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                        useNPCStatScaling: useNPCStatScaling,
                        awardsCombatPoints: awardsCombatPoints,
                        effectDamageType: effectDamageType,
                        combatImpactDamageAbility: combatImpactDamageAbility,
                        sendsNoTargetMessage: sendsNoTargetMessage,
                        resolvesHit: resolvesHit,
                        canCritical: canCritical,
                        useUnscaledDamage: useUnscaledDamage);

                    if (ability != null)
                    {
                        var summary = EndAbilityImpact(creator);
                        impactEnded = true;
                        ApplyAbilityImpactEffects(creator, summary);
                        afterImpactAction?.Invoke(summary);
                    }

                }
                finally
                {
                    if (impactStarted && !impactEnded)
                    {
                        AbortAbilityImpact(creator);
                    }

                    CompleteDeferredAbilityStaminaCostContext(creator, ability);
                }
            };
        }

        private int ApplyCombatImpactToCreatures(
            uint activator,
            IEnumerable<uint> creatures,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            int maxTargets = 0,
            int enmityBonus = 0,
            Action<uint> beforeImpact = null,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool sendsNoTargetMessage = true,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false)
        {
            var totalDamage = 0;
            var affectedCount = 0;
            var trackedAbility = _impactState.GetTrackedAbilityImpact(activator)?.Ability;

            var impactTargets = creatures.Distinct().ToList();
            if (beforeImpact != null)
            {
                // Capture prerequisites for the entire target set before damage or riders
                // on any hit can change another target's eligibility during this impact.
                foreach (var creature in impactTargets)
                {
                    if (GetIsObjectValid(creature) && GetIsReactionTypeHostile(creature, activator))
                        beforeImpact(creature);
                }
            }

            foreach (var creature in impactTargets)
            {
                if (!GetIsObjectValid(creature) || !GetIsReactionTypeHostile(creature, activator))
                    continue;

                if (maxTargets > 0 && affectedCount >= maxTargets)
                    break;

                totalDamage += ApplyHostileCombatImpact(
                    activator,
                    creature,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus,
                    afterSuccessfulHit,
                    beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment,
                    criticalRatePercentAdjustment,
                    useNPCStatScaling,
                    awardsCombatPoints,
                    effectDamageType,
                    combatImpactDamageAbility,
                    resolvesHit,
                    canCritical,
                    useUnscaledDamage);
                affectedCount++;
            }

            if (affectedCount <= 0 && sendsNoTargetMessage)
            {
                _impactMessage.SendCombatImpactNoTargetsMessage(activator, trackedAbility);
            }

            return totalDamage;
        }

        /// <summary>Plays the current ability's declared impact gesture independently of target outcomes.</summary>
        public void PlayAbilityImpactAnimation(uint activator) =>
            PlayCombatImpactAnimation(activator, Animation.Invalid);

        /// <summary>
        /// Plays a non-weapon combat impact animation while preserving explicit throw carriers.
        /// </summary>
        private void PlayCombatImpactAnimation(uint activator, Animation impactAnimation)
        {
            var trackedAbility = _impactState.GetTrackedAbilityImpact(activator)?.Ability;

            // Queued weapon abilities resolve inside the engine's landed auto-attack. Playing a
            // scripted impact animation here would enqueue a second swing after the real hit,
            // which becomes especially visible after a lethal attack has already killed its target.
            if (trackedAbility?.ActivationType == AbilityActivationType.Weapon)
                return;

            var animation = impactAnimation == Animation.Invalid
                ? trackedAbility?.ImpactAnimationType ?? Animation.Invalid
                : impactAnimation;

            if (animation == Animation.Invalid)
                return;

            var authoredImpact = SWLOR.Game.Server.Domain.Abilities.AbilityAnimationBinding.ImpactClip(trackedAbility, activator);
            if (authoredImpact != null)
            {
                // Damage and projectile effects are already dispatched by the ability. Use the
                // named one-shot carrier here, including grenades, without queuing another action.
                _namedAnimation.Play(activator, authoredImpact, equipmentRequirement: trackedAbility.AnimationEquipmentRequirement);
                return;
            }

            // Native impact carriers must not reuse a preceding named clip's replacements.
            _namedAnimation.ReleaseForNativePlayback(activator);

            if (trackedAbility?.ImmediateNativeImpactAnimationDuration > 0f)
            {
                AssignCommand(activator, () => PistolAnimationRemap.PlayAnimationPreservingExplicitThrow(
                    activator, animation, 1f, trackedAbility.ImmediateNativeImpactAnimationDuration, immediate: true));
                return;
            }

            var sourceAnimationName = string.Empty;
            var replacementAnimationName = string.Empty;
            var restoreDelaySeconds = 0f;

            if (impactAnimation == Animation.Invalid && trackedAbility != null)
            {
                sourceAnimationName = trackedAbility.ImpactAnimationSourceAnimationName;
                replacementAnimationName = trackedAbility.ImpactAnimationReplacementAnimationName;
                restoreDelaySeconds = trackedAbility.ImpactAnimationRestoreDelaySeconds;
            }

            if (!string.IsNullOrWhiteSpace(sourceAnimationName) &&
                !string.IsNullOrWhiteSpace(replacementAnimationName))
            {
                AssignCommand(activator, () =>
                {
                    PistolAnimationRemap.PlayAnimationWithTemporaryReplacementPreservingExplicitThrow(
                        activator,
                        animation,
                        1.0f,
                        restoreDelaySeconds,
                        sourceAnimationName,
                        replacementAnimationName,
                        restoreDelaySeconds);
                });
                return;
            }

            AssignCommand(
                activator,
                () => PistolAnimationRemap.PlayAnimationPreservingExplicitThrow(
                    activator,
                    animation));
        }

        public int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int damage,
            CombatDamageType damageType,
            Type statusEffect = null,
            int duration = 0,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool firstHostileAbilityHitDamageBonusApplied = false)
        {
            using var damageDerivedHealing = BeginDamageDerivedHealing(activator, target);
            // Read before this hit's damage and enmity can turn the target onto the activator.
            var targetWasAttackingAlly = IsTargetAttackingAllyForStatusAppliedRiders(activator, target);
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            var allowsHitProcs = trackedImpact?.Ability.MeleeTechnique == null ||
                trackedImpact.Sequence.TryTriggerDamageRider($"TechniqueHitProcs:{target}");
            // Hostile abilities raise no attack or damage events when they only apply status effects.
            _music.StartBattleTheme(target, activator);

            // Register the combat point before applying damage. A lethal hit resolves the target's
            // death (and its skill XP distribution) synchronously during EffectDamage below, so
            // registering afterward would miss the payout entirely when an ability one-shots a target.
            if (awardsCombatPoints)
                _combatPoint.AddCombatPoint(activator, target, skillType, 3);

            if (damage > 0)
            {
                ConsumeStatusAppliedNextAttackDamageBonus(trackedImpact, activator);
                _damageDelivery.DeliverHostileImpactDamage(
                    activator,
                    target,
                    skillType,
                    damage,
                    damageType,
                    effectDamageType,
                    trackedImpact,
                    allowsHitProcs);
            }

            ApplyHostileAbilityEnmity(
                activator,
                target,
                Math.Max(0, enmityBonus) + Math.Max(0, trackedImpact?.NextAttackEnmityBonus ?? 0));

            var statusApplied = _statusApplication.ApplyCombatImpactStatusEffect(
                activator,
                target,
                skillType,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                statusResistanceType,
                damageType,
                out var appliedStatusCategories);
            if (statusApplied)
            {
                _darkForce.ApplyDarkForceCastConversion(activator, target);
            }

            // Statuses the hook applies on this hit (conditional statuses) count like the impact's own
            // status, so category-keyed perks such as "after you apply a control effect" see them.
            if (beforeSuccessfulImpactRiders?.Invoke(target) is { } riderStatusCategories)
            {
                statusApplied = true;
                appliedStatusCategories |= riderStatusCategories;
            }

            _capstoneProof.RecordAbilityImpact(activator, target, trackedImpact?.Ability);

            ApplySuccessfulAbilityImpactRiders(
                activator,
                target,
                trackedImpact?.Ability,
                skillType,
                damageType,
                damage,
                statusApplied,
                statusEffect,
                additionalStatusEffects,
                firstHostileAbilityHitDamageBonusApplied,
                trackedImpact == null || trackedImpact.Summary.ImpactedTargetCount == 0,
                appliedStatusCategories,
                allowsHitProcs,
                targetWasAttackingAlly);

            if (damage > 0 && trackedImpact?.Ability is { } impactAbility &&
                impactAbility.DamageImpactVisualEffect != VisualEffect.None)
            {
                var damageVisual = EffectVisualEffect(impactAbility.DamageImpactVisualEffect);
                ApplyEffectToObject(DurationType.Instant, damageVisual, target);
            }

            if (damage > 0 || statusApplied)
            {
                if (trackedImpact?.VisualEffects.Effect is { } authoredEffect && authoredEffect != VisualEffect.None)
                    PlaySuccessfulImpactVisualEffect(activator, target);
                else if (targetVisualEffect != VisualEffect.None)
                    VisualEffectPlayback.ApplyToObject(targetVisualEffect, target);
            }

            afterSuccessfulHit?.Invoke(target);
            _impactState.RecordAbilityImpactTarget(activator, target, skillType, false);
            return damage;
        }

        /// <summary>
        /// An impact that adds weapon DMG also takes that weapon's element, matching auto-attacks.
        /// Like native weapon resolution, an element only replaces Physical; authored non-Physical types stay.
        /// </summary>
        public CombatDamageType GetWeaponAbilityDamageType(uint activator, SkillType skillType, CombatDamageType damageType)
        {
            return ResolveWeaponAbilityDamageType(activator, skillType, damageType, _impactState.GetTrackedAbilityImpact(activator));
        }

        private CombatDamageType ResolveWeaponAbilityDamageType(
            uint activator,
            SkillType skillType,
            CombatDamageType damageType,
            AbilityImpactTracker trackedImpact)
        {
            if (WeaponImpactProfile.Get(activator) is { } explicitProfile)
                return explicitProfile.Weapon.DamageType;
            var usesQueuedNaturalWeapon = _damageInput.UsesQueuedNaturalWeapon(trackedImpact, skillType);
            if (damageType != CombatDamageType.Physical ||
                !IncludesCombatImpactWeaponDamage(skillType, usesQueuedNaturalWeapon))
                return damageType;

            return trackedImpact?.TriggeringWeaponDamageType ??
                   GetCombatImpactWeaponDamageType(activator, skillType, usesQueuedNaturalWeapon);
        }

        private int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false)
        {
            using var damageDerivedHealing = BeginDamageDerivedHealing(activator, target);
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            // Unscaled impacts add no weapon DMG, so they keep their authored type.
            if (!_meleeTechnique.IsImpactValid(activator, target, trackedImpact?.Ability))
                return 0;
            if (!useUnscaledDamage)
                damageType = ResolveWeaponAbilityDamageType(activator, skillType, damageType, trackedImpact);
            TrackHostileAbilityActivity(activator);
            TrackHostileDefensiveCombatEntryActivity(target, activator);
            // Before the hit roll, so a missed ability still starts the battle theme.
            _music.StartBattleTheme(target, activator);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var usesNPCStatScaling = _damageInput.ShouldUseNPCStatScaling(activator, useNPCStatScaling);
            var damageAbility = combatImpactDamageAbility != AbilityType.Invalid
                ? combatImpactDamageAbility
                : trackedImpact?.Ability?.CombatImpactDamageAbility ?? AbilityType.Invalid;
            var appliedStatusCategories = _statusApplication.GetCombatImpactStatusCategories(statusEffect, additionalStatusEffects, statusEffectFactory);
            var statusCategoryHitChanceAdjustment = GetAbilityStatusCategoryHitChancePercentAdjustment(
                activator,
                skillType,
                appliedStatusCategories);
            var shouldResolveHit = resolvesHit && ShouldResolveCombatImpactHit(trackedImpact);
            var hitRate = 100;
            if (shouldResolveHit &&
                !TryResolveAbilityHit(
                    activator,
                    target,
                    skillType,
                    perkType,
                    out hitRate,
                    hitChancePercentAdjustment + statusCategoryHitChanceAdjustment,
                    // -1 keeps NPC ability accuracy on the creature's own level, matching what its
                    // auto-attacks use. GetNPCAbilityScalingRank stays on the damage path only:
                    // feeding it here made a level 40 enemy resolve ability accuracy as rank 11.
                    -1,
                    damageAbility,
                    _meleeTechnique.GetAbilityAccuracy(activator, target, trackedImpact?.Ability)))
            {
                _impactMessage.SendCombatImpactResultMessage(activator, target, trackedImpact?.Ability, 4, hitRate);
                if (awardsCombatPoints)
                    _combatPoint.AddCombatPoint(activator, target, skillType, 1);
                ApplyMissedHostileAbilityEnmity(activator, target);
                return 0;
            }

            if (shouldResolveHit)
                _impactMessage.SendCombatImpactResultMessage(activator, target, trackedImpact?.Ability, 1, hitRate);

            var adjustedBaseDamage = Math.Max(0, baseDamage + (baseDamageAdjustment?.Invoke(target) ?? 0));
            var impactDamage = ResolveCombatImpactBaseDamage(
                adjustedBaseDamage,
                trackedImpact?.Ability,
                () =>
                {
                    trackedImpact?.EnsureDamageBonuses();
                    return GetAbilityImpactBaseDamageBonus(
                          activator, target, trackedImpact?.Ability, skillType) +
                      GetAbilityStatusCategoryDamageBonus(
                          activator, skillType, appliedStatusCategories);
                });
            adjustedBaseDamage = impactDamage.BaseDamage;
            var authoredDamagePercentAdjustment = impactDamage.DealsDamage ? damagePercentAdjustment?.Invoke(target) ?? 0 : 0;
            var lowHPDamagePercentAdjustment = impactDamage.DealsDamage ? _darkForce.GetDarkForceTargetLowHPDamageAdjustment(activator, target) : 0;
            var damage = !impactDamage.DealsDamage ? 0 : useUnscaledDamage
                ? _damageInput.CalculateUnscaledCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType, authoredDamagePercentAdjustment, lowHPDamagePercentAdjustment)
                : usesNPCStatScaling
                    ? _damageStage.CalculateNPCCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType, criticalRatePercentAdjustment, damageAbility, canCritical, authoredDamagePercentAdjustment, lowHPDamagePercentAdjustment)
                    : _damageStage.CalculateCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType, criticalRatePercentAdjustment, damageAbility, canCritical, authoredDamagePercentAdjustment, lowHPDamagePercentAdjustment);
            return ApplyHostileCombatImpact(
                activator,
                target,
                skillType,
                damage,
                damageType,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                statusResistanceType,
                targetVisualEffect,
                enmityBonus,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                awardsCombatPoints,
                effectDamageType,
                firstHostileAbilityHitDamageBonusApplied: impactDamage.DealsDamage);
        }

        private (int BaseDamage, bool DealsDamage) ResolveCombatImpactBaseDamage(
            int baseDamage,
            AbilityDetail ability,
            Func<int> getDamageBonus)
        {
            // Queued weapon damage and explicitly declared deferred damage remain eligible even
            // with zero immediate base damage. Control-only impacts must not consume bonuses.
            var dealsDamage = CanDealCombatImpactDamage(baseDamage, ability);
            return dealsDamage ? (baseDamage + getDamageBonus(), true) : (0, false);
        }

        private bool CanDealCombatImpactDamage(int baseDamage, AbilityDetail ability)
        {
            return baseDamage > 0 || ability?.ActivationType == AbilityActivationType.Weapon ||
                   ability?.DealsDeferredDamage == true;
        }

        /// <summary>
        /// Adds this impact's captured flat bonus to an already-scaled damage payload before
        /// target mitigation. Control-only payloads remain zero.
        /// </summary>
        public int ApplyCapturedAbilityDamageBonus(uint activator, int damage)
        {
            return damage > 0 ? damage + (_impactState.GetTrackedAbilityImpact(activator)?.NextAbilityDamageBonus ?? 0) : 0;
        }

        private void PrepareCombatImpactDamageBonuses(uint activator, int baseDamage)
        {
            var impact = _impactState.GetTrackedAbilityImpact(activator);
            if (impact != null && CanDealCombatImpactDamage(baseDamage, impact.Ability))
                impact.EnsureDamageBonuses();
        }

        private bool ShouldResolveCombatImpactHit(AbilityImpactTracker trackedImpact)
        {
            return trackedImpact?.Ability?.ActivationType != AbilityActivationType.Weapon;
        }

        protected virtual int GetAbilityDMG(uint creature, uint weapon, SkillType abilitySkill) => WeaponDamage.GetAbilityDMG(creature, weapon, abilitySkill);

        protected virtual CombatDamageType GetWeaponDamageType(uint weapon) => WeaponDamage.GetDamageType(weapon);

        protected virtual SkillType GetAbilitySkillType(uint creature, AbilityDetail ability) => _queries.GetAbilitySkillType(creature, ability);

        protected virtual int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType) => _abilityBonus.ConsumeNextAbilityDamageBonus(creature, perkType);

        protected virtual (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(uint creature, SkillType skillType, StatusEffectCategory abilityStatusCategories) =>
            _abilityBonus.ConsumeNextSkillAbilityBonuses(creature, skillType, abilityStatusCategories);

        protected virtual int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType) => _abilityBonus.GetPersistentNextSkillAbilityCriticalRateBonus(creature, skillType);

        protected virtual (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(uint creature, SkillType skillType) =>
            _abilityBonus.ConsumeQueuedWeaponAbilityBonuses(creature, skillType);

        protected virtual (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(uint creature) =>
            _abilityBonus.ConsumeNextAttackGuardedHitBonuses(creature);

        protected virtual int GetStatusAppliedNextAttackDamageBonus(uint creature) => _abilityBonus.GetStatusAppliedNextAttackDamageBonus(creature);

        protected virtual int ConsumeStatusAppliedNextAttackDamageBonus(uint creature) => _abilityBonus.ConsumeStatusAppliedNextAttackDamageBonus(creature);

        protected virtual int GetCombatImpactWeaponDamage(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false, int? triggeringWeaponDamage = null, bool abilityImpact = false) =>
            _queries.GetCombatImpactWeaponDamage(activator, skillType, usesQueuedNaturalWeapon, triggeringWeaponDamage: triggeringWeaponDamage, abilityImpact: abilityImpact);

        protected virtual CombatDamageType GetCombatImpactWeaponDamageType(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon) =>
            _queries.GetCombatImpactWeaponDamageType(activator, skillType, usesQueuedNaturalWeapon);

        protected virtual void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary) => _abilityImpact.ApplyAbilityImpactEffects(activator, summary);

        protected virtual int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability) => _resources.RefundCriticalRangedAbilityStaminaCost(creature, ability);

        protected virtual int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType) => _abilityBonus.ConsumePersistentNextSkillAbilityCriticalRateBonus(creature, skillType);

        protected virtual void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType) => _hitEffects.ApplyNonCriticalAbilityEffects(activator, target, skillType);

        protected virtual void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability) => _resources.DeferAbilityStaminaCostContext(creature, ability);

        protected virtual void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability) => _resources.CompleteDeferredAbilityStaminaCostContext(creature, ability);

        protected virtual IDisposable BeginDamageDerivedHealing(uint creature, uint target) => _resources.BeginDamageDerivedHealing(creature, target);

        protected virtual bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target) => _abilityImpact.IsTargetAttackingAllyForStatusAppliedRiders(activator, target);

        protected virtual void ApplySuccessfulAbilityImpactRiders(uint activator, uint target, AbilityDetail ability, SkillType skillType, CombatDamageType damageType, int damage, bool statusApplied, Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, bool firstHostileAbilityHitDamageBonusApplied, bool isFirstSuccessfulTarget, StatusEffectCategory appliedStatusCategories, bool allowsHitProcs, bool targetWasAttackingAlly) =>
            _abilityImpact.ApplySuccessfulAbilityImpactRiders(activator, target, ability, skillType, damageType, damage, statusApplied, primaryStatusEffect, additionalStatusEffects, firstHostileAbilityHitDamageBonusApplied, isFirstSuccessfulTarget, appliedStatusCategories, allowsHitProcs, targetWasAttackingAlly);

        protected virtual bool IncludesCombatImpactWeaponDamage(SkillType skillType, bool usesQueuedNaturalWeapon) => _queries.IncludesCombatImpactWeaponDamage(skillType, usesQueuedNaturalWeapon);

        protected virtual void TrackHostileAbilityActivity(uint creature) => _combatActivity.TrackHostileAbilityActivity(creature);

        protected virtual void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker) => _combatActivity.TrackHostileDefensiveCombatEntryActivity(creature, attacker);

        protected virtual int GetAbilityStatusCategoryHitChancePercentAdjustment(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) =>
            _abilityBonus.GetAbilityStatusCategoryHitChancePercentAdjustment(creature, skillType, appliedCategories);

        protected virtual bool TryResolveAbilityHit(uint attacker, uint defender, SkillType skillType, PerkType perkType, out int hitRate, int hitChancePercentAdjustment, int skillLevelOverride, AbilityType statOverride, int accuracyFlatAdjustment) =>
            _abilityHit.TryResolveAbilityHit(attacker, defender, skillType, perkType, out hitRate, hitChancePercentAdjustment, skillLevelOverride, statOverride, accuracyFlatAdjustment);

        protected virtual int GetAbilityImpactBaseDamageBonus(uint activator, uint target, AbilityDetail ability, SkillType skillType) => _abilityImpact.GetAbilityImpactBaseDamageBonus(activator, target, ability, skillType);

        protected virtual int GetAbilityStatusCategoryDamageBonus(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) => _abilityBonus.GetAbilityStatusCategoryDamageBonus(creature, skillType, appliedCategories);

        private void ConsumeStatusAppliedNextAttackDamageBonus(AbilityImpactTracker impact, uint activator)
        {
            if (impact == null || impact.StatusAppliedNextAttackDamageBonusConsumed ||
                impact.StatusAppliedNextAttackDamageBonus <= 0)
            {
                return;
            }

            ConsumeStatusAppliedNextAttackDamageBonus(activator);
            impact.CompleteStatusAppliedNextAttackDamageBonusConsumption();
        }
    }
}
