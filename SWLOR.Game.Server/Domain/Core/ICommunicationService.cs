using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Chat handling: the in-character, Comms and out-of-character channels, language coloring and translation, emote
    /// styles, and the chat-channel labels. One member per public member of the old static <c>Communication</c> class.
    /// </summary>
    public interface ICommunicationService
    {
        /// <summary>The default color of out-of-character text (red, green, blue).</summary>
        (byte, byte, byte) OOCChatColor { get; }

        /// <summary>The default color of emote text (red, green, blue).</summary>
        (byte, byte, byte) EmoteChatColor { get; }

        /// <summary>Relabels the Party chat channel as Comms on the client of a player who enters the module.</summary>
        void ApplyCommsChannelName();

        /// <summary>Labels the Shout chat channel on the client of a player or DM who enters the module.</summary>
        void ApplyShoutChannelName();

        /// <summary>
        /// Whenever a DM possesses a creature, clear its busy status to prevent ability usage issues.
        /// This runs before the engine accepts the possession, and the engine refuses creatures that have a
        /// master (familiars, henchmen, and other associates), so nothing here may assume the possession happened.
        /// </summary>
        void OnDMPossess();

        /// <summary>
        /// When a player focuses the chatbar, set a typing indicator on the player; when
        /// unfocused, remove the indicator.
        /// </summary>
        void TypingIndicator();

        /// <summary>Runs the DMFI voice command handler, which lives in nwscript land.</summary>
        void ProcessNativeChatMessage();

        /// <summary>
        /// Handles a chat message: skips the native message and sends each recipient the colored, language-translated
        /// text for the channel (talk, whisper, Comms), or the notices for a disabled channel or a dead speaker.
        /// </summary>
        void ProcessChatMessage();

        /// <summary>The emote style of a player, or of a DM (shared by their avatar and every creature they possess).</summary>
        /// <param name="player">A player, a DM avatar, or the creature a DM possesses.</param>
        EmoteStyle GetEmoteStyle(uint player);

        /// <summary>
        /// Saves a player's or DM's emote style.
        /// </summary>
        /// <param name="player">A player, a DM avatar, or the creature a DM possesses.</param>
        /// <param name="style">The new style.</param>
        /// <returns>false when there was nowhere to save it.</returns>
        bool SetEmoteStyle(uint player, EmoteStyle style);
    }
}
