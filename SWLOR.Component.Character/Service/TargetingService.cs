using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Character.Service
{

    public class TargetingService : ITargetingService
    {
        private readonly Dictionary<uint, Action<uint>> _playerTargetingActions = new();

        /// <summary>
        /// Forces player to enter targeting mode.
        /// When the player targets an object, the selectionAction specified will run.
        /// </summary>
        /// <param name="player">The player entering targeting mode.</param>
        /// <param name="objectType">The types of objects allowed to be targeted.</param>
        /// <param name="selectionAction">The action to run when an object is targeted.</param>
        /// <param name="message">The message to send to the player when entering targeting mode.</param>
        public void EnterTargetingMode(
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
        [ScriptHandler<OnModulePlayerTarget>]
        public void RunTargetedItemAction()
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
