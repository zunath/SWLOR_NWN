using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using AssociateCommand = SWLOR.NWN.API.NWScript.Enum.Associate.Command;

namespace SWLOR.Game.Server.Service.CompanionControlService
{
    public static class CompanionControl
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
            public int HostileAbilityFeatCount { get; set; } = -1;
            public int HostileAbilityFeatChecksum { get; set; }
            public List<(FeatType Feat, AbilityDetail Ability)> HostileAbilities { get; } = new();
        }

        private static readonly Dictionary<uint, CompanionControlState> _states = new();

        public static bool IsRegisteredCompanion(uint creature)
        {
            return _states.ContainsKey(creature);
        }

        public static bool IsControlledCompanion(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return false;

            var master = GetMaster(creature);
            return GetIsObjectValid(master) &&
                   GetIsPC(master) &&
                   (Droid.IsDroid(creature) || BeastMastery.IsPlayerBeast(creature));
        }

        public static void Initialize(uint companion, bool preserveActionQueue = false)
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

        public static void Clear(uint companion)
        {
            _states.Remove(companion);
            NPCAI.ClearState(companion);
        }

        public static CompanionMode GetMode(uint companion)
        {
            return GetState(companion).Mode;
        }

        public static bool AreAbilitiesEnabled(uint companion)
        {
            return !IsRegisteredCompanion(companion) || GetState(companion).AbilitiesEnabled;
        }

        public static bool HandleConversation(uint companion)
        {
            if (!IsControlledCompanion(companion))
                return false;

            var commandValue = GetLastAssociateCommand(companion);
            if (GetListenPatternNumber() != commandValue)
                return false;

            var master = GetMaster(companion);
            if (GetLastSpeaker() != master)
                return false;

            var command = (AssociateCommand)commandValue;
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

        public static void RegisterDefensiveThreat(uint companion, uint threat)
        {
            if (!IsControlledCompanion(companion) || !IsValidHostileTarget(companion, threat))
                return;

            var state = GetState(companion);
            state.DefensiveThreats[threat] = DateTime.UtcNow;
            state.ExplicitOrderUntil = default;

            if (!GetIsObjectValid(state.AttackNearestTarget))
                ProcessCombatRound(companion);
        }

        public static void RegisterOwnerAttack(uint companion, uint target)
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

        public static void BeginExplicitOrder(uint companion, float durationSeconds = 30f)
        {
            if (!IsControlledCompanion(companion))
                return;

            UsePerkFeat.InterruptAbilityActivation(companion);
            var state = GetState(companion);
            state.ExplicitOrderUntil = DateTime.UtcNow.AddSeconds(Math.Max(1f, durationSeconds));
        }

        public static void CancelExplicitOrder(uint companion)
        {
            if (_states.TryGetValue(companion, out var state))
                state.ExplicitOrderUntil = default;
        }

        public static void ProcessCombatRound(uint companion, bool bypassDecisionThrottle = false)
        {
            if (!IsControlledCompanion(companion) || Activity.IsBusy(companion))
                return;

            if (IsExplicitOrderInProgress(companion))
                return;

            var target = AdvanceAuthorizedTarget(companion);
            if (!GetIsObjectValid(target))
                MaintainModePosition(companion);

            var actionIssued = AI.ProcessTrigger(
                companion,
                AITriggerType.CombatRound,
                target,
                bypassDecisionThrottle);
            var state = GetState(companion);
            if (actionIssued &&
                GetIsObjectValid(target) &&
                state.TrackedTarget != target)
            {
                StartTracking(state, companion, target);
            }
        }

        public static void ProcessHeartbeat(uint companion)
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

        public static void ResumeModePosition(uint companion)
        {
            if (IsControlledCompanion(companion))
                MaintainModePosition(companion);
        }

        private static uint AdvanceAuthorizedTarget(uint companion)
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

        public static uint PeekAuthorizedTarget(uint companion)
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

            var master = GetMaster(companion);
            var now = DateTime.UtcNow;
            var defensiveTarget = state.DefensiveThreats
                .Where(x =>
                {
                    var threat = x.Key;
                    var attackTarget = GetIsObjectValid(threat)
                        ? GetAttackTarget(threat)
                        : OBJECT_INVALID;
                    var activelyThreatening = attackTarget == master || attackTarget == companion;
                    var recentlyThreatened = (now - x.Value).TotalSeconds <
                                             CompanionControlPolicy.PathingTimeoutSeconds;
                    return (activelyThreatening || recentlyThreatened) &&
                           ValidateAuthorizedTarget(
                               companion,
                               threat,
                               CompanionEngagementType.Defensive);
                })
                .Select(x => x.Key)
                .OrderByDescending(x => GetAttackTarget(x) == master)
                .ThenBy(x => GetDistanceBetween(companion, x))
                .FirstOrDefault(OBJECT_INVALID);
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

        public static uint ResolveHostileAbilityTarget(
            uint companion,
            AbilityDetail ability,
            uint authorizedTarget,
            uint selectedTarget)
        {
            if (!GetIsObjectValid(authorizedTarget))
                return OBJECT_INVALID;

            if (ability.IsAreaAbility && selectedTarget == companion)
                return companion;

            return selectedTarget == authorizedTarget
                ? selectedTarget
                : OBJECT_INVALID;
        }

        public static bool CanIssueAttackCommand(uint companion, uint target)
        {
            if (!IsRegisteredCompanion(companion))
                return true;

            var authorizedTarget = PeekAuthorizedTarget(companion);
            if (authorizedTarget != target)
                return false;

            var state = GetState(companion);
            return state.Mode != CompanionMode.StandGround || IsWithinWeaponRange(companion, target);
        }

        public static void ResetOwnerCompanionToFollow(uint master)
        {
            if (!GetIsObjectValid(master))
                return;

            var companion = GetAssociate(AssociateType.Henchman, master);
            if (IsControlledCompanion(companion))
                ResetToFollow(companion, false, true);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RenameToggleAbilitiesCommand()
        {
            var player = GetEnteringObject();
            if (GetIsPC(player))
                PlayerPlugin.SetTlkOverride(player, AssociateAbilitiesLabelStrRef, "abilities");
        }

        [NWNEventHandler(ScriptName.OnPlayerAttacked)]
        public static void OnOwnerAttacked()
        {
            RegisterOwnerThreat(OBJECT_SELF, GetLastAttacker(OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void OnOwnerDamaged()
        {
            RegisterOwnerThreat(OBJECT_SELF, GetLastDamager(OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnPlayerSpellCastAt)]
        public static void OnHostileSpellCastAtOwner()
        {
            if (GetLastSpellHarmful())
                RegisterOwnerThreat(OBJECT_SELF, GetLastSpellCaster());
        }

        [NWNEventHandler(ScriptName.OnPlayerRoundEnd)]
        public static void OnOwnerCombatRound()
        {
            var master = OBJECT_SELF;
            var companion = GetAssociate(AssociateType.Henchman, master);
            RegisterOwnerAttack(companion, GetAttackTarget(master));
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnOwnerAreaExit()
        {
            var exitingObject = GetExitingObject();
            if (GetIsPC(exitingObject))
                ResetOwnerCompanionToFollow(exitingObject);
        }

        private static CompanionControlState GetState(uint companion)
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

        private static void SetMode(uint companion, CompanionMode mode, string response)
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

        private static void AttackNearest(uint companion)
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
            SendResponse(companion, $"Attacking {PlayerName.GetDisplayName(master, target)}, then returning to Follow.");
            ProcessCombatRound(companion, true);
        }

        private static void HealMaster(uint companion)
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

            if (NPCAI.TryUseBestTargetedSupportAbility(companion, master, out var abilityName))
            {
                SendResponse(companion, $"Using {abilityName} on you, then returning to Follow.");
                return;
            }

            SendResponse(companion, "Unable to heal you: no compatible healing ability is ready.");
            IssueFollowAction(companion);
        }

        private static void ToggleAbilities(uint companion)
        {
            InterruptAndClear(companion);
            var state = GetState(companion);
            state.AbilitiesEnabled = !state.AbilitiesEnabled;
            if (!state.AbilitiesEnabled)
                UsePerkFeat.DequeueWeaponAbility(companion, false);

            SendResponse(companion, state.AbilitiesEnabled
                ? "Abilities enabled."
                : "Abilities disabled; basic attacks only.");
            ProcessCombatRound(companion);
        }

        private static void RegisterOwnerThreat(uint master, uint threat)
        {
            var companion = GetAssociate(AssociateType.Henchman, master);
            RegisterDefensiveThreat(companion, threat);
        }

        private static uint GetDefensiveTarget(uint companion, CompanionControlState state)
        {
            var master = GetMaster(companion);
            var now = DateTime.UtcNow;
            var candidates = new List<uint>();

            foreach (var (threat, _) in state.DefensiveThreats.ToList())
            {
                var attackTarget = GetIsObjectValid(threat)
                    ? GetAttackTarget(threat)
                    : OBJECT_INVALID;
                if (attackTarget == master || attackTarget == companion)
                    state.DefensiveThreats[threat] = now;

                var recentlyThreatened = (now - state.DefensiveThreats[threat]).TotalSeconds <
                                         CompanionControlPolicy.PathingTimeoutSeconds;
                if (!recentlyThreatened ||
                    !ValidateAuthorizedTarget(companion, threat, CompanionEngagementType.Defensive))
                {
                    state.DefensiveThreats.Remove(threat);
                    continue;
                }

                candidates.Add(threat);
            }

            var selected = candidates
                .OrderByDescending(x => GetAttackTarget(x) == master)
                .ThenBy(x => GetDistanceBetween(companion, x))
                .FirstOrDefault(OBJECT_INVALID);

            if (GetIsObjectValid(selected) &&
                !TrackProgress(companion, selected, CompanionEngagementType.Defensive))
            {
                state.DefensiveThreats.Remove(selected);
                ResetProgress(state);
                MaintainModePosition(companion);
                return OBJECT_INVALID;
            }

            return selected;
        }

        private static bool ValidateAuthorizedTarget(
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

        private static bool TrackProgress(
            uint companion,
            uint target,
            CompanionEngagementType engagementType)
        {
            var state = GetState(companion);
            if (state.TrackedTarget != target)
                return true;

            var distance = GetDistanceBetween(companion, target);
            var offensiveActivityAt = Combat.GetLastOffensiveActivityAt(companion);
            var hasNewOffensiveActivity = offensiveActivityAt > state.LastOffensiveActivityAt;

            if (CompanionControlPolicy.HasCombatProgress(
                    hasNewOffensiveActivity,
                    state.LastDistanceToTarget,
                    distance))
            {
                state.LastProgressAt = DateTime.UtcNow;
                state.LastDistanceToTarget = distance;
                state.LastOffensiveActivityAt = offensiveActivityAt;
                return true;
            }

            if (!CompanionControlPolicy.HasPathingTimedOut(state.LastProgressAt, DateTime.UtcNow))
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

        private static void CompleteAttackNearest(uint companion)
        {
            var state = GetState(companion);
            state.Mode = CompanionMode.Follow;
            state.AttackNearestTarget = OBJECT_INVALID;
            state.OwnerAssistTarget = OBJECT_INVALID;
            ResetProgress(state);
            Log.Write(LogGroup.AI, $"{GetName(companion)} completed Attack Nearest and returned to Follow.");
            MaintainModePosition(companion);
        }

        private static void StartTracking(CompanionControlState state, uint companion, uint target)
        {
            state.TrackedTarget = target;
            state.LastDistanceToTarget = GetDistanceBetween(companion, target);
            state.LastProgressAt = DateTime.UtcNow;
            state.LastOffensiveActivityAt = Combat.GetLastOffensiveActivityAt(companion);
        }

        private static bool CanEngageWithoutMoving(uint companion, uint target)
        {
            return IsWithinWeaponRange(companion, target) ||
                   CanUseHostileAbilityWithoutMoving(companion, target);
        }

        private static bool CanUseHostileAbilityWithoutMoving(uint companion, uint target)
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
                if (isSelfCenteredArea &&
                    GetDistanceBetween(companion, target) > ability.Targeting.ResolveSizeX(companion, true))
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
                    : Perk.GetPerkLevel(companion, ability.EffectiveLevelPerkType);

                if (Ability.CanUseAbility(companion, abilityTarget, feat, effectiveLevel, targetLocation))
                    return true;
            }

            return false;
        }

        private static IReadOnlyList<(FeatType Feat, AbilityDetail Ability)> GetCachedHostileAbilities(uint companion)
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
                if (!Ability.IsFeatRegistered(feat))
                    continue;

                var ability = Ability.GetAbilityDetail(feat);
                if (ability.IsHostileAbility)
                    state.HostileAbilities.Add((feat, ability));
            }

            return state.HostileAbilities;
        }

        private static bool IsWithinWeaponRange(uint companion, uint target)
        {
            var skill = Combat.GetEquippedWeaponSkillType(companion);
            return GetDistanceBetween(companion, target) <=
                   Combat.GetWeaponEngagementRange(skill) + AttackRangeToleranceMeters;
        }

        private static bool IsValidHostileTarget(uint companion, uint target)
        {
            return GetIsObjectValid(target) &&
                   target != companion &&
                   GetCurrentHitPoints(target) > 0 &&
                   GetArea(companion) == GetArea(target) &&
                   GetIsReactionTypeHostile(target, companion);
        }

        private static uint FindNearestVisibleEnemy(uint companion)
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

        private static void ResetToFollow(uint companion, bool sendResponse, bool interrupt)
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

        private static void ClearEngagements(CompanionControlState state)
        {
            state.AttackNearestTarget = OBJECT_INVALID;
            state.OwnerAssistTarget = OBJECT_INVALID;
            state.DefensiveThreats.Clear();
            ResetProgress(state);
        }

        private static void ResetProgress(CompanionControlState state)
        {
            state.TrackedTarget = OBJECT_INVALID;
            state.LastDistanceToTarget = float.MaxValue;
            state.LastProgressAt = default;
            state.LastOffensiveActivityAt = default;
        }

        private static void InterruptAndClear(uint companion)
        {
            CancelExplicitOrder(companion);
            UsePerkFeat.InterruptAbilityActivation(companion);
            AssignCommand(companion, () => ClearAllActions(true));
        }

        private static bool IsExplicitOrderInProgress(uint companion)
        {
            var state = GetState(companion);
            var currentAction = GetCurrentAction(companion);
            if (!CompanionControlPolicy.ShouldPreserveExplicitOrder(
                    state.ExplicitOrderUntil,
                    DateTime.UtcNow,
                    currentAction))
            {
                state.ExplicitOrderUntil = default;
                return false;
            }

            return true;
        }

        private static void MaintainModePosition(uint companion)
        {
            if (Activity.IsBusy(companion) || IsExplicitOrderInProgress(companion))
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

        private static void IssueFollowAction(uint companion, bool clearActions = true)
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

        private static void SendResponse(uint companion, string response)
        {
            var master = GetMaster(companion);
            if (GetIsObjectValid(master))
                SendMessageToPC(master, $"{GetName(companion)}: {response}");
        }
    }
}
