using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using AssociateCommand = SWLOR.NWN.API.NWScript.Enum.Associate.Command;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Companion control: the orders a player gives a droid or beast (follow, guard, stand ground, attack nearest, heal,
    /// toggle abilities), the owner events that make it defend, and the combat round that moves it. The registered
    /// companions are in <see cref="ICompanionControlStoreService"/>, the mode position in
    /// <see cref="ICompanionPositioningService"/> and the target it may engage in <see cref="ICompanionTargetAuthorityService"/>.
    /// The event handlers run on the container's instance and static callers reach it through <c>GameServices.CompanionControl</c>.
    /// </summary>
    public class CompanionControlService : ICompanionControlService
    {
        private const int MaximumNearestCreatureChecks = 64;
        // The native associate radial combines its Toggle label with base TLK 8127 ("casting").
        public const int AssociateAbilitiesLabelStrRef = 8127;

        private readonly IAIService _ai;
        private readonly IActivityService _activity;
        private readonly IPlayerNameService _playerName;
        private readonly IStatusEffectService _statusEffect;
        private readonly ICompanionControlStoreService _store;
        private readonly ICompanionPositioningService _positioning;
        private readonly ICompanionTargetAuthorityService _authority;

        public CompanionControlService(
            IAIService ai,
            IActivityService activity,
            IPlayerNameService playerName,
            IStatusEffectService statusEffect,
            ICompanionControlStoreService store,
            ICompanionPositioningService positioning,
            ICompanionTargetAuthorityService authority)
        {
            _ai = ai;
            _activity = activity;
            _playerName = playerName;
            _statusEffect = statusEffect;
            _store = store;
            _positioning = positioning;
            _authority = authority;
        }

        public void Initialize(uint companion, bool preserveActionQueue = false)
        {
            if (!_store.IsControlledCompanion(companion))
                return;

            _store.Register(companion);

            SetAssociateListenPatterns(companion);
            _positioning.IssueFollowAction(companion, !preserveActionQueue);
        }

        public void Clear(uint companion)
        {
            _store.Remove(companion);
            _ai.ClearAIState(companion);
        }

        public bool HandleConversation(uint companion)
        {
            if (!_store.IsControlledCompanion(companion))
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
            if (!_store.IsControlledCompanion(companion) || !_authority.IsValidHostileTarget(companion, threat))
                return;

            var state = _store.GetState(companion);
            if (state.TrackedTarget == threat &&
                CompanionControlPolicy.HasPathingTimedOut(state.LastProgressAt, UtcNow()))
            {
                _positioning.ResetProgress(state);
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
            if (!_store.IsControlledCompanion(companion) ||
                GetIsObjectValid(_store.GetState(companion).AttackNearestTarget) ||
                !_authority.IsValidHostileTarget(companion, target))
            {
                return;
            }

            var state = _store.GetState(companion);
            state.OwnerAssistTarget = target;
            state.ExplicitOrderUntil = default;
            ProcessCombatRound(companion);
        }

        public void BeginExplicitOrder(uint companion, float durationSeconds = 30f)
        {
            if (!_store.IsControlledCompanion(companion))
                return;

            InterruptAbilityActivation(companion);
            var state = _store.GetState(companion);
            state.ExplicitOrderUntil = UtcNow().AddSeconds(Math.Max(1f, durationSeconds));
        }

        public void CancelExplicitOrder(uint companion)
        {
            if (_store.TryGetState(companion, out var state))
                state.ExplicitOrderUntil = default;
        }

        public void ProcessCombatRound(uint companion, bool bypassDecisionThrottle = false)
        {
            if (!_store.IsControlledCompanion(companion))
                return;

            ResumeFromRestIfMasterIsActive(companion, GetMaster(companion));
            if (IsBusy(companion))
                return;

            var state = _store.GetState(companion);
            if (_positioning.IsExplicitOrderInProgress(companion))
                return;

            if (state.DefensiveReactionPending)
            {
                state.DefensiveReactionPending = false;
                bypassDecisionThrottle = true;
            }

            var target = _authority.AdvanceAuthorizedTarget(companion);
            if (!GetIsObjectValid(target))
                _positioning.MaintainModePosition(companion);

            var actionIssued = _ai.ProcessTrigger(
                companion,
                AITriggerType.CombatRound,
                target,
                bypassDecisionThrottle);
            if (actionIssued &&
                GetIsObjectValid(target) &&
                state.TrackedTarget != target)
            {
                _authority.StartTracking(state, companion, target);
            }
        }

        public void ProcessHeartbeat(uint companion)
        {
            if (!_store.IsControlledCompanion(companion))
            {
                Clear(companion);
                return;
            }

            var state = _store.GetState(companion);
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
            if (_store.IsControlledCompanion(companion))
                _positioning.MaintainModePosition(companion);
        }

        public bool TryProcessPendingDefensiveReaction(uint companion)
        {
            if (!_store.TryGetState(companion, out var state) ||
                !state.DefensiveReactionPending)
            {
                return false;
            }

            ProcessCombatRound(companion, true);
            return true;
        }

        public void ResetOwnerCompanionToFollow(uint master)
        {
            if (!GetIsObjectValid(master))
                return;

            var companion = GetAssociate(AssociateType.Henchman, master);
            if (_store.IsControlledCompanion(companion))
                ResetToFollow(companion, false, true);
        }
        [PlayerSession(PlayerReload.LoginOnly,
            "The TLK override stays in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void RenameToggleAbilitiesCommand()
        {
            var player = GetEnteringObject();
            if (GetIsPC(player))
                PlayerPlugin.SetTlkOverride(player, AssociateAbilitiesLabelStrRef, "abilities");
        }
        [NWNEventHandler(ScriptName.OnPlayerAttacked)]
        public void OnOwnerAttacked()
        {
            RegisterOwnerThreat(OBJECT_SELF, GetLastAttacker(OBJECT_SELF));
        }
        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public void OnOwnerDamaged()
        {
            RegisterOwnerThreat(OBJECT_SELF, GetLastDamager(OBJECT_SELF));
        }
        [NWNEventHandler(ScriptName.OnPlayerSpellCastAt)]
        public void OnHostileSpellCastAtOwner()
        {
            if (GetLastSpellHarmful())
                RegisterOwnerThreat(OBJECT_SELF, GetLastSpellCaster());
        }
        [NWNEventHandler(ScriptName.OnPlayerRoundEnd)]
        public void OnOwnerCombatRound()
        {
            var master = OBJECT_SELF;
            var companion = GetAssociate(AssociateType.Henchman, master);
            RegisterOwnerAttack(companion, GetAttackTarget(master));
        }
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void OnOwnerAreaExit()
        {
            var exitingObject = GetExitingObject();
            if (GetIsPC(exitingObject))
                ResetOwnerCompanionToFollow(exitingObject);
        }

        private void SetMode(uint companion, CompanionMode mode, string response)
        {
            InterruptAndClear(companion);
            var state = _store.GetState(companion);
            state.Mode = mode;
            _positioning.ClearEngagements(state);

            if (mode != CompanionMode.StandGround)
                _positioning.IssueFollowAction(companion);

            Log.Write(LogGroup.AI, $"{GetName(companion)} entered companion mode {mode}.");
            _positioning.SendResponse(companion, response);
        }

        private void AttackNearest(uint companion)
        {
            var target = FindNearestVisibleEnemy(companion);
            if (!GetIsObjectValid(target))
            {
                ResetToFollow(companion, false, true);
                _positioning.SendResponse(companion, $"No visible enemy is within {CompanionControlPolicy.AttackNearestRangeMeters:0} meters.");
                return;
            }

            InterruptAndClear(companion);
            var state = _store.GetState(companion);
            state.Mode = CompanionMode.Follow;
            _positioning.ClearEngagements(state);
            state.AttackNearestTarget = target;
            var master = GetMaster(companion);
            _positioning.SendResponse(companion, $"Attacking {GetDisplayName(master, target)}, then returning to Follow.");
            ProcessCombatRound(companion, true);
        }

        private void HealMaster(uint companion)
        {
            InterruptAndClear(companion);
            ResetToFollow(companion, false, false);

            var master = GetMaster(companion);
            if (GetCurrentHitPoints(master) >= GetMaxHitPoints(master))
            {
                _positioning.SendResponse(companion, "You do not need healing.");
                _positioning.IssueFollowAction(companion);
                return;
            }

            if (!_store.AreAbilitiesEnabled(companion))
            {
                _positioning.SendResponse(companion, "Unable to heal: abilities are disabled.");
                _positioning.IssueFollowAction(companion);
                return;
            }

            if (_ai.TryUseBestTargetedSupportAbility(companion, master, out var abilityName))
            {
                _positioning.SendResponse(companion, $"Using {abilityName} on you, then returning to Follow.");
                return;
            }

            _positioning.SendResponse(companion, "Unable to heal you: no compatible healing ability is ready.");
            _positioning.IssueFollowAction(companion);
        }

        private void ToggleAbilities(uint companion)
        {
            InterruptAndClear(companion);
            var state = _store.GetState(companion);
            state.AbilitiesEnabled = !state.AbilitiesEnabled;
            if (!state.AbilitiesEnabled)
                DequeueWeaponAbility(companion);

            _positioning.SendResponse(companion, state.AbilitiesEnabled
                ? "Abilities enabled."
                : "Abilities disabled; basic attacks only.");
            ProcessCombatRound(companion);
        }

        private void RegisterOwnerThreat(uint master, uint threat)
        {
            var companion = GetAssociate(AssociateType.Henchman, master);
            RegisterDefensiveThreat(companion, threat);
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
                    _authority.IsValidHostileTarget(companion, target) &&
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

            var state = _store.GetState(companion);
            state.Mode = CompanionMode.Follow;
            _positioning.ClearEngagements(state);

            if (sendResponse)
                _positioning.SendResponse(companion, "Returning to Follow.");

            _positioning.MaintainModePosition(companion);
        }

        private void InterruptAndClear(uint companion)
        {
            CancelExplicitOrder(companion);
            InterruptAbilityActivation(companion);
            AssignCommand(companion, () => ClearAllActions(true));
        }

        // Each protected virtual method below has the original call as its body, so the server behaves as it did.
        // They name the static services that are not converted yet (and the clock), so a test can answer them.

        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual bool IsBusy(uint target) => _activity.IsBusy(target);

        protected virtual ActivityStatusType GetBusyType(uint target) => _activity.GetBusyType(target);

        protected virtual void ClearBusy(uint target) => _activity.ClearBusy(target);

        protected virtual bool HasRestStatusEffect(uint creature) =>
            _statusEffect.HasStatusEffect(creature, typeof(RestStatusEffect));

        protected virtual void RemoveRestStatusEffect(uint creature) =>
            _statusEffect.RemoveStatusEffect(creature, typeof(RestStatusEffect), false);

        protected virtual bool InterruptAbilityActivation(uint activator) =>
            UsePerkFeat.InterruptAbilityActivation(activator);

        protected virtual void DequeueWeaponAbility(uint creature) =>
            UsePerkFeat.DequeueWeaponAbility(creature, false);

        protected virtual string GetDisplayName(uint observer, uint target) =>
            _playerName.GetDisplayName(observer, target);
    }
}
