using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Puts a player into targeting mode and runs the action the player's next target selection answers. The logic that
    /// used to be static in <c>Service.Targeting</c> is moved here unchanged; the pending actions are this instance's own.
    /// The event handler runs on the container's instance; static callers reach it through <c>GameServices.Targeting</c>.
    /// </summary>
    public class TargetingService : ITargetingService
    {
        private class TargetingActionDetail
        {
            public TargetingActionDetail(Action<uint> selectionAction, bool allowsLocationTarget)
            {
                SelectionAction = selectionAction;
                AllowsLocationTarget = allowsLocationTarget;
            }

            public Action<uint> SelectionAction { get; }
            public bool AllowsLocationTarget { get; }
        }

        private readonly Dictionary<uint, TargetingActionDetail> _playerTargetingActions = new();

        public void EnterTargetingMode(
            uint player,
            ObjectType objectType,
            string message,
            Action<uint> selectionAction,
            bool allowsLocationTarget = false)
        {
            NWScript.EnterTargetingMode(player, objectType);
            _playerTargetingActions[player] = new TargetingActionDetail(selectionAction, allowsLocationTarget);

            if (!string.IsNullOrWhiteSpace(message))
            {
                SendMessageToPC(player, message);
            }
        }

        [NWNEventHandler(ScriptName.OnModulePlayerTarget)]
        public void RunTargetedItemAction()
        {
            var player = GetLastPlayerToSelectTarget();
            if (!_playerTargetingActions.TryGetValue(player, out var targetingAction))
                return;

            // Each registration answers one targeting click. Left in place, it would rerun on a later
            // targeting click started by code that registers its own handler.
            _playerTargetingActions.Remove(player);
            var targetedObject = GetTargetingModeSelectedObject();
            var targetedLocation = GetTargetingModeSelectedPosition();

            if (GetIsObjectValid(targetedObject))
            {
                targetingAction.SelectionAction(targetedObject);
            }
            else if (targetingAction.AllowsLocationTarget && targetedLocation != Vector3())
            {
                targetingAction.SelectionAction(OBJECT_INVALID);
            }
        }
    }
}
