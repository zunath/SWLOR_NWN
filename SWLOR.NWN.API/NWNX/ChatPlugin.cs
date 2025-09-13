using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public static class ChatPlugin
    {
        /// <summary>
        /// Sends a chat message.
        /// </summary>
        /// <param name="channel">The channel to send the message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="target">The receiver of the message.</param>
        /// <returns>True if successful, false otherwise.</returns>
        /// <remarks>If no target is provided, then it broadcasts to all eligible targets.</remarks>
        public static int SendMessage(ChatChannel channel, string message, uint sender, uint target)
        {
            return global::NWN.Core.NWNX.ChatPlugin.SendMessage((int)channel, message, sender, target);
        }

        /// <summary>
        /// Registers the script which receives all chat messages.
        /// </summary>
        /// <param name="script">The script name to handle the chat events.</param>
        /// <remarks>If a script was previously registered, this one will take over.</remarks>
        public static void RegisterChatScript(string script)
        {
            global::NWN.Core.NWNX.ChatPlugin.RegisterChatScript(script);
        }

        /// <summary>
        /// Skips a chat message.
        /// </summary>
        /// <remarks>Must be called from a chat or system script handler.</remarks>
        public static void SkipMessage()
        {
            global::NWN.Core.NWNX.ChatPlugin.SkipMessage();
        }

        /// <summary>
        /// Gets the chat channel.
        /// </summary>
        /// <returns>The channel the message is sent.</returns>
        /// <remarks>Must be called from a chat or system script handler.</remarks>
        public static ChatChannel GetChannel()
        {
            int result = global::NWN.Core.NWNX.ChatPlugin.GetChannel();
            return (ChatChannel)result;
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <returns>The message sent.</returns>
        /// <remarks>Must be called from a chat or system script handler.</remarks>
        public static string GetMessage()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetMessage();
        }

        /// <summary>
        /// Gets the sender of the message.
        /// </summary>
        /// <returns>The object sending the message.</returns>
        /// <remarks>Must be called from a chat or system script handler.</remarks>
        public static uint GetSender()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetSender();
        }

        /// <summary>
        /// Gets the target of the message.
        /// </summary>
        /// <returns>The target of the message or OBJECT_INVALID if no target.</returns>
        /// <remarks>Must be called from a chat or system script handler.</remarks>
        public static uint GetTarget()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetTarget();
        }

        /// <summary>
        /// Sets the distance with which the player hears talks or whispers.
        /// </summary>
        /// <param name="distance">The distance in meters.</param>
        /// <param name="listener">The listener, if OBJECT_INVALID then it will be set server wide.</param>
        /// <param name="channel">The channel to modify the distance heard. Only applicable for talk and whisper.</param>
        /// <remarks>Per player settings override server wide.</remarks>
        public static void SetChatHearingDistance(
            float distance, 
            uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            uint listenerValue = listener ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.ChatPlugin.SetChatHearingDistance(distance, listenerValue, (int)channel);
        }

        /// <summary>
        /// Gets the distance with which the player hears talks or whisper.
        /// </summary>
        /// <param name="listener">The listener, if OBJECT_INVALID then will return server wide setting.</param>
        /// <param name="channel">The channel. Only applicable for talk and whisper.</param>
        /// <returns>The hearing distance.</returns>
        public static float GetChatHearingDistance(uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            uint listenerValue = listener ?? OBJECT_INVALID;
            return global::NWN.Core.NWNX.ChatPlugin.GetChatHearingDistance(listenerValue, (int)channel);
        }
    }
}