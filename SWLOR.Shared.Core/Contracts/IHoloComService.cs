using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IHoloComService
    {
        /// <summary>
        /// Determines if a player is currently in a HoloCom call.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>true if in call, false otherwise</returns>
        bool IsInCall(uint player);

        /// <summary>
        /// Gets the target for the active call.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>The target of the active call</returns>
        uint GetTargetForActiveCall(uint player);

        /// <summary>
        /// Sets whether a player is in a call.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        /// <param name="target">The target of the call.</param>
        /// <param name="isInCall">Whether the player is in a call.</param>
        void SetIsInCall(uint player, uint target, bool isInCall);

        /// <summary>
        /// Determines if a player is a call sender.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>true if call sender, false otherwise</returns>
        bool IsCallSender(uint player);

        /// <summary>
        /// Gets the call receiver for a player.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>The call receiver</returns>
        uint GetCallReceiver(uint player);

        /// <summary>
        /// Cleans up a call attempt.
        /// </summary>
        /// <param name="sender">The sender of the call.</param>
        /// <param name="receiver">The receiver of the call.</param>
        void CleanupCallAttempt(uint sender, uint receiver);

        /// <summary>
        /// Determines if a player is a call receiver.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>true if call receiver, false otherwise</returns>
        bool IsCallReceiver(uint player);

        /// <summary>
        /// Gets the call sender for a player.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>The call sender</returns>
        uint GetCallSender(uint player);

        /// <summary>
        /// Sets a player as a call sender.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        void SetIsCallSender(uint player);

        /// <summary>
        /// Sets a player as a call sender with a specific value.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        /// <param name="isCallSender">Whether the player is a call sender.</param>
        void SetIsCallSender(uint player, bool isCallSender);

        /// <summary>
        /// Sets the call sender for a player.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        /// <param name="sender">The sender of the call.</param>
        void SetCallSender(uint player, uint sender);

        /// <summary>
        /// Sets whether a player is a call receiver.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        /// <param name="isCallReceiver">Whether the player is a call receiver.</param>
        void SetIsCallReceiver(uint player, bool isCallReceiver);

        /// <summary>
        /// Sets the call receiver for a player.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        /// <param name="receiver">The receiver of the call.</param>
        void SetCallReceiver(uint player, uint receiver);

        /// <summary>
        /// Gets the call attempt count for a player.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>The call attempt count</returns>
        int GetCallAttempt(uint player);

        /// <summary>
        /// Sets the call attempt count for a player.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        /// <param name="attempt">The attempt count.</param>
        void SetCallAttempt(uint player, int attempt);

        /// <summary>
        /// Gets the HoloGram owner for a player.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>The HoloGram owner</returns>
        uint GetHoloGramOwner(uint player);

        /// <summary>
        /// Cleans up all HoloCom state for a player.
        /// </summary>
        /// <param name="player">The player to clean up.</param>
        void CleanupAllHoloComState(uint player);
    }
}
