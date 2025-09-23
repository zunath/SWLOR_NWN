using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Communication.Contracts
{
    public interface ICommunicationService
    {
        (byte, byte, byte) OOCChatColor { get; }
        (byte, byte, byte) EmoteChatColor { get; }

        /// <summary>
        /// Whenever a DM possesses a creature, track the NPC on their object so that messages can be
        /// sent to them during the possession.
        /// </summary>
        void OnDMPossess();

        /// <summary>
        /// When a player enters the server, set a local bool on their PC representing
        /// the current state of their holonet visibility.
        /// </summary>
        void LoadHolonetSetting();

        /// <summary>
        /// When a player focuses the chatbar, set a typing indicator on the player; when
        /// unfocused, remove the indicator.
        /// </summary>
        void TypingIndicator();

        void ProcessNativeChatMessage();
        void ProcessChatMessage();
        EmoteStyle GetEmoteStyle(uint player);
        void SetEmoteStyle(uint player, EmoteStyle style);
        void ConfigureFeedbackMessages();
    }

}
