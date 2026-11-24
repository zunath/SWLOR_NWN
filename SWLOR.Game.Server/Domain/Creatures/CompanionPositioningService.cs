using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Where a companion stands and what it is told: the follow action, the mode position (follow, guard or stand ground),
    /// the reset of its engagements and progress, the check for an order in progress and the responses it sends its master.
    /// Moved here unchanged from <see cref="CompanionControlService"/>.
    /// </summary>
    public class CompanionPositioningService : ICompanionPositioningService
    {
        private const float FollowDistanceMeters = 2f;
        private const float GuardFollowDistanceMeters = 3f;

        private readonly ICompanionControlStoreService _store;
        private readonly IActivityService _activity;

        public CompanionPositioningService(
            ICompanionControlStoreService store,
            IActivityService activity)
        {
            _store = store;
            _activity = activity;
        }

        public void ClearEngagements(CompanionControlState state)
        {
            state.AttackNearestTarget = OBJECT_INVALID;
            state.OwnerAssistTarget = OBJECT_INVALID;
            state.DefensiveThreats.Clear();
            state.DefensiveReactionPending = false;
            ResetProgress(state);
        }

        public void ResetProgress(CompanionControlState state)
        {
            state.TrackedTarget = OBJECT_INVALID;
            state.LastDistanceToTarget = float.MaxValue;
            state.LastProgressAt = default;
            state.LastOffensiveActivityAt = default;
        }

        public bool IsExplicitOrderInProgress(uint companion)
        {
            var state = _store.GetState(companion);
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

        public void MaintainModePosition(uint companion)
        {
            if (IsBusy(companion) || IsExplicitOrderInProgress(companion))
                return;

            var state = _store.GetState(companion);
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

        public void IssueFollowAction(uint companion, bool clearActions = true)
        {
            var master = GetMaster(companion);
            if (!GetIsObjectValid(master))
                return;

            var followDistance = _store.GetState(companion).Mode == CompanionMode.Guard
                ? GuardFollowDistanceMeters
                : FollowDistanceMeters;
            AssignCommand(companion, () =>
            {
                if (clearActions)
                    ClearAllActions(true);

                ActionForceFollowObject(master, followDistance);
            });
        }

        public void SendResponse(uint companion, string response)
        {
            var master = GetMaster(companion);
            if (GetIsObjectValid(master))
                SendMessageToPC(master, $"{GetName(companion)}: {response}");
        }

        // Each protected virtual method below has the original call as its body, so the server behaves as it did.
        // They name the static services that are not converted yet (and the clock), so a test can answer them.

        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual bool IsBusy(uint target) => _activity.IsBusy(target);
    }
}
