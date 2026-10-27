using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.PlayerListService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the HoloCom calls. The logic and state live in <see cref="IHoloComService"/>; every member
    /// here forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class HoloCom
    {
        // Compile-time constant: callers get the value inlined, as before.
        /// <summary>
        /// The "Neutral" faction in Module/fac/repute.fac.json, which is neither hostile to nor hated by any faction.
        /// </summary>
        public const int HologramFactionId = Domain.World.HoloComService.HologramFactionId;

        private static IHoloComService Instance => FacadeAccessor.Resolve<IHoloComService>();

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnModuleDeath()
        {
            Instance.OnModuleDeath();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnModuleEnter()
        {
            Instance.OnModuleEnter();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnModuleLeave()
        {
            Instance.OnModuleLeave();
        }

        [NWNEventHandler(ScriptName.OnModuleChat)]
        public static void OnModuleChat()
        {
            Instance.OnModuleChat();
        }

        /// <summary>
        /// Where a player stands in a call: none, ringing someone, being rung, or connected.
        /// </summary>
        public static PlayerListCallState GetCallState(uint player)
        {
            return Instance.GetCallState(player);
        }

        /// <summary>
        /// Starts ringing another player. The caller checks <see cref="PlayerListRules"/> first.
        /// </summary>
        public static void StartCall(uint sender, uint receiver)
        {
            Instance.StartCall(sender, receiver);
        }

        /// <summary>
        /// Connects an incoming call. Answering is refused while either player is in space.
        /// </summary>
        public static void AnswerCall(uint player)
        {
            Instance.AnswerCall(player);
        }

        /// <summary>
        /// Declines an incoming call and tells the caller.
        /// </summary>
        public static void DeclineCall(uint player)
        {
            Instance.DeclineCall(player);
        }

        /// <summary>
        /// Ends a connected call or cancels an outgoing attempt.
        /// </summary>
        /// <returns>False when the player has neither.</returns>
        public static bool EndCall(uint player)
        {
            return Instance.EndCall(player);
        }

        public static bool IsInCall(uint player)
        {
            return Instance.IsInCall(player);
        }

        public static void SetIsInCall(uint sender, uint receiver, bool value = true)
        {
            Instance.SetIsInCall(sender, receiver, value);
        }

        /// <summary>
        /// Determines whether a creature is a HoloCom call hologram.
        /// </summary>
        public static bool IsHologram(uint creature)
        {
            return Instance.IsHologram(creature);
        }

        public static uint GetHoloGram(uint player)
        {
            return Instance.GetHoloGram(player);
        }

        public static uint GetHoloGramOwner(uint hologram)
        {
            return Instance.GetHoloGramOwner(hologram);
        }

        public static uint GetTargetForActiveCall(uint player)
        {
            return Instance.GetTargetForActiveCall(player);
        }

        public static bool IsCallSender(uint player)
        {
            return Instance.IsCallSender(player);
        }

        public static void SetIsCallSender(uint player, bool value = true)
        {
            Instance.SetIsCallSender(player, value);
        }

        public static uint GetCallSender(uint player)
        {
            return Instance.GetCallSender(player);
        }

        public static void SetCallSender(uint player, uint sender)
        {
            Instance.SetCallSender(player, sender);
        }

        public static bool IsCallReceiver(uint player)
        {
            return Instance.IsCallReceiver(player);
        }

        public static void SetIsCallReceiver(uint player, bool value = true)
        {
            Instance.SetIsCallReceiver(player, value);
        }

        public static uint GetCallReceiver(uint player)
        {
            return Instance.GetCallReceiver(player);
        }

        public static void SetCallReceiver(uint player, uint receiver)
        {
            Instance.SetCallReceiver(player, receiver);
        }

        public static int GetCallAttempt(uint player)
        {
            return Instance.GetCallAttempt(player);
        }

        public static void SetCallAttempt(uint player, int value = 0)
        {
            Instance.SetCallAttempt(player, value);
        }

        /// <summary>
        /// Cleans up call attempt state for both sender and receiver
        /// </summary>
        /// <param name="sender">The player who initiated the call</param>
        /// <param name="receiver">The player who was being called</param>
        public static void CleanupCallAttempt(uint sender, uint receiver)
        {
            Instance.CleanupCallAttempt(sender, receiver);
        }

        /// <summary>
        /// Comprehensive cleanup of all HoloCom state for a player
        /// </summary>
        /// <param name="player">The player to clean up</param>
        public static void CleanupAllHoloComState(uint player)
        {
            Instance.CleanupAllHoloComState(player);
        }
    }
}
