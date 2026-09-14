using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class Targeting
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

        private static readonly Dictionary<uint, TargetingActionDetail> _playerTargetingActions = new();

        /// <summary>
        /// Forces player to enter targeting mode.
        /// When the player targets an object, the selectionAction specified will run.
        /// </summary>
        /// <param name="player">The player entering targeting mode.</param>
        /// <param name="objectType">The types of objects allowed to be targeted.</param>
        /// <param name="selectionAction">The action to run when an object is targeted.</param>
        /// <param name="message">The message to send to the player when entering targeting mode.</param>
        /// <param name="allowsLocationTarget">true if ground selections should execute the selection action.</param>
        public static void EnterTargetingMode(
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

        /// <summary>
        /// When a player targets an object, execute the assigned action.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModulePlayerTarget)]
        public static void RunTargetedItemAction()
        {
            var player = GetLastPlayerToSelectTarget();
            if (!_playerTargetingActions.ContainsKey(player))
                return;
            var targetedObject = GetTargetingModeSelectedObject();
            var targetedLocation = GetTargetingModeSelectedPosition();
            var targetingAction = _playerTargetingActions[player];

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
