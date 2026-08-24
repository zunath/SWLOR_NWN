using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature
{
    public static class UsePerkFeat
    {
        private enum ActivationStatus
        {
            Invalid = 0,
            Started = 1,
            Interrupted = 2,
            Completed = 3
        }

        private sealed class ActiveAbilityActivation
        {
            public string ActivationId { get; init; }
            public AbilityDetail Ability { get; init; }
            public List<string> TelegraphIds { get; init; }
            public uint ResumeAttackTarget { get; init; }
        }

        // Variable names for queued abilities.
        private const string ActiveAbilityIdName = "ACTIVE_ABILITY_ID";
        private const string ActiveAbilityFeatIdName = "ACTIVE_ABILITY_FEAT_ID";
        private const string ActiveAbilityEffectivePerkLevelName = "ACTIVE_ABILITY_EFFECTIVE_PERK_LEVEL";
        private const string ActiveAbilityWeaponIneffectiveFeedbackSuppressedName = "ACTIVE_ABILITY_WEAPON_INEFFECTIVE_FEEDBACK_SUPPRESSED";
        private const string ActiveAbilityWeaponIneffectiveFeedbackWasHiddenName = "ACTIVE_ABILITY_WEAPON_INEFFECTIVE_FEEDBACK_WAS_HIDDEN";
        private static readonly Dictionary<uint, ActiveAbilityActivation> _activeAbilityActivations = new();

        private static uint GetResumeAttackTarget(uint activator, uint target, AbilityDetail ability)
        {
            if (CompanionControl.IsRegisteredCompanion(activator))
                return CompanionControl.PeekAuthorizedTarget(activator);

            if (GetCurrentAction(activator) == ActionType.AttackObject)
            {
                var attackTarget = GetAttackTarget(activator);
                if (GetIsObjectValid(attackTarget))
                    return attackTarget;
            }

            if (!GetIsPC(activator))
            {
                var enmityTarget = Enmity.GetHighestEnmityTarget(activator);
                if (GetIsObjectValid(enmityTarget))
                    return enmityTarget;

                if (ability.IsHostileAbility &&
                    GetIsObjectValid(target) &&
                    target != activator &&
                    GetIsEnemy(target, activator))
                {
                    return target;
                }
            }

            return OBJECT_INVALID;
        }

        private static (uint Target, Location TargetLocation) ResolveAbilityTarget(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability)
        {
            if (!ability.UsesActiveAttackTarget)
                return (target, targetLocation);

            var attackTarget = GetAttackTarget(activator);
            if (!GetIsObjectValid(attackTarget))
                return (OBJECT_INVALID, targetLocation);

            return (attackTarget, GetLocation(attackTarget));
        }

        private static void ResumeAttack(uint activator, uint target, bool clearActions = true)
        {
            if (!GetIsObjectValid(activator) ||
                GetCurrentHitPoints(activator) <= 0)
            {
                return;
            }

            if (CompanionControl.TryProcessPendingDefensiveReaction(activator))
                return;

            if (CompanionControl.IsRegisteredCompanion(activator) &&
                (!GetIsObjectValid(target) ||
                 CompanionControl.PeekAuthorizedTarget(activator) != target))
            {
                CompanionControl.ResumeModePosition(activator);
                return;
            }

            if (!GetIsPC(activator) && !GetIsPC(GetMaster(activator)))
                target = Enmity.GetHighestEnmityTarget(activator);

            if (!GetIsObjectValid(target) ||
                GetCurrentHitPoints(target) <= 0 ||
                GetArea(activator) != GetArea(target))
                return;

            if (!GetIsPC(activator))
            {
                Enmity.IssueAttackCommand(activator, target, clearActions);
                return;
            }

            AssignCommand(activator, () =>
            {
                ActionAttack(target);
            });
        }

        /// <summary>
        /// Interrupts the creature's current cast or channel, if one is active.
        /// </summary>
        public static bool InterruptAbilityActivation(uint activator)
        {
            return InterruptAbilityActivation(activator, null);
        }

        private static bool InterruptAbilityActivation(uint activator, string expectedActivationId)
        {
            if (!_activeAbilityActivations.TryGetValue(activator, out var activation) ||
                !string.IsNullOrWhiteSpace(expectedActivationId) &&
                activation.ActivationId != expectedActivationId ||
                GetLocalInt(activator, activation.ActivationId) != (int)ActivationStatus.Started)
            {
                return false;
            }

            Log.WriteStructured(
                LogGroup.Server,
                "Ability activation interrupted: Activator={Activator} ActivationId={ActivationId} Ability={Ability} IsChanneled={IsChanneled}",
                activator,
                activation.ActivationId,
                activation.Ability.Name,
                activation.Ability.IsChanneled);

            RemoveEffectByTag(activator, "ACTIVATION_VFX");
            CancelActivationTargetingTelegraphs(activation.TelegraphIds);
            if (GetIsPC(activator))
                PlayerPlugin.StopGuiTimingBar(activator, string.Empty);

            Messaging.SendMessageNearbyToPlayers(
                activator,
                receiver => $"{PlayerName.GetDisplayName(receiver, activator)}'s ability has been interrupted.");
            SetLocalInt(activator, activation.ActivationId, (int)ActivationStatus.Interrupted);
            Activity.ClearBusy(activator);
            ClearAbilityActivationIdleSnapshots(activator);

            if (activation.Ability.IsChanneled)
                activation.Ability.ChannelInterruptAction?.Invoke(activator);

            _activeAbilityActivations.Remove(activator);
            ResumeAttack(activator, activation.ResumeAttackTarget);
            return true;
        }

        private static void ClearAbilityActivationIdleSnapshots(uint activator)
        {
            Combat.ClearAbilityActivationIdleBonuses(activator);
            Combat.ClearWeaponAbilityActivationIdleBonuses(activator);
        }

        private static void ClearActiveAbilityActivation(uint activator, string activationId)
        {
            if (_activeAbilityActivations.TryGetValue(activator, out var activation) &&
                activation.ActivationId == activationId)
            {
                _activeAbilityActivations.Remove(activator);
            }
        }

        private static void ResumeAttackAfterDelay(uint activator, uint target, float delay, bool clearActions = true)
        {
            // Autonomous NPCs reacquire their highest-enmity target inside ResumeAttack. Schedule
            // the callback even when the target saved before the cast has since become invalid.
            var isPlayerControlled = GetIsPC(activator) || GetIsPC(GetMaster(activator));
            if (!GetIsObjectValid(target) &&
                isPlayerControlled &&
                !CompanionControl.IsRegisteredCompanion(activator))
            {
                return;
            }

            DelayCommand(delay, () =>
            {
                ResumeAttack(activator, target, clearActions);
            });
        }

        /// <summary>
        /// Breaks stealth and invisibility effects if the ability is configured to do so.
        /// </summary>
        /// <param name="activator">The creature using the ability</param>
        /// <param name="ability">The ability details</param>
        private static void HandleStealthBreaking(uint activator, AbilityDetail ability)
        {
            if (!ability.BreaksStealth) return;

            Combat.TrackStealthOpeningWindow(activator);

            // If activator is in stealth mode, force them out of stealth mode.
            if (GetActionMode(activator, ActionMode.Stealth))
                SetActionMode(activator, ActionMode.Stealth, false);

            // Remove invisibility effects (stealth generator)
            RemoveEffect(activator, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility);
        }

        /// <summary>
        /// When a creature uses any feat, this will check and see if the feat is registered with the perk system.
        /// If it is, requirements to use the feat will be checked and then the ability will activate.
        /// If there are errors at any point in this process, the creature will be notified and the execution will end.
        /// </summary>
        [NWNEventHandler(ScriptName.OnFeatUseBefore)]
        public static void UseFeat()
        {
            var activator = OBJECT_SELF;
            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            var targetArea = StringToObject(EventsPlugin.GetEventData("AREA_OBJECT_ID"));
            var targetPosition = Vector3(
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_X")),
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Y")),
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Z"))
            );

            // If we have a valid target, use its position
            if (GetIsObjectValid(target))
            {
                targetPosition = GetPosition(target);
            }

            var targetLocation = Location(targetArea, targetPosition, 0.0f);

            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));
            TryUseAbility(activator, target, feat, targetLocation, true);
        }

        /// <summary>
        /// Returns true if the given weapon-activated ability is currently queued on the
        /// creature, waiting for its next landed hit.
        /// </summary>
        public static bool IsWeaponAbilityQueued(uint creature, FeatType feat)
        {
            return GetLocalInt(creature, ActiveAbilityFeatIdName) == (int)feat;
        }

        public static bool TryUseAbility(
            uint activator,
            uint target,
            FeatType feat,
            Location targetLocation,
            bool skipNativeEvent = false)
        {
            if (!Ability.IsFeatRegistered(feat))
                return false;

            var ability = Ability.GetAbilityDetail(feat);
            if (skipNativeEvent)
                EventsPlugin.SkipEvent();

            (target, targetLocation) = ResolveAbilityTarget(
                activator,
                target,
                targetLocation,
                ability);

            // Creature cannot use the feat.
            var effectivePerkLevel =
                ability.EffectiveLevelPerkType == PerkType.Invalid
                    ? 1 // If there's not an associated perk, default level to 1.
                    : Perk.GetPerkLevel(activator, ability.EffectiveLevelPerkType);

            // Weapon abilities are queued for the next time the activator's attack lands on an enemy.
            if (ability.ActivationType == AbilityActivationType.Weapon)
            {
                if (Ability.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                {
                    Combat.ClearQueuedWeaponAbilityActivationBonuses(activator);
                    var executeQueue = ability.ActivationAction == null ||
                                       ability.ActivationAction.Invoke(
                                           activator,
                                           target,
                                           ability.AbilityLevel,
                                           targetLocation);
                    if (!executeQueue)
                    {
                        Combat.ClearQueuedWeaponAbilityActivationBonuses(activator);
                        Combat.ClearQueuedWeaponAbilityAttemptBonuses(activator);
                        ClearAbilityActivationIdleSnapshots(activator);
                        return true;
                    }

                    if(ability.DisplaysActivationMessage)
                        Messaging.SendMessageNearbyToPlayers(
                            activator,
                            receiver => $"{PlayerName.GetDisplayName(receiver, activator)} queues {ability.Name} for the next attack.");
                    QueueWeaponAbility(activator, target, ability, feat);
                    Combat.TrackQueuedWeaponAbilityUse(activator, ability);
                    return true;
                }
            }
            // All other abilities are funneled through the same process.
            else
            {
                if (Ability.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                {
                    if (GetIsObjectValid(target) && target != activator)
                    {
                        if (ability.DisplaysActivationMessage)
                            Messaging.SendMessageNearbyToPlayers(
                                activator,
                                receiver => $"{PlayerName.GetDisplayName(receiver, activator)} readies {ability.Name} on {PlayerName.GetDisplayName(receiver, target)}.");
                    }
                    else
                    {
                        if (ability.DisplaysActivationMessage)
                            Messaging.SendMessageNearbyToPlayers(
                                activator,
                                receiver => $"{PlayerName.GetDisplayName(receiver, activator)} readies {ability.Name}.");
                    }

                    ActivateAbility(activator, target, feat, ability, targetLocation);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Applies effects to the activator for each requirement.
        /// Depending on the ability type, this may be called before or after the ability has finished.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <param name="ability">The ability details</param>
        private static void ApplyRequirementEffects(uint activator, AbilityDetail ability)
        {
            foreach (var req in ability.Requirements)
            {
                req.AfterActivationAction(activator, ability);
            }
        }

        private static void PlayAbilitySound(uint activator, string soundResref)
        {
            if (!GetIsObjectValid(activator) ||
                string.IsNullOrWhiteSpace(soundResref))
            {
                return;
            }

            AssignCommand(activator, () => PlaySound(soundResref));
        }

        private static void ExecuteAbilityImpact(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            Location targetLocation)
        {
            var impactEnded = false;
            try
            {
                PlayAbilitySound(activator, ability.ImpactSound);
                Ability.BeginAbilityImpact(activator, ability);
                ability.ImpactAction?.Invoke(activator, target, ability.AbilityLevel, targetLocation);
                var summary = Ability.EndAbilityImpact(activator);
                impactEnded = true;

                Combat.ApplyAbilityActivatedEffects(activator, target, feat, ability, summary);
                Combat.ApplyAbilityImpactEffects(activator, summary);

                if (!GetIsPC(activator))
                {
                    Mimicry.OnCreatureAbilityUsed(activator, feat);
                }
            }
            finally
            {
                if (!impactEnded)
                {
                    Ability.AbortAbilityImpact(activator);
                }

                Combat.CompleteAbilityStaminaCostContext(activator, ability);
            }
        }


        /// <summary>
        /// Handles casting abilities. These can be combat-related or casting-related and may or may not have a casting delay.
        /// Requirement reductions (FP, STM, etc) are applied after the casting has completed.
        /// In the event there is no casting delay, the reductions are applied immediately.
        /// </summary>
        /// <param name="activator">The creature activating the ability.</param>
        /// <param name="target">The target of the ability</param>
        /// <param name="feat">The type of feat associated with this ability.</param>
        /// <param name="ability">The ability details</param>
        /// <param name="targetLocation">The targeted location</param>
        private static void ActivateAbility(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            Location targetLocation)
        {
            float CalculateActivationDelay()
            {
                var delayReductionPercent = Combat.ConsumeNextAbilityDelayReductionPercent(activator, ability);
                if (delayReductionPercent >= 100)
                    return 0f;

                var abilityDelay = ability.ActivationDelay?.Invoke(activator, target, ability.AbilityLevel) ?? 0.0f;
                var delayAdjustment = Stat.GetStatAdjustment(activator, StatType.ActivationDelayFlatAdjustment);
                var delay = Math.Max(0f, abilityDelay + delayAdjustment);

                if (delayReductionPercent > 0)
                    delay *= (100 - delayReductionPercent) / 100f;

                return delay;
            }

            // Handles displaying animation and visual effects.
            List<string> ProcessAnimationAndVisualEffects(float delay)
            {
                /// <summary>
                /// Plays the configured activation animation without corrupting explicit throws.
                /// </summary>
                void PlayActivationAnimation(float animationLength)
                {
                    var sourceAnimationName = ability.AnimationSourceAnimationName;
                    var replacementAnimationName = ability.AnimationReplacementAnimationName;

                    if (!string.IsNullOrWhiteSpace(sourceAnimationName) &&
                        !string.IsNullOrWhiteSpace(replacementAnimationName))
                    {
                        AssignCommand(activator, () =>
                        {
                            PistolAnimationRemap.PlayAnimationWithTemporaryReplacementPreservingExplicitThrow(
                                activator,
                                ability.AnimationType,
                                1.0f,
                                animationLength,
                                sourceAnimationName,
                                replacementAnimationName,
                                ability.AnimationRestoreDelaySeconds);
                        });
                        return;
                    }

                    AssignCommand(
                        activator,
                        () => PistolAnimationRemap.PlayAnimationPreservingExplicitThrow(
                            activator,
                            ability.AnimationType,
                            1.0f,
                            animationLength));
                }

                // Force out of stealth unless the activation must inspect or toggle the current
                // stealth mode when its impact runs.
                if (!ability.PreservesStealthDuringActivation &&
                    GetActionMode(activator, ActionMode.Stealth))
                    SetActionMode(activator, ActionMode.Stealth, false);

                AssignCommand(activator, () =>
                {
                    if (GetIsPC(activator))
                        ClearAllActions();
                    else
                        ClearAllActions(true);
                });

                if (GetIsObjectValid(target) && target != activator)
                {
                    BiowarePosition.TurnToFaceObject(target, activator);
                }
                else if (GetIsObjectValid(GetAreaFromLocation(targetLocation)))
                {
                    BiowarePosition.TurnToFaceLocation(targetLocation, activator);
                }

                PlayAbilitySound(activator, ability.ActivationSound);

                // Display a casting visual effect if one has been specified.
                if (ability.ActivationVisualEffect != VisualEffect.None)
                {
                    var vfx = TagEffect(EffectVisualEffect(ability.ActivationVisualEffect), "ACTIVATION_VFX");
                    ApplyEffectToObject(DurationType.Temporary, vfx, activator, delay + 0.2f);
                }

                // Casted types play an animation of casting.
                if (ability.ActivationType == AbilityActivationType.Casted &&
                    ability.AnimationType != Animation.Invalid)
                {
                    var animationLength = delay - 0.2f;
                    if (animationLength < 0f)
                        animationLength = 0f;

                    PlayActivationAnimation(animationLength);
                }

                return DisplayActivationTargetingTelegraphs(activator, target, targetLocation, ability, delay);
            }

            // Recursive function which checks if player has moved since starting the casting.
            void CheckForActivationInterruption(string activationId, Vector3 originalPosition, List<string> activationTelegraphIds, uint resumeAttackTarget)
            {
                if (!GetIsPC(activator)) return;

                // Completed and externally interrupted abilities should no longer run.
                var status = GetLocalInt(activator, activationId);
                if (status != (int)ActivationStatus.Started) return;

                var currentPosition = GetPosition(activator);

                if (currentPosition.X != originalPosition.X ||
                    currentPosition.Y != originalPosition.Y ||
                    currentPosition.Z != originalPosition.Z)
                {
                    InterruptAbilityActivation(activator, activationId);
                    return;
                }

                DelayCommand(0.5f, () => CheckForActivationInterruption(activationId, originalPosition, activationTelegraphIds, resumeAttackTarget));
            }

            // This method is called after the delay of the ability has finished.
            void CompleteActivation(string activationId, float abilityRecastDelay, uint resumeAttackTarget, List<string> activationTelegraphIds)
            {
                void CancelActivation(bool resumeAttack)
                {
                    ClearActiveAbilityActivation(activator, activationId);
                    DeleteLocalInt(activator, activationId);
                    CancelActivationTargetingTelegraphs(activationTelegraphIds);

                    if (resumeAttack)
                        ResumeAttack(activator, resumeAttackTarget);
                }

                // Interrupted activations were already fully released (busy cleared, channel effects
                // ended, attack resumed) the moment the interruption was detected, and the activator
                // may have started a new activation since - don't clear that one's busy state.
                if (GetLocalInt(activator, activationId) == (int)ActivationStatus.Interrupted)
                {
                    ClearActiveAbilityActivation(activator, activationId);
                    DeleteLocalInt(activator, activationId);
                    return;
                }

                Activity.ClearBusy(activator);

                // Activator died during casting. Cancel the activation.
                var activatorIsAlive = GetCurrentHitPoints(activator) > 0;
                if (!activatorIsAlive)
                {
                    ClearAbilityActivationIdleSnapshots(activator);
                    CancelActivation(false);
                    return;
                }

                // Channeled abilities applied their impact, costs, and recast when the channel
                // started; completing the channel only releases the activator.
                if (ability.IsChanneled)
                {
                    ClearAbilityActivationIdleSnapshots(activator);
                    CancelActivation(true);
                    return;
                }

                if (ability.IsHostileAbility &&
                    CompanionControl.IsRegisteredCompanion(activator) &&
                    !CompanionControl.IsHostileAbilityTargetAuthorized(activator, ability, target))
                {
                    ClearAbilityActivationIdleSnapshots(activator);
                    CancelActivation(true);
                    return;
                }

                var effectivePerkLevel =
                    ability.EffectiveLevelPerkType == PerkType.Invalid
                        ? 1
                        : Perk.GetPerkLevel(activator, ability.EffectiveLevelPerkType);

                if (!Ability.CanUseAbility(activator, target, feat, effectivePerkLevel, targetLocation))
                {
                    ClearAbilityActivationIdleSnapshots(activator);
                    CancelActivation(true);
                    return;
                }

                CancelActivation(false);

                ApplyRequirementEffects(activator, ability);
                HandleStealthBreaking(activator, ability);
                ExecuteAbilityImpact(activator, target, feat, ability, targetLocation);
                Recast.ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay);
                ResumeAttackAfterDelay(activator, resumeAttackTarget, 0.1f);

                // If this is an attack make the NPC react.
                if (GetIsObjectValid(target) && target != activator)
                {
                    Enmity.AttackHighestEnmityTarget(target);
                }

            }

            // Begin the main process
            var activationId = Guid.NewGuid().ToString();
            var activationDelay = CalculateActivationDelay();
            var recastDelay = Combat.ApplyAbilityRecastDelayModifiers(
                activator,
                ability,
                ability.RecastDelay?.Invoke(activator) ?? 0f);
            var position = GetPosition(activator);
            var resumeAttackTarget = GetResumeAttackTarget(activator, target, ability);
            var activationTelegraphIds = ProcessAnimationAndVisualEffects(activationDelay);
            SetLocalInt(activator, activationId, (int)ActivationStatus.Started);
            _activeAbilityActivations[activator] = new ActiveAbilityActivation
            {
                ActivationId = activationId,
                Ability = ability,
                TelegraphIds = activationTelegraphIds,
                ResumeAttackTarget = resumeAttackTarget
            };
            CheckForActivationInterruption(activationId, position, activationTelegraphIds, resumeAttackTarget);

            var executeImpact = ability.ActivationAction == null
                ? true
                : ability.ActivationAction?.Invoke(activator, target, ability.AbilityLevel, targetLocation);

            if (executeImpact != true)
            {
                ClearAbilityActivationIdleSnapshots(activator);
                ClearActiveAbilityActivation(activator, activationId);
                DeleteLocalInt(activator, activationId);
                CancelActivationTargetingTelegraphs(activationTelegraphIds);
                ResumeAttack(activator, resumeAttackTarget);
                return;
            }

            if (GetIsPC(activator))
            {
                if (activationDelay > 0.0f)
                {
                    PlayerPlugin.StartGuiTimingBar(activator, activationDelay, string.Empty);
                }
            }

            // Channeled abilities grant their effects for the length of the channel, so the impact,
            // costs, and recast delay apply up front. Interruption ends the effects early via the
            // channel interrupt action but does not refund the recast delay.
            if (ability.IsChanneled)
            {
                ApplyRequirementEffects(activator, ability);
                HandleStealthBreaking(activator, ability);
                ExecuteAbilityImpact(activator, target, feat, ability, targetLocation);
                Recast.ApplyRecastDelay(activator, ability.RecastGroup, recastDelay);
            }

            Activity.SetBusy(activator, ActivityStatusType.AbilityActivation);
            DelayCommand(activationDelay, () => CompleteActivation(activationId, recastDelay, resumeAttackTarget, activationTelegraphIds));
        }

        /// <summary>
        /// Handles queuing a weapon ability for the activator's next attack.
        /// Local variables are set on the activator which are picked up the next time the activator's weapon hits a target.
        /// If the activator does not hit a target within 30 seconds, the queued ability wears off automatically.
        /// Requirement reductions (FP, STM, etc) are applied as soon as the ability is queued.
        /// </summary>
        /// <param name="activator">The creature activating the ability.</param>
        /// <param name="ability">The ability details</param>
        /// <param name="feat">The feat being activated</param>
        private static void QueueWeaponAbility(uint activator, uint target, AbilityDetail ability, FeatType feat)
        {
            var abilityId = Guid.NewGuid().ToString();
            var resumeAttackTarget = GetResumeAttackTarget(activator, target, ability);

            RestoreQueuedAbilityFeedback(activator);

            // Assign local variables which will be picked up on the next weapon OnHit event by this player.
            SetLocalString(activator, ActiveAbilityIdName, abilityId);
            SetLocalInt(activator, ActiveAbilityFeatIdName, (int)feat);
            SetLocalInt(activator, ActiveAbilityEffectivePerkLevelName, ability.AbilityLevel);
            SuppressQueuedAbilityFeedback(activator);

            ApplyRequirementEffects(activator, ability);

            var abilityRecastDelay = Combat.ApplyAbilityRecastDelayModifiers(
                activator,
                ability,
                ability.RecastDelay?.Invoke(activator) ?? 0.0f);
            Recast.ApplyRecastDelay(activator, ability.RecastGroup, abilityRecastDelay);

            // Activator must attack within 30 seconds after queueing or else it wears off.
            DelayCommand(30.0f, () =>
            {
                DequeueWeaponAbility(activator, ability.DisplaysActivationMessage, abilityId);
            });

            // Weapon abilities are queued for the next hit, so AI users need to resume attacking.
            ResumeAttackAfterDelay(activator, resumeAttackTarget, 0.1f);
        }

        public static void DequeueWeaponAbility(uint target, bool sendMessage = true, string expectedAbilityId = null)
        {
            var abilityId = GetLocalString(target, ActiveAbilityIdName);
            if (string.IsNullOrWhiteSpace(abilityId))
                return;

            if (!string.IsNullOrWhiteSpace(expectedAbilityId) && abilityId != expectedAbilityId)
                return;

            var featId = GetLocalInt(target, ActiveAbilityFeatIdName);
            if (featId == 0)
            {
                ClearQueuedAbility(target);
                return;
            }

            var featType = (FeatType)featId;
            if (!Ability.IsFeatRegistered(featType))
            {
                ClearQueuedAbility(target);
                return;
            }

            var abilityDetail = Ability.GetAbilityDetail(featType);
            ClearQueuedAbility(target);

            // Notify the activator and nearby players
            SendMessageToPC(target, $"Your weapon ability {abilityDetail.Name} is no longer queued.");

            if (sendMessage)
                Messaging.SendMessageNearbyToPlayers(
                    target,
                    receiver => $"{PlayerName.GetDisplayName(receiver, target)} no longer has weapon ability {abilityDetail.Name} readied.");
        }

        public static bool HasQueuedWeaponAbility(uint activator)
        {
            return TryGetQueuedWeaponAbility(activator, out _);
        }

        public static bool HasQueuedWeaponAbility(uint activator, SkillType weaponSkillType)
        {
            return TryGetQueuedWeaponAbility(activator, out var ability) &&
                   Combat.CanWeaponSkillTriggerAbility(weaponSkillType, ability.SkillType);
        }

        public static bool TryGetQueuedWeaponAbility(
            uint activator,
            SkillType weaponSkillType,
            out AbilityDetail ability)
        {
            if (!TryGetQueuedWeaponAbility(activator, out ability) ||
                !Combat.CanWeaponSkillTriggerAbility(weaponSkillType, ability.SkillType))
            {
                ability = null;
                return false;
            }

            return true;
        }

        public static bool TryGetQueuedWeaponAbility(uint activator, out AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator))
            {
                ability = null;
                return false;
            }

            var abilityId = GetLocalString(activator, ActiveAbilityIdName);
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                ability = null;
                return false;
            }

            var activeWeaponAbility = (FeatType)GetLocalInt(activator, ActiveAbilityFeatIdName);
            if (!Ability.IsFeatRegistered(activeWeaponAbility))
            {
                ClearQueuedAbility(activator);
                ability = null;
                return false;
            }

            ability = Ability.GetAbilityDetail(activeWeaponAbility);
            if (ability.ActivationType == AbilityActivationType.Weapon &&
                IsQueuedWeaponAbilityStillAvailable(activator, activeWeaponAbility, ability))
            {
                return true;
            }

            ClearQueuedAbility(activator);
            ability = null;
            return false;
        }

        private static bool IsQueuedWeaponAbilityStillAvailable(
            uint activator,
            FeatType feat,
            AbilityDetail ability)
        {
            if (!GetHasFeat(feat, activator))
                return false;

            var effectivePerkLevel =
                ability.EffectiveLevelPerkType == PerkType.Invalid
                    ? 1
                    : Perk.GetPerkLevel(activator, ability.EffectiveLevelPerkType);
            if (effectivePerkLevel <= 0 || ability.AbilityLevel > effectivePerkLevel)
                return false;

            return !Perk.ShouldEnforceActiveAbilityFeatReplacement(
                       activator,
                       ability.EffectiveLevelPerkType) ||
                   Perk.IsCurrentActiveAbilityFeat(
                       feat,
                       ability.EffectiveLevelPerkType,
                       effectivePerkLevel);
        }

        private static List<string> DisplayActivationTargetingTelegraphs(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability,
            float activationDelay)
        {
            var telegraphIds = new List<string>();

            if (activationDelay <= 0f)
                return telegraphIds;

            AddActivationTargetingTelegraph(
                telegraphIds,
                activator,
                target,
                targetLocation,
                ability,
                ability.Targeting,
                activationDelay);

            foreach (var targeting in ability.AdditionalActivationTargeting)
            {
                AddActivationTargetingTelegraph(
                    telegraphIds,
                    activator,
                    target,
                    targetLocation,
                    ability,
                    targeting,
                    activationDelay);
            }

            return telegraphIds;
        }

        private static void AddActivationTargetingTelegraph(
            List<string> telegraphIds,
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability,
            AbilityTargetingDetail targeting,
            float activationDelay)
        {
            if (targeting == null)
                return;

            var sizeX = targeting.ResolveSizeX(activator, true);
            var sizeY = targeting.ResolveSizeY();

            if (sizeX <= 0f || sizeY < 0f)
                return;

            var position = ResolveActivationTargetingPosition(activator, target, targetLocation, targeting);
            var rotation = ResolveActivationTargetingRotation(activator, target, targetLocation);
            var isHostile = ability.IsHostileAbility || targeting.Flags.HasFlag(AbilityTargetingFlags.HarmsEnemies);
            var telegraphId = string.Empty;

            switch (targeting.Shape)
            {
                case AbilityTargetingShapeType.Sphere:
                case AbilityTargetingShapeType.HSphere:
                    telegraphId = Telegraph.CreateSphereTelegraph(
                        activator,
                        position,
                        sizeX,
                        activationDelay,
                        isHostile,
                        null);
                    break;
                case AbilityTargetingShapeType.Rect:
                    telegraphId = Telegraph.CreateLineTelegraph(
                        activator,
                        position,
                        rotation,
                        sizeX,
                        sizeY,
                        activationDelay,
                        isHostile,
                        null);
                    break;
                case AbilityTargetingShapeType.Cone:
                    telegraphId = Telegraph.CreateConeTelegraph(
                        activator,
                        position,
                        rotation,
                        sizeX,
                        sizeY,
                        activationDelay,
                        isHostile,
                        null);
                    break;
                case AbilityTargetingShapeType.None:
                default:
                    return;
            }

            if (!string.IsNullOrWhiteSpace(telegraphId))
                telegraphIds.Add(telegraphId);
        }

        private static void CancelActivationTargetingTelegraphs(List<string> telegraphIds)
        {
            foreach (var telegraphId in telegraphIds)
            {
                if (string.IsNullOrWhiteSpace(telegraphId))
                    continue;

                Telegraph.CancelTelegraph(telegraphId);
            }
        }

        private static Vector3 ResolveActivationTargetingPosition(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityTargetingDetail targeting)
        {
            if (targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf))
                return GetPosition(activator);

            if (GetIsObjectValid(target))
                return GetPosition(target);

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? GetPositionFromLocation(targetLocation)
                : GetPosition(activator);
        }

        private static Vector3 ResolveActivationTargetingDestination(
            uint activator,
            uint target,
            Location targetLocation)
        {
            if (GetIsObjectValid(target))
                return GetPosition(target);

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? GetPositionFromLocation(targetLocation)
                : GetPosition(activator);
        }

        private static float ResolveActivationTargetingRotation(
            uint activator,
            uint target,
            Location targetLocation)
        {
            var origin = GetPosition(activator);
            var destination = ResolveActivationTargetingDestination(activator, target, targetLocation);
            var deltaX = destination.X - origin.X;
            var deltaY = destination.Y - origin.Y;

            if (Math.Abs(deltaX) <= 0.01f && Math.Abs(deltaY) <= 0.01f)
                return GetFacing(activator) * ((float)Math.PI / 180f);

            return (float)Math.Atan2(deltaY, deltaX);
        }

        /// <summary>
        /// When a player's weapon hits a target, if an ability is queued, that ability will be executed.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void ProcessQueuedWeaponAbility()
        {
            var activator = OBJECT_SELF;
            if (!GetIsObjectValid(activator)) return;

            var target = GetSpellTargetObject();
            var targetLocation = GetLocation(target);
            var item = GetSpellCastItem();

            // If this method was triggered by our own armor (from getting hit), return.
            if (GetBaseItemType(item) == BaseItem.Armor) return;

            var activeAbilityEffectivePerkLevel = GetLocalInt(activator, ActiveAbilityEffectivePerkLevelName);

            if (!TryGetQueuedWeaponAbility(activator, out var abilityDetail))
                return;

            var activeWeaponAbility = (FeatType)GetLocalInt(activator, ActiveAbilityFeatIdName);
            if (!Combat.CanItemTriggerWeaponAbility(item, abilityDetail.SkillType))
                return;

            HandleStealthBreaking(activator, abilityDetail);
            var impactEnded = false;
            try
            {
                Ability.BeginAbilityImpact(activator, abilityDetail);
                abilityDetail.ImpactAction?.Invoke(activator, target, activeAbilityEffectivePerkLevel, targetLocation);
                var summary = Ability.EndAbilityImpact(activator);
                impactEnded = true;
                Combat.ApplyAbilityActivatedEffects(activator, target, activeWeaponAbility, abilityDetail, summary);
                Combat.ApplyAbilityImpactEffects(activator, summary);

                if (!GetIsPC(activator))
                {
                    Mimicry.OnCreatureAbilityUsed(activator, activeWeaponAbility);
                }
            }
            finally
            {
                if (!impactEnded)
                {
                    Ability.AbortAbilityImpact(activator);
                }

                Combat.CompleteAbilityStaminaCostContext(activator, abilityDetail);
                DeleteLocalString(activator, ActiveAbilityIdName);
                DeleteLocalInt(activator, ActiveAbilityFeatIdName);
                DeleteLocalInt(activator, ActiveAbilityEffectivePerkLevelName);
                DelayCommand(0.2f, () => RestoreQueuedAbilityFeedbackIfNoQueuedAbility(activator));
            }
        }

        /// <summary>
        /// Whenever a player enters the server, any temporary variables related to ability execution
        /// will be removed from their PC.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ClearTemporaryQueuedVariables()
        {
            var player = GetEnteringObject();

            ClearQueuedAbility(player);
        }

        /// <summary>
        /// Whenever a player starts resting, clear any queued abilities.
        /// </summary>
        [NWNEventHandler(ScriptName.OnRestStarted)]
        public static void ClearTemporaryQueuedVariablesOnRest()
        {
            ClearQueuedAbility(OBJECT_SELF);
        }

        /// <summary>
        /// Whenever a player equips an item, clear any queued abilities.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        public static void ClearTemporaryQueuedVariablesOnEquip()
        {
            ClearQueuedAbility(OBJECT_SELF);
        }

        /// <summary>
        /// Clears the queued ability of a player.
        /// </summary>
        /// <param name="player">The player to clear</param>
        private static void ClearQueuedAbility(uint player)
        {
            Combat.ClearQueuedWeaponAbilityActivationBonuses(player);
            Combat.ClearQueuedWeaponAbilityAttemptBonuses(player);
            var featType = (FeatType)GetLocalInt(player, ActiveAbilityFeatIdName);
            if (Ability.IsFeatRegistered(featType))
            {
                Combat.CompleteAbilityStaminaCostContext(player, Ability.GetAbilityDetail(featType));
            }

            RestoreQueuedAbilityFeedback(player);
            DeleteLocalString(player, ActiveAbilityIdName);
            DeleteLocalInt(player, ActiveAbilityFeatIdName);
            DeleteLocalInt(player, ActiveAbilityEffectivePerkLevelName);
        }

        private static void SuppressQueuedAbilityFeedback(uint player)
        {
            if (!GetIsPC(player))
                return;

            var wasHidden = FeedbackPlugin.GetFeedbackMessageHidden(FeedbackMessageTypes.CombatWeaponNotEffective, player);
            SetLocalInt(player, ActiveAbilityWeaponIneffectiveFeedbackSuppressedName, 1);
            SetLocalInt(player, ActiveAbilityWeaponIneffectiveFeedbackWasHiddenName, wasHidden ? 1 : 0);
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackMessageTypes.CombatWeaponNotEffective, true, player);
        }

        private static void RestoreQueuedAbilityFeedback(uint player)
        {
            if (!GetIsPC(player))
                return;

            if (GetLocalInt(player, ActiveAbilityWeaponIneffectiveFeedbackSuppressedName) == 0)
                return;

            var wasHidden = GetLocalInt(player, ActiveAbilityWeaponIneffectiveFeedbackWasHiddenName) != 0;
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackMessageTypes.CombatWeaponNotEffective, wasHidden, player);
            DeleteLocalInt(player, ActiveAbilityWeaponIneffectiveFeedbackSuppressedName);
            DeleteLocalInt(player, ActiveAbilityWeaponIneffectiveFeedbackWasHiddenName);
        }

        private static void RestoreQueuedAbilityFeedbackIfNoQueuedAbility(uint player)
        {
            if (!string.IsNullOrWhiteSpace(GetLocalString(player, ActiveAbilityIdName)))
                return;

            RestoreQueuedAbilityFeedback(player);
        }
    }
}
