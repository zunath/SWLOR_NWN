using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Puts a player into targeting mode and runs the action the player's next target selection answers.</summary>
    public interface ITargetingService
    {
        /// <summary>
        /// Forces player to enter targeting mode.
        /// When the player targets an object, the selectionAction specified will run.
        /// </summary>
        /// <param name="player">The player entering targeting mode.</param>
        /// <param name="objectType">The types of objects allowed to be targeted.</param>
        /// <param name="message">The message to send to the player when entering targeting mode.</param>
        /// <param name="selectionAction">The action to run when an object is targeted.</param>
        /// <param name="allowsLocationTarget">true if ground selections should execute the selection action.</param>
        void EnterTargetingMode(
            uint player,
            ObjectType objectType,
            string message,
            Action<uint> selectionAction,
            bool allowsLocationTarget = false);

        /// <summary>When a player targets an object, execute the assigned action.</summary>
        void RunTargetedItemAction();
    }
}
