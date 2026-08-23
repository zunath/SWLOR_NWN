using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
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
        private const float ProgressDistanceMeters = 0.25f;
        private const float AttackRangeToleranceMeters = 0.25f;
        private const int MaximumNearestCreatureChecks = 64;
        // The native associate radial combines its Toggle label with base TLK 8127 ("casting").
        private const int AssociateCastingLabelStrRef = 8127;

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
            public uint LastMasterArea { get; set; } = OBJECT_INVALID;
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

        public static void Initialize(uint companion)
        {
            if (!IsControlledCompanion(companion))
                return;

            var master = GetMaster(companion);
            _states[companion] = new CompanionControlState
            {
                LastMasterArea = GetArea(master)
            };

            SetAssociateListenPatterns(companion);
            IssueFollowAction(companion);
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

            GetState(companion).OwnerAssistTarget = target;
            ProcessCombatRound(companion);
        }

        public static void ProcessCombatRound(uint companion)
        {
            if (!IsControlledCompanion(companion) || Activity.IsBusy(companion))
                return;

            var target = GetAuthorizedTarget(companion);
            if (!GetIsObjectValid(target))
            {
                MaintainModePosition(companion);
                return;
            }

            AI.ProcessTrigger(companion, AITriggerType.CombatRound, target);
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

        public static uint GetAuthorizedTarget(uint companion)
        {
            if (!IsControlledCompanion(companion))
                return OBJECT_INVALID;

            var state = GetState(companion);

            if (GetIsObjectValid(state.AttackNearestTarget))
            {
                if (ValidateAuthorizedTarget(companion, state.AttackNearestTarget, CompanionEngagementType.AttackNearest))
                    return state.AttackNearestTarget;

                CompleteAttackNearest(companion);
                return GetAuthorizedTarget(companion);
            }

            var defensiveTarget = GetDefensiveTarget(companion, state);
            if (GetIsObjectValid(defensiveTarget))
                return defensiveTarget;

            if (GetIsObjectValid(state.OwnerAssistTarget))
            {
                if (ValidateAuthorizedTarget(companion, state.OwnerAssistTarget, CompanionEngagementType.OwnerAssist))
                    return state.OwnerAssistTarget;

                state.OwnerAssistTarget = OBJECT_INVALID;
                ResetProgress(state);
            }

            return OBJECT_INVALID;
        }

        public static uint ResolveHostileAbilityTarget(uint companion, AbilityDetail ability)
        {
            var authorizedTarget = GetAuthorizedTarget(companion);
            if (!GetIsObjectValid(authorizedTarget))
                return OBJECT_INVALID;

            if (ability.IsAreaAbility &&
                ability.Targeting?.Shape == AbilityTargetingShapeType.Sphere &&
                ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf))
            {
                return companion;
            }

            return authorizedTarget;
        }

        public static bool CanIssueAttackCommand(uint companion, uint target)
        {
            if (!IsRegisteredCompanion(companion))
                return true;

            var authorizedTarget = GetAuthorizedTarget(companion);
            if (authorizedTarget != target)
                return false;

            var state = GetState(companion);
            return state.Mode != CompanionMode.StandGround || IsWithinWeaponRange(companion, target);
        }

        public static bool TryIssueAuthorizedAttack(uint companion)
        {
            var target = GetAuthorizedTarget(companion);
            if (!GetIsObjectValid(target) || !CanIssueAttackCommand(companion, target))
                return false;

            Enmity.IssueAttackCommand(companion, target);
            return true;
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
                PlayerPlugin.SetTlkOverride(player, AssociateCastingLabelStrRef, "abilities");
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
            StartTracking(state, companion, target);
            var master = GetMaster(companion);
            SendResponse(companion, $"Attacking {PlayerName.GetDisplayName(master, target)}, then returning to Follow.");
            ProcessCombatRound(companion);
        }

        private static void HealMaster(uint companion)
        {
            InterruptAndClear(companion);
            ResetToFollow(companion, false, false);

            if (!AreAbilitiesEnabled(companion))
            {
                SendResponse(companion, "Unable to heal: abilities are disabled.");
                IssueFollowAction(companion);
                return;
            }

            var master = GetMaster(companion);
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
                    !ValidateAuthorizedTarget(companion, threat, CompanionEngagementType.Defensive, false))
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

            if (GetIsObjectValid(selected) && !TrackProgress(companion, selected))
            {
                state.DefensiveThreats.Remove(selected);
                ReturnToFollowPreservingThreats(companion);
                return OBJECT_INVALID;
            }

            return selected;
        }

        private static bool ValidateAuthorizedTarget(
            uint companion,
            uint target,
            CompanionEngagementType engagementType,
            bool trackProgress = true)
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

            return !trackProgress || TrackProgress(companion, target);
        }

        private static bool TrackProgress(uint companion, uint target)
        {
            var state = GetState(companion);
            if (state.TrackedTarget != target)
            {
                StartTracking(state, companion, target);
                return true;
            }

            var distance = GetDistanceBetween(companion, target);
            var hasOpportunity = IsWithinWeaponRange(companion, target) ||
                                 CanUseHostileAbilityWithoutMoving(companion, target);

            if (hasOpportunity || distance + ProgressDistanceMeters < state.LastDistanceToTarget)
            {
                state.LastProgressAt = DateTime.UtcNow;
                state.LastDistanceToTarget = distance;
                return true;
            }

            if (!CompanionControlPolicy.HasPathingTimedOut(state.LastProgressAt, DateTime.UtcNow))
                return true;

            SendResponse(companion, "Unable to reach the target; returning to Follow.");
            ReturnToFollowPreservingThreats(companion);
            return false;
        }

        private static void CompleteAttackNearest(uint companion)
        {
            var state = GetState(companion);
            state.Mode = CompanionMode.Follow;
            state.AttackNearestTarget = OBJECT_INVALID;
            state.OwnerAssistTarget = OBJECT_INVALID;
            ResetProgress(state);
            MaintainModePosition(companion);
        }

        private static void ReturnToFollowPreservingThreats(uint companion)
        {
            var state = GetState(companion);
            state.Mode = CompanionMode.Follow;
            state.AttackNearestTarget = OBJECT_INVALID;
            state.OwnerAssistTarget = OBJECT_INVALID;
            ResetProgress(state);
        }

        private static void StartTracking(CompanionControlState state, uint companion, uint target)
        {
            state.TrackedTarget = target;
            state.LastDistanceToTarget = GetDistanceBetween(companion, target);
            state.LastProgressAt = DateTime.UtcNow;
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

            var distance = GetDistanceBetween(companion, target);
            foreach (var (feat, ability) in Ability.GetAllAbilityDetails())
            {
                if (!ability.IsHostileAbility ||
                    !GetHasFeat(feat, companion) ||
                    distance > ability.MaxRange)
                {
                    continue;
                }

                if (!ability.RequiresTarget || GetObjectSeen(target, companion))
                    return true;
            }

            return false;
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
        }

        private static void InterruptAndClear(uint companion)
        {
            UsePerkFeat.InterruptAbilityActivation(companion);
            AssignCommand(companion, () => ClearAllActions(true));
        }

        private static void MaintainModePosition(uint companion)
        {
            if (Activity.IsBusy(companion))
                return;

            var state = GetState(companion);
            if (state.Mode == CompanionMode.StandGround)
                return;

            if (GetCurrentAction(companion) != ActionType.Follow)
                IssueFollowAction(companion);
        }

        private static void IssueFollowAction(uint companion)
        {
            var master = GetMaster(companion);
            if (!GetIsObjectValid(master))
                return;

            var followDistance = GetState(companion).Mode == CompanionMode.Guard
                ? GuardFollowDistanceMeters
                : FollowDistanceMeters;
            AssignCommand(companion, () =>
            {
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
