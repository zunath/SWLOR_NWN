using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class Targeting
    {
        private static readonly Dictionary<uint, Action<uint>> _playerTargetingActions = new();

        /// <summary>
        /// Forces player to enter targeting mode.
        /// When the player targets an object, the selectionAction specified will run.
        /// </summary>
        /// <param name="player">The player entering targeting mode.</param>
        /// <param name="objectType">The types of objects allowed to be targeted.</param>
        /// <param name="selectionAction">The action to run when an object is targeted.</param>
        /// <param name="message">The message to send to the player when entering targeting mode.</param>
        public static void EnterTargetingMode(
            uint player, 
            ObjectType objectType,
            string message,
            Action<uint> selectionAction)
        {
            NWScript.EnterTargetingMode(player, objectType);
            _playerTargetingActions[player] = selectionAction;

            if (!string.IsNullOrWhiteSpace(message))
            {
                SendMessageToPC(player, message);
            }
        }

        /// <summary>
        /// When a player targets an object, execute the assigned action.
        /// </summary>
        [NWNEventHandler("mod_p_target")]
        public static void RunTargetedItemAction()
        {
            var player = GetLastPlayerToSelectTarget();
            if (!_playerTargetingActions.ContainsKey(player))
                return;
            var targetedObject = GetTargetingModeSelectedObject();

            if (GetIsObjectValid(targetedObject))
            {
                _playerTargetingActions[player](targetedObject);
            }
        }
    }
}
