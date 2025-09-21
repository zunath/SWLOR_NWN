using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IHoloComService
    {
        void OnModuleDeath();
        void OnModuleEnter();
        void OnModuleLeave();
        void OnModuleChat();
        bool IsInCall(uint player);
        void SetIsInCall(uint sender, uint receiver, bool value = true);
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
