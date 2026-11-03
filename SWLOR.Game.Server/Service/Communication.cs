using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the chat handling. The logic lives in <see cref="ICommunicationService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class Communication
    {
        private static ICommunicationService Instance => FacadeAccessor.Resolve<ICommunicationService>();

        public const string EventCommsAreaVariable = Domain.Core.CommunicationService.EventCommsAreaVariable;
        public const string SuppressChatAuditVariable = "SUPPRESS_CHAT_AUDIT";

        public static (byte, byte, byte) OOCChatColor => Instance.OOCChatColor;
        public static (byte, byte, byte) EmoteChatColor => Instance.EmoteChatColor;

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyCommsChannelName()
        {
            Instance.ApplyCommsChannelName();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyShoutChannelName()
        {
            Instance.ApplyShoutChannelName();
        }

        /// <summary>
        /// Whenever a DM possesses a creature, clear its busy status to prevent ability usage issues.
        /// This runs before the engine accepts the possession, and the engine refuses creatures that have a
        /// master (familiars, henchmen, and other associates), so nothing here may assume the possession happened.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void OnDMPossess()
        {
            Instance.OnDMPossess();
        }

        /// <summary>
        /// When a player focuses the chatbar, set a typing indicator on the player; when
        /// unfocused, remove the indicator.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleGuiEvent)]
        public static void TypingIndicator()
        {
            Instance.TypingIndicator();
        }

        // Register DMFI Voice Command Handler which lives in nwscript land.
        [NWNEventHandler(ScriptName.OnModuleChat)]
        public static void ProcessNativeChatMessage()
        {
            Instance.ProcessNativeChatMessage();
        }

        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public static void ProcessChatMessage()
        {
            Instance.ProcessChatMessage();
        }

        public static EmoteStyle GetEmoteStyle(uint player)
        {
            return Instance.GetEmoteStyle(player);
        }

        /// <summary>
        /// Saves a player's or DM's emote style.
        /// </summary>
        /// <param name="player">A player, a DM avatar, or the creature a DM possesses.</param>
        /// <param name="style">The new style.</param>
        /// <returns>false when there was nowhere to save it.</returns>
        public static bool SetEmoteStyle(uint player, EmoteStyle style)
        {
            return Instance.SetEmoteStyle(player, style);
        }
    }
}
