namespace SWLOR.Shared.Core.Constants
{
    public static class CommunicationConstants
    {
        /// <summary>
        /// Default OOC chat color as RGB values.
        /// </summary>
        public static (byte, byte, byte) OOCChatColor { get; } = (64, 64, 64);

        /// <summary>
        /// Default emote chat color as RGB values.
        /// </summary>
        public static (byte, byte, byte) EmoteChatColor { get; } = (0, 255, 0);
    }
}
