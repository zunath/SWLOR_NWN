using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface ICommunication
    {
        /// <summary>
        /// Gets the OOC chat color as a tuple of RGB values.
        /// </summary>
        (byte, byte, byte) OOCChatColor { get; }

        /// <summary>
        /// Gets the emote chat color as a tuple of RGB values.
        /// </summary>
        (byte, byte, byte) EmoteChatColor { get; }

        /// <summary>
        /// Gets the emote style for a player.
        /// </summary>
        /// <param name="player">The player to get the emote style for.</param>
        /// <returns>The emote style for the player.</returns>
        EmoteStyle GetEmoteStyle(uint player);

        /// <summary>
        /// Sets the emote style for a player.
        /// </summary>
        /// <param name="player">The player to set the emote style for.</param>
        /// <param name="style">The emote style to set.</param>
        void SetEmoteStyle(uint player, EmoteStyle style);
    }
}
