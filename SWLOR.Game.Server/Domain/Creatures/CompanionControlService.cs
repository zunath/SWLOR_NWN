using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using AssociateCommand = SWLOR.NWN.API.NWScript.Enum.Associate.Command;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Companion control: the follow, guard, stand-ground and attack-nearest modes of a player's droid or beast, the
    /// authorized target it may engage and its hostile abilities. Moved here unchanged from the static
    /// <c>Service.CompanionControl</c>; the facade still carries the event handlers and forwards every call to the
    /// container's instance.
    /// </summary>
    public class CompanionControlService : ICompanionControlService
    {
        private const float FollowDistanceMeters = 2f;
        private const float GuardFollowDistanceMeters = 3f;
        private const float AttackRangeToleranceMeters = 0.25f;
        private const int MaximumNearestCreatureChecks = 64;
        // The native associate radial combines its Toggle label with base TLK 8127 ("casting").
        public const int AssociateAbilitiesLabelStrRef = 8127;

        private sealed class CompanionControlState
        {
            public CompanionMode Mode { get; set; } = CompanionMode.Follow;
            public bool AbilitiesEnabled { get; set; } = true;
            public uint AttackNearestTarget { get; set; } = OBJECT_INVALID;
            public uint OwnerAssistTarget { get; set; } = OBJECT_INVALID;
            public Dictionary<uint, DateTime> DefensiveThreats { get; } = new();
            public uint TrackedTarget { get; set; } = OBJECT_INVALID;
            public float LastDistanceToTarget { get; set; } = float.MaxValue;
            public DateTime LastProgressAt { get; set; }
            public DateTime LastOffensiveActivityAt { get; set; }
            public uint LastMasterArea { get; set; } = OBJECT_INVALID;
            public DateTime ExplicitOrderUntil { get; set; }
            public bool DefensiveReactionPending { get; set; }
            public int HostileAbilityFeatCount { get; set; } = -1;
            public int HostileAbilityFeatChecksum { get; set; }
            public List<(FeatType Feat, AbilityDetail Ability)> HostileAbilities { get; } = new();
        }

        private readonly Dictionary<uint, CompanionControlState> _states = new();

        private readonly IAIService _ai;
        private readonly IBeastMasteryService _beastMastery;

        public CompanionControlService(IAIService ai, IBeastMasteryService beastMastery)
        {
            _ai = ai;
            _beastMastery = beastMastery;
        }

        public bool IsRegisteredCompanion(uint creature)
        {
            return _states.ContainsKey(creature);
        }

        public bool IsControlledCompanion(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return false;

            var master = GetMaster(creature);
            return GetIsObjectValid(master) &&
                   GetIsPC(master) &&
                   (IsDroid(creature) || _beastMastery.IsPlayerBeast(creature));
        }

        public void Initialize(uint companion, bool preserveActionQueue = false)
        {
            if (!IsControlledCompanion(companion))
                return;

            var master = GetMaster(companion);
            _states[companion] = new CompanionControlState
            {
                LastMasterArea = GetArea(master)
            };

            SetAssociateListenPatterns(companion);
            IssueFollowAction(companion, !preserveActionQueue);
        }

        public void Clear(uint companion)
        {
            _states.Remove(companion);
            _ai.ClearAIState(companion);
        }

        public CompanionMode GetMode(uint companion)
        {
            return GetState(companion).Mode;
        }

        public bool AreAbilitiesEnabled(uint companion)
        {
            return !IsRegisteredCompanion(companion) || GetState(companion).AbilitiesEnabled;
        }

        public bool HandleConversation(uint companion)
        {
            if (!IsControlledCompanion(companion))
                return false;

            // A command left to the stock henchman AI can swap a ranged weapon for bare hands near an enemy,
            // so every command is read from its own event rather than the companion's latest command.
            if (!CompanionControlPolicy.TryGetAssociateCommand(GetListenPatternNumber(), out var command))
                return false;

            var master = GetMaster(companion);
            if (GetLastSpeaker() != master)
                return false;

            switch (command)
            {
                case AssociateCommand.FollowMaster:
                    SetMode(companion, CompanionMode.Follow, "Following you.");
                    return true;
                case AssociateCommand.GuardMaster:
                    SetMode(companion, CompanionMode.Guard, "Guarding you.");
                    return true;
                case AssociateCommand.StandGround:
                    SetMode(companion, CompanionMode.StandGround, "Holding this position.");
                    return true;
                case AssociateCommand.AttackNearest:
                    AttackNearest(companion);
                    return true;
                case AssociateCommand.HealMaster:
                    HealMaster(companion);
                    return true;
                case AssociateCommand.ToggleAbilities:
                    ToggleAbilities(companion);
                    return true;
                case AssociateCommand.MasterUnderAttack:
                    RegisterDefensiveThreat(companion, GetLastHostileActor(master));
                    return true;
                case AssociateCommand.MasterGoingtobeAttacked:
                    RegisterDefensiveThreat(companion, GetGoingToBeAttackedBy(master));
                    return true;
                case AssociateCommand.MasterAttackedOther:
                    RegisterOwnerAttack(companion, GetAttackTarget(master));
                    return true;
                case AssociateCommand.MasterFailedLockpick:
                case AssociateCommand.MasterSawTrap:
                case AssociateCommand.PickLock:
                case AssociateCommand.DisarmTrap:
                    BeginExplicitOrder(companion);
                    return false;
                default:
                    return false;
            }
        }

        public void RegisterDefensiveThreat(uint companion, uint threat)
        {
            if (!IsControlledCompanion(companion) || !IsValidHostileTarget(companion, threat))
                return;

            var state = GetState(companion);
            if (state.TrackedTarget == threat &&
                CompanionControlPolicy.HasPathingTimedOut(state.LastProgressAt, UtcNow()))
            {
                ResetProgress(state);
            }

            state.DefensiveThreats[threat] = UtcNow();
            state.ExplicitOrderUntil = default;

            if (!GetIsObjectValid(state.AttackNearestTarget))
            {
                state.DefensiveReactionPending = true;
                ProcessCombatRound(companion, true);
            }
        }

        public void RegisterOwnerAttack(uint companion, uint target)
        {
            if (!IsControlledCompanion(companion) ||
                GetIsObjectValid(GetState(companion).AttackNearestTarget) ||
                !IsValidHostileTarget(companion, target))
            {
                return;
            }

            var state = GetState(companion);
            state.OwnerAssistTarget = target;
            state.ExplicitOrderUntil = default;
            ProcessCombatRound(companion);
        }

        public void BeginExplicitOrder(uint companion, float durationSeconds = 30f)
        {
            if (!IsControlledCompanion(companion))
                return;

            InterruptAbilityActivation(companion);
            var state = GetState(companion);
            state.ExplicitOrderUntil = UtcNow().AddSeconds(Math.Max(1f, durationSeconds));
        }

        public void CancelExplicitOrder(uint companion)
        {
            if (_states.TryGetValue(companion, out var state))
                state.ExplicitOrderUntil = default;
        }

        public void ProcessCombatRound(uint companion, bool bypassDecisionThrottle = false)
        {
            if (!IsControlledCompanion(companion))
                return;

            ResumeFromRestIfMasterIsActive(companion, GetMaster(companion));
            if (IsBusy(companion))
                return;

            var state = GetState(companion);
            if (IsExplicitOrderInProgress(companion))
                return;

            if (state.DefensiveReactionPending)
            {
                state.DefensiveReactionPending = false;
                bypassDecisionThrottle = true;
            }

            var target = AdvanceAuthorizedTarget(companion);
            if (!GetIsObjectValid(target))
                MaintainModePosition(companion);

            var actionIssued = _ai.ProcessTrigger(
                companion,
                AITriggerType.CombatRound,
                target,
                bypassDecisionThrottle);
            if (actionIssued &&
                GetIsObjectValid(target) &&
                state.TrackedTarget != target)
            {
                StartTracking(state, companion, target);
            }
        }

        public void ProcessHeartbeat(uint companion)
        {
            if (!IsControlledCompanion(companion))
            {
                Clear(companion);
                return;
            }

            var state = GetState(companion);
            var master = GetMaster(companion);
            var masterArea = GetArea(master);
            if (state.LastMasterArea != OBJECT_INVALID && state.LastMasterArea != masterArea)
            {
                ResetToFollow(companion, false, true);
            }

            state.LastMasterArea = masterArea;
            ProcessCombatRound(companion);
        }

        private void ResumeFromRestIfMasterIsActive(uint companion, uint master)
        {
            if (GetBusyType(companion) != ActivityStatusType.Resting ||
                GetBusyType(master) == ActivityStatusType.Resting)
            {
                return;
            }

            if (HasRestStatusEffect(companion))
                RemoveRestStatusEffect(companion);
            else
                ClearBusy(companion);
        }

        public void ResumeModePosition(uint companion)
        {
            if (IsControlledCompanion(companion))
                MaintainModePosition(companion);
        }

        public bool TryProcessPendingDefensiveReaction(uint companion)
        {
            if (!_states.TryGetValue(companion, out var state) ||
                !state.DefensiveReactionPending)
            {
                return false;
            }

            ProcessCombatRound(companion, true);
            return true;
        }

        private uint AdvanceAuthorizedTarget(uint companion)
        {
            if (!IsControlledCompanion(companion))
                return OBJECT_INVALID;

            var state = GetState(companion);

            if (GetIsObjectValid(state.AttackNearestTarget))
            {
                if (ValidateAuthorizedTarget(
                        companion,
                        state.AttackNearestTarget,
                        CompanionEngagementType.AttackNearest) &&
                    TrackProgress(
                        companion,
                        state.AttackNearestTarget,
                        CompanionEngagementType.AttackNearest))
                {
                    return state.AttackNearestTarget;
                }

                CompleteAttackNearest(companion);
                return AdvanceAuthorizedTarget(companion);
            }

            var defensiveTarget = GetDefensiveTarget(companion, state);
            if (GetIsObjectValid(defensiveTarget))
                return defensiveTarget;

            if (GetIsObjectValid(state.OwnerAssistTarget))
            {
                if (ValidateAuthorizedTarget(
                        companion,
                        state.OwnerAssistTarget,
                        CompanionEngagementType.OwnerAssist) &&
                    TrackProgress(
                        companion,
                        state.OwnerAssistTarget,
                        CompanionEngagementType.OwnerAssist))
                {
                    return state.OwnerAssistTarget;
                }

                state.OwnerAssistTarget = OBJECT_INVALID;
                ResetProgress(state);
            }

            return OBJECT_INVALID;
        }

        public uint PeekAuthorizedTarget(uint companion)
        {
            if (!IsControlledCompanion(companion))
                return OBJECT_INVALID;

            var state = GetState(companion);
            if (GetIsObjectValid(state.AttackNearestTarget) &&
                ValidateAuthorizedTarget(
                    companion,
                    state.AttackNearestTarget,
                    CompanionEngagementType.AttackNearest))
            {
                return state.AttackNearestTarget;
            }

            var defensiveTarget = SelectDefensiveTarget(companion, state, false);
            if (GetIsObjectValid(defensiveTarget))
                return defensiveTarget;

            return GetIsObjectValid(state.OwnerAssistTarget) &&
                   ValidateAuthorizedTarget(
                       companion,
                       state.OwnerAssistTarget,
                       CompanionEngagementType.OwnerAssist)
                ? state.OwnerAssistTarget
                : OBJECT_INVALID;
        }

        public uint ResolveHostileAbilityTarget(
            uint companion,
            AbilityDetail ability,
            uint authorizedTarget,
            uint selectedTarget)
        {
            if (!GetIsObjectValid(authorizedTarget))
                return OBJECT_INVALID;

            if (ability.IsAreaAbility &&
                ability.Targeting != null &&
                ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf))
            {
                if (GetArea(companion) != GetArea(authorizedTarget))
                    return OBJECT_INVALID;

                var distance = GetDistanceBetween(companion, authorizedTarget);
                var sizeX = ability.Targeting.ResolveSizeX(companion, true);
                if (!CompanionControlPolicy.IsWithinSelfOriginAreaReach(
                        ability.Targeting.Shape,
                        true,
                        distance,
                        sizeX))
                {
                    return OBJECT_INVALID;
                }
            }

            if (ability.IsAreaAbility && selectedTarget == companion)
            {
                if (GetArea(companion) != GetArea(authorizedTarget))
                    return OBJECT_INVALID;

                var distance = GetDistanceBetween(companion, authorizedTarget);
                if (ability.Targeting == null)
                {
                    return CompanionControlPolicy.IsWithinAreaReach(distance, ability.MaxRange)
                        ? companion
                        : OBJECT_INVALID;
                }

                if (ability.Targeting.Shape is not AbilityTargetingShapeType.Sphere and
                    not AbilityTargetingShapeType.HSphere)
                {
                    return OBJECT_INVALID;
                }

                return ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf)
                    ? companion
                    : OBJECT_INVALID;
            }

            return selectedTarget == authorizedTarget
                ? selectedTarget
                : OBJECT_INVALID;
        }

        public bool IsHostileAbilityTargetAuthorized(
            uint companion,
            AbilityDetail ability,
            uint selectedTarget)
        {
            var authorizedTarget = PeekAuthorizedTarget(companion);
            return GetIsObjectValid(ResolveHostileAbilityTarget(
                companion,
                ability,
                authorizedTarget,
                selectedTarget));
        }

        public bool CanIssueAttackCommand(uint companion, uint target)
        {
            if (!IsRegisteredCompanion(companion))
                return true;

            var authorizedTarget = PeekAuthorizedTarget(companion);
            if (authorizedTarget != target)
                return false;

            var state = GetState(companion);
            return state.Mode != CompanionMode.StandGround || IsWithinWeaponRange(companion, target);
        }

        public void ResetOwnerCompanionToFollow(uint master)
        {
            if (!GetIsObjectValid(master))
                return;

            var companion = GetAssociate(AssociateType.Henchman, master);
            if (IsControlledCompanion(companion))
                ResetToFollow(companion, false, true);
        }
        public void RenameToggleAbilitiesCommand()
        {
            var player = GetEnteringObject();
            if (GetIsPC(player))
                PlayerPlugin.SetTlkOverride(player, AssociateAbilitiesLabelStrRef, "abilities");
        }
        public void OnOwnerAttacked()
        {
            RegisterOwnerThreat(OBJECT_SELF, GetLastAttacker(OBJECT_SELF));
        }
        public void OnOwnerDamaged()
        {
            RegisterOwnerThreat(OBJECT_SELF, GetLastDamager(OBJECT_SELF));
        }
        public void OnHostileSpellCastAtOwner()
        {
            if (GetLastSpellHarmful())
                RegisterOwnerThreat(OBJECT_SELF, GetLastSpellCaster());
        }
        public void OnOwnerCombatRound()
        {
            var master = OBJECT_SELF;
            var companion = GetAssociate(AssociateType.Henchman, master);
            RegisterOwnerAttack(companion, GetAttackTarget(master));
        }
        public void OnOwnerAreaExit()
        {
            var exitingObject = GetExitingObject();
            if (GetIsPC(exitingObject))
                ResetOwnerCompanionToFollow(exitingObject);
        }

        private CompanionControlState GetState(uint companion)
        {
            if (!_states.TryGetValue(companion, out var state))
            {
                state = new CompanionControlState
                {
                    LastMasterArea = GetArea(GetMaster(companion))
                };
                _states[companion] = state;
            }

            return state;
        }

        private void SetMode(uint companion, CompanionMode mode, string response)
        {
            InterruptAndClear(companion);
            var state = GetState(companion);
            state.Mode = mode;
            ClearEngagements(state);

            if (mode != CompanionMode.StandGround)
                IssueFollowAction(companion);

            Log.Write(LogGroup.AI, $"{GetName(companion)} entered companion mode {mode}.");
            SendResponse(companion, response);
        }

        private void AttackNearest(uint companion)
        {
            var target = FindNearestVisibleEnemy(companion);
            if (!GetIsObjectValid(target))
            {
                ResetToFollow(companion, false, true);
                SendResponse(companion, $"No visible enemy is within {CompanionControlPolicy.AttackNearestRangeMeters:0} meters.");
                return;
            }

            InterruptAndClear(companion);
            var state = GetState(companion);
            state.Mode = CompanionMode.Follow;
            ClearEngagements(state);
            state.AttackNearestTarget = target;
            var master = GetMaster(companion);
            SendResponse(companion, $"Attacking {GetDisplayName(master, target)}, then returning to Follow.");
            ProcessCombatRound(companion, true);
        }

        private void HealMaster(uint companion)
        {
            InterruptAndClear(companion);
            ResetToFollow(companion, false, false);

            var master = GetMaster(companion);
            if (GetCurrentHitPoints(master) >= GetMaxHitPoints(master))
            {
                SendResponse(companion, "You do not need healing.");
                IssueFollowAction(companion);
                return;
            }

            if (!AreAbilitiesEnabled(companion))
            {
                SendResponse(companion, "Unable to heal: abilities are disabled.");
                IssueFollowAction(companion);
                return;
            }

            if (_ai.TryUseBestTargetedSupportAbility(companion, master, out var abilityName))
            {
                SendResponse(companion, $"Using {abilityName} on you, then returning to Follow.");
                return;
            }

            SendResponse(companion, "Unable to heal you: no compatible healing ability is ready.");
            IssueFollowAction(companion);
        }

        private void ToggleAbilities(uint companion)
        {
            InterruptAndClear(companion);
            var state = GetState(companion);
            state.AbilitiesEnabled = !state.AbilitiesEnabled;
            if (!state.AbilitiesEnabled)
                DequeueWeaponAbility(companion);

            SendResponse(companion, state.AbilitiesEnabled
                ? "Abilities enabled."
                : "Abilities disabled; basic attacks only.");
            ProcessCombatRound(companion);
        }

        private void RegisterOwnerThreat(uint master, uint threat)
        {
            var companion = GetAssociate(AssociateType.Henchman, master);
            RegisterDefensiveThreat(companion, threat);
        }

        private uint GetDefensiveTarget(uint companion, CompanionControlState state)
        {
            var selected = SelectDefensiveTarget(companion, state, true);

            if (GetIsObjectValid(selected) &&
                !TrackProgress(companion, selected, CompanionEngagementType.Defensive))
            {
                RemoveDefensiveThreat(state, selected);
                MaintainModePosition(companion);
                return OBJECT_INVALID;
            }

            return selected;
        }

        private uint SelectDefensiveTarget(
            uint companion,
            CompanionControlState state,
            bool updateState)
        {
            var master = GetMaster(companion);
            var now = UtcNow();
            var candidates = new List<uint>();

            foreach (var (threat, _) in state.DefensiveThreats.ToList())
            {
                var attackTarget = GetIsObjectValid(threat)
                    ? GetAttackTarget(threat)
                    : OBJECT_INVALID;
                var activelyThreatening = attackTarget == master || attackTarget == companion;
                if (updateState && activelyThreatening)
                    state.DefensiveThreats[threat] = now;

                var recentlyThreatened = (now - state.DefensiveThreats[threat]).TotalSeconds <
                                         CompanionControlPolicy.PathingTimeoutSeconds;
                var isAuthorized = (activelyThreatening || recentlyThreatened) &&
                                   ValidateAuthorizedTarget(
                                       companion,
                                       threat,
                                       CompanionEngagementType.Defensive);
                if (!isAuthorized)
                {
                    if (updateState)
                        RemoveDefensiveThreat(state, threat);

                    continue;
                }

                candidates.Add(threat);
            }

            return candidates
                .OrderByDescending(x => GetAttackTarget(x) == master)
                .ThenBy(x => GetDistanceBetween(companion, x))
                .FirstOrDefault(OBJECT_INVALID);
        }

        private void RemoveDefensiveThreat(CompanionControlState state, uint threat)
        {
            state.DefensiveThreats.Remove(threat);
            if (state.TrackedTarget == threat)
                ResetProgress(state);
        }

        private bool ValidateAuthorizedTarget(
            uint companion,
            uint target,
            CompanionEngagementType engagementType)
        {
            if (!IsValidHostileTarget(companion, target))
                return false;

            var master = GetMaster(companion);
            var state = GetState(companion);
            if (state.Mode == CompanionMode.StandGround &&
                engagementType != CompanionEngagementType.AttackNearest)
            {
                return CanEngageWithoutMoving(companion, target);
            }

            var tether = CompanionControlPolicy.GetTetherMeters(state.Mode, engagementType);
            if (GetArea(master) != GetArea(target) || GetDistanceBetween(master, target) > tether)
                return false;

            return true;
        }

        private bool TrackProgress(
            uint companion,
            uint target,
            CompanionEngagementType engagementType)
        {
            var state = GetState(companion);
            if (state.TrackedTarget != target)
                return true;

            var distance = GetDistanceBetween(companion, target);
            var offensiveActivityAt = GetLastOffensiveActivityAt(companion);
            var hasNewOffensiveActivity = offensiveActivityAt > state.LastOffensiveActivityAt;

            if (CompanionControlPolicy.HasCombatProgress(
                    hasNewOffensiveActivity,
                    state.LastDistanceToTarget,
                    distance))
            {
                state.LastProgressAt = UtcNow();
                state.LastDistanceToTarget = distance;
                state.LastOffensiveActivityAt = offensiveActivityAt;
                return true;
            }

            if (!CompanionControlPolicy.HasPathingTimedOut(state.LastProgressAt, UtcNow()))
                return true;

            var returnsToFollow = CompanionControlPolicy.ReturnsToFollowWhenComplete(engagementType);
            Log.Write(
                LogGroup.AI,
                returnsToFollow
                    ? $"{GetName(companion)} timed out pathing to {GetName(target)} and returned to Follow."
                    : $"{GetName(companion)} timed out pathing to {GetName(target)} and retained {state.Mode} mode.");
            SendResponse(
                companion,
                returnsToFollow
                    ? "Unable to reach the target; returning to Follow."
                    : $"Unable to reach the target; remaining in {state.Mode}.");
            return false;
        }

        private void CompleteAttackNearest(uint companion)
        {
            var state = GetState(companion);
            state.Mode = CompanionMode.Follow;
            state.AttackNearestTarget = OBJECT_INVALID;
            state.OwnerAssistTarget = OBJECT_INVALID;
            ResetProgress(state);
            Log.Write(LogGroup.AI, $"{GetName(companion)} completed Attack Nearest and returned to Follow.");
            MaintainModePosition(companion);
        }

        private void StartTracking(CompanionControlState state, uint companion, uint target)
        {
            state.TrackedTarget = target;
            state.LastDistanceToTarget = GetDistanceBetween(companion, target);
            state.LastProgressAt = UtcNow();
            state.LastOffensiveActivityAt = GetLastOffensiveActivityAt(companion);
        }

        private bool CanEngageWithoutMoving(uint companion, uint target)
        {
            return IsWithinWeaponRange(companion, target) ||
                   CanUseHostileAbilityWithoutMoving(companion, target);
        }

        private bool CanUseHostileAbilityWithoutMoving(uint companion, uint target)
        {
            if (!AreAbilitiesEnabled(companion))
                return false;

            foreach (var (feat, ability) in GetCachedHostileAbilities(companion))
            {
                if (ability.ActivationType == AbilityActivationType.Weapon)
                    continue;

                if (ability.IsAreaAbility && ability.Targeting == null)
                    continue;

                var isSelfCenteredArea = ability.IsAreaAbility &&
                                         ability.Targeting?.Shape == AbilityTargetingShapeType.Sphere &&
                                         ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf);
                var isSelfOriginArea = ability.IsAreaAbility &&
                                       ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf);
                var targetDistance = GetDistanceBetween(companion, target);
                if (ability.IsAreaAbility &&
                    !isSelfOriginArea &&
                    !CompanionControlPolicy.IsWithinDeclaredPlacementRange(
                        ability.HasExplicitMaxRange,
                        targetDistance,
                        ability.MaxRange))
                {
                    continue;
                }

                if (isSelfOriginArea &&
                    !CompanionControlPolicy.IsWithinSelfOriginAreaReach(
                        ability.Targeting.Shape,
                        true,
                        targetDistance,
                        ability.Targeting.ResolveSizeX(companion, true)))
                {
                    continue;
                }

                var abilityTarget = ResolveHostileAbilityTarget(
                    companion,
                    ability,
                    target,
                    isSelfCenteredArea ? companion : target);
                var targetLocation = GetLocation(
                    isSelfCenteredArea
                        ? companion
                        : target);
                var effectiveLevel = ability.EffectiveLevelPerkType == PerkType.Invalid
                    ? 1
                    : GetPerkLevel(companion, ability.EffectiveLevelPerkType);

                if (CanUseAbility(companion, abilityTarget, feat, effectiveLevel, targetLocation))
                    return true;
            }

            return false;
        }

        private IReadOnlyList<(FeatType Feat, AbilityDetail Ability)> GetCachedHostileAbilities(uint companion)
        {
            var state = GetState(companion);
            var featCount = CreaturePlugin.GetFeatCount(companion);
            var featChecksum = 17;
            var knownFeats = new List<FeatType>(featCount);

            unchecked
            {
                for (var index = 0; index < featCount; index++)
                {
                    var feat = CreaturePlugin.GetFeatByIndex(companion, index);
                    knownFeats.Add(feat);
                    featChecksum = featChecksum * 31 + (int)feat;
                }
            }

            if (state.HostileAbilityFeatCount == featCount &&
                state.HostileAbilityFeatChecksum == featChecksum)
            {
                return state.HostileAbilities;
            }

            state.HostileAbilityFeatCount = featCount;
            state.HostileAbilityFeatChecksum = featChecksum;
            state.HostileAbilities.Clear();

            foreach (var feat in knownFeats)
            {
                if (!IsFeatRegistered(feat))
                    continue;

                var ability = GetAbilityDetail(feat);
                if (ability.IsHostileAbility)
                    state.HostileAbilities.Add((feat, ability));
            }

            return state.HostileAbilities;
        }

        private bool IsWithinWeaponRange(uint companion, uint target)
        {
            var skill = GetEquippedWeaponSkillType(companion);
            return GetDistanceBetween(companion, target) <=
                   GetWeaponEngagementRange(skill) + AttackRangeToleranceMeters;
        }

        private bool IsValidHostileTarget(uint companion, uint target)
        {
            return GetIsObjectValid(target) &&
                   target != companion &&
                   GetCurrentHitPoints(target) > 0 &&
                   GetArea(companion) == GetArea(target) &&
                   GetIsReactionTypeHostile(target, companion);
        }

        private uint FindNearestVisibleEnemy(uint companion)
        {
            var master = GetMaster(companion);
            for (var nth = 1; nth <= MaximumNearestCreatureChecks; nth++)
            {
                var target = GetNearestCreature(
                    CreatureType.Reputation,
                    (int)ReputationType.Enemy,
                    companion,
                    nth,
                    (int)CreatureType.IsAlive,
                    1);
                if (!GetIsObjectValid(target))
                    break;

                if (GetDistanceBetween(companion, target) > CompanionControlPolicy.AttackNearestRangeMeters)
                    break;

                if (GetDistanceBetween(master, target) <= CompanionControlPolicy.AttackNearestRangeMeters &&
                    IsValidHostileTarget(companion, target) &&
                    GetObjectSeen(target, companion))
                {
                    return target;
                }
            }

            return OBJECT_INVALID;
        }

        private void ResetToFollow(uint companion, bool sendResponse, bool interrupt)
        {
            if (interrupt)
                InterruptAndClear(companion);

            var state = GetState(companion);
            state.Mode = CompanionMode.Follow;
            ClearEngagements(state);

            if (sendResponse)
                SendResponse(companion, "Returning to Follow.");

            MaintainModePosition(companion);
        }

        private void ClearEngagements(CompanionControlState state)
        {
            state.AttackNearestTarget = OBJECT_INVALID;
            state.OwnerAssistTarget = OBJECT_INVALID;
            state.DefensiveThreats.Clear();
            state.DefensiveReactionPending = false;
            ResetProgress(state);
        }

        private void ResetProgress(CompanionControlState state)
        {
            state.TrackedTarget = OBJECT_INVALID;
            state.LastDistanceToTarget = float.MaxValue;
            state.LastProgressAt = default;
            state.LastOffensiveActivityAt = default;
        }

        private void InterruptAndClear(uint companion)
        {
            CancelExplicitOrder(companion);
            InterruptAbilityActivation(companion);
            AssignCommand(companion, () => ClearAllActions(true));
        }

        private bool IsExplicitOrderInProgress(uint companion)
        {
            var state = GetState(companion);
            var currentAction = GetCurrentAction(companion);
            if (!CompanionControlPolicy.ShouldPreserveExplicitOrder(
                    state.ExplicitOrderUntil,
                    UtcNow(),
                    currentAction))
            {
                state.ExplicitOrderUntil = default;
                return false;
            }

            return true;
        }

        private void MaintainModePosition(uint companion)
        {
            if (IsBusy(companion) || IsExplicitOrderInProgress(companion))
                return;

            var state = GetState(companion);
            if (state.Mode == CompanionMode.StandGround)
            {
                var currentAction = GetCurrentAction(companion);
                if (CompanionControlPolicy.ShouldStopActionInStandGround(currentAction))
                    AssignCommand(companion, () => ClearAllActions(true));

                return;
            }

            if (GetCurrentAction(companion) != ActionType.Follow)
                IssueFollowAction(companion);
        }

        private void IssueFollowAction(uint companion, bool clearActions = true)
        {
            var master = GetMaster(companion);
            if (!GetIsObjectValid(master))
                return;

            var followDistance = GetState(companion).Mode == CompanionMode.Guard
                ? GuardFollowDistanceMeters
                : FollowDistanceMeters;
            AssignCommand(companion, () =>
            {
                if (clearActions)
                    ClearAllActions(true);

                ActionForceFollowObject(master, followDistance);
            });
        }

        private void SendResponse(uint companion, string response)
        {
            var master = GetMaster(companion);
            if (GetIsObjectValid(master))
                SendMessageToPC(master, $"{GetName(companion)}: {response}");
        }
    

        // Each protected virtual method below has the original call as its body, so the server behaves as it did.
        // They name the static services that are not converted yet (and the clock), so a test can answer them.

        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual bool IsDroid(uint creature) => Droid.IsDroid(creature);

        protected virtual bool IsBusy(uint target) => Activity.IsBusy(target);

        protected virtual ActivityStatusType GetBusyType(uint target) => Activity.GetBusyType(target);

        protected virtual void ClearBusy(uint target) => Activity.ClearBusy(target);

        protected virtual bool HasRestStatusEffect(uint creature) =>
            StatusEffect.HasStatusEffect(creature, typeof(RestStatusEffect));

        protected virtual void RemoveRestStatusEffect(uint creature) =>
            StatusEffect.RemoveStatusEffect(creature, typeof(RestStatusEffect), false);

        protected virtual bool InterruptAbilityActivation(uint activator) =>
            UsePerkFeat.InterruptAbilityActivation(activator);

        protected virtual void DequeueWeaponAbility(uint creature) =>
            UsePerkFeat.DequeueWeaponAbility(creature, false);

        protected virtual string GetDisplayName(uint observer, uint target) =>
            PlayerName.GetDisplayName(observer, target);

        protected virtual DateTime GetLastOffensiveActivityAt(uint creature) =>
            Combat.GetLastOffensiveActivityAt(creature);

        protected virtual SkillType GetEquippedWeaponSkillType(uint creature) =>
            Combat.GetEquippedWeaponSkillType(creature);

        protected virtual float GetWeaponEngagementRange(SkillType skill) =>
            Combat.GetWeaponEngagementRange(skill);

        protected virtual int GetPerkLevel(uint creature, PerkType perkType) =>
            Perk.GetPerkLevel(creature, perkType);

        protected virtual bool IsFeatRegistered(FeatType feat) => Ability.IsFeatRegistered(feat);

        protected virtual AbilityDetail GetAbilityDetail(FeatType feat) => Ability.GetAbilityDetail(feat);

        protected virtual bool CanUseAbility(
            uint activator,
            uint target,
            FeatType feat,
            int effectiveLevel,
            Location targetLocation) =>
            Ability.CanUseAbility(activator, target, feat, effectiveLevel, targetLocation);
    }
}
