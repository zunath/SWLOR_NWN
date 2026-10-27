using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// HoloCom calls between players: ringing, answering, connected calls with their holograms, and the state kept on
    /// each player. The static facade <c>Service.HoloCom</c> forwards to the container's instance of this interface,
    /// repeats the faction constant and keeps the event handlers.
    /// </summary>
    public interface IHoloComService
    {
        /// <summary>
        /// When a player dies in a call, ends the call.
        /// </summary>
        void OnModuleDeath();

        /// <summary>
        /// When a player enters the module, removes the HoloCom immobilize from them.
        /// </summary>
        void OnModuleEnter();

        /// <summary>
        /// When a player leaves the module, cleans up every call state they had.
        /// </summary>
        void OnModuleLeave();

        /// <summary>
        /// When a player in a call speaks, their hologram at the other end says the same thing.
        /// </summary>
        void OnModuleChat();

        /// <summary>
        /// Where a player stands in a call: none, ringing someone, being rung, or connected.
        /// </summary>
        PlayerListCallState GetCallState(uint player);

        /// <summary>
        /// Starts ringing another player. The caller checks <see cref="PlayerListRules"/> first.
        /// </summary>
        void StartCall(uint sender, uint receiver);

        /// <summary>
        /// Connects an incoming call. Answering is refused while either player is in space.
        /// </summary>
        void AnswerCall(uint player);

        /// <summary>
        /// Declines an incoming call and tells the caller.
        /// </summary>
        void DeclineCall(uint player);

        /// <summary>
        /// Ends a connected call or cancels an outgoing attempt.
        /// </summary>
        /// <returns>False when the player has neither.</returns>
        bool EndCall(uint player);

        bool IsInCall(uint player);

        void SetIsInCall(uint sender, uint receiver, bool value = true);

        /// <summary>
        /// Determines whether a creature is a HoloCom call hologram.
        /// </summary>
        bool IsHologram(uint creature);

        uint GetHoloGram(uint player);

        uint GetHoloGramOwner(uint hologram);

        uint GetTargetForActiveCall(uint player);

        bool IsCallSender(uint player);

        void SetIsCallSender(uint player, bool value = true);

        uint GetCallSender(uint player);

        void SetCallSender(uint player, uint sender);

        bool IsCallReceiver(uint player);

        void SetIsCallReceiver(uint player, bool value = true);

        uint GetCallReceiver(uint player);

        void SetCallReceiver(uint player, uint receiver);

        int GetCallAttempt(uint player);

        void SetCallAttempt(uint player, int value = 0);

        /// <summary>
        /// Cleans up call attempt state for both sender and receiver
        /// </summary>
        /// <param name="sender">The player who initiated the call</param>
        /// <param name="receiver">The player who was being called</param>
        void CleanupCallAttempt(uint sender, uint receiver);

        /// <summary>
        /// Comprehensive cleanup of all HoloCom state for a player
        /// </summary>
        /// <param name="player">The player to clean up</param>
        void CleanupAllHoloComState(uint player);
    }
}
