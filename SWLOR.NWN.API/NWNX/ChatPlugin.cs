using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive chat system functionality including message sending, receiving, filtering,
    /// and custom chat channel management. This plugin allows for advanced chat system customization
    /// and monitoring of all chat communications.
    /// </summary>
    public static class ChatPlugin
    {
        /// <summary>
        /// Sends a chat message through the specified channel to the target recipient.
        /// </summary>
        /// <param name="channel">The chat channel to send the message through. See ChatChannel enum for available channels.</param>
        /// <param name="message">The message content to send. Cannot be null or empty.</param>
        /// <param name="sender">The object ID of the message sender. Must be a valid object.</param>
        /// <param name="target">The object ID of the message recipient. Use OBJECT_INVALID to broadcast to all eligible targets.</param>
        /// <returns>1 if the message was sent successfully, 0 if it failed.</returns>
        /// <remarks>
        /// If target is OBJECT_INVALID, the message will be broadcast to all eligible targets based on the channel type.
        /// Different channels have different visibility rules and target eligibility.
        /// The sender must be a valid object that can send messages on the specified channel.
        /// </remarks>
        public static int SendMessage(ChatChannel channel, string message, uint sender, uint target)
        {
            return global::NWN.Core.NWNX.ChatPlugin.SendMessage((int)channel, message, sender, target);
        }

        /// <summary>
        /// Registers a script to handle all incoming chat messages and events.
        /// </summary>
        /// <param name="script">The name of the script to register for handling chat events. Must be a valid script name.</param>
        /// <remarks>
        /// The registered script will receive all chat messages before they are processed.
        /// This allows for message filtering, modification, logging, or custom chat behavior.
        /// If a script was previously registered, this one will replace it and take over.
        /// The script should be designed to handle chat events and can use other ChatPlugin methods to inspect messages.
        /// </remarks>
        public static void RegisterChatScript(string script)
        {
            global::NWN.Core.NWNX.ChatPlugin.RegisterChatScript(script);
        }

        /// <summary>
        /// Prevents the current chat message from being processed and displayed.
        /// </summary>
        /// <remarks>
        /// This method must be called from within a chat or system script handler.
        /// When called, the current message will be completely blocked and not sent to any recipients.
        /// This is useful for implementing chat filters, profanity blocking, or custom chat rules.
        /// The message will be silently dropped without any indication to the sender.
        /// </remarks>
        public static void SkipMessage()
        {
            global::NWN.Core.NWNX.ChatPlugin.SkipMessage();
        }

        /// <summary>
        /// Retrieves the chat channel of the current message being processed.
        /// </summary>
        /// <returns>The chat channel of the current message. See ChatChannel enum for possible values.</returns>
        /// <remarks>
        /// This method must be called from within a chat or system script handler.
        /// It returns the channel type of the message currently being processed.
        /// This is useful for implementing channel-specific logic or filtering.
        /// </remarks>
        public static ChatChannel GetChannel()
        {
            int result = global::NWN.Core.NWNX.ChatPlugin.GetChannel();
            return (ChatChannel)result;
        }

        /// <summary>
        /// Retrieves the content of the current message being processed.
        /// </summary>
        /// <returns>The message content as a string. Returns empty string if no message is being processed.</returns>
        /// <remarks>
        /// This method must be called from within a chat or system script handler.
        /// It returns the actual text content of the message currently being processed.
        /// This is useful for message analysis, filtering, or modification before sending.
        /// </remarks>
        public static string GetMessage()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetMessage();
        }

        /// <summary>
        /// Retrieves the sender of the current message being processed.
        /// </summary>
        /// <returns>The object ID of the message sender. Returns OBJECT_INVALID if no sender is available.</returns>
        /// <remarks>
        /// This method must be called from within a chat or system script handler.
        /// It returns the object that sent the message currently being processed.
        /// This is useful for implementing sender-based filtering or logging.
        /// </remarks>
        public static uint GetSender()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetSender();
        }

        /// <summary>
        /// Retrieves the target recipient of the current message being processed.
        /// </summary>
        /// <returns>The object ID of the message target. Returns OBJECT_INVALID if the message is broadcast to all eligible targets.</returns>
        /// <remarks>
        /// This method must be called from within a chat or system script handler.
        /// It returns the specific target of the message, or OBJECT_INVALID for broadcast messages.
        /// This is useful for implementing target-specific logic or filtering.
        /// </remarks>
        public static uint GetTarget()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetTarget();
        }

        /// <summary>
        /// Sets the hearing distance for chat messages on the specified channel for a specific listener.
        /// </summary>
        /// <param name="distance">The hearing distance in meters. Must be a positive value.</param>
        /// <param name="listener">The listener object to set the distance for. If null or OBJECT_INVALID, sets server-wide default.</param>
        /// <param name="channel">The chat channel to modify. Only applicable for PlayerTalk and PlayerWhisper channels.</param>
        /// <remarks>
        /// This controls how far away a player can hear talk and whisper messages.
        /// Per-player settings override server-wide settings.
        /// Only applies to PlayerTalk and PlayerWhisper channels; other channels are not affected.
        /// Setting a server-wide default affects all players who don't have individual settings.
        /// </remarks>
        public static void SetChatHearingDistance(
            float distance, 
            uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            uint listenerValue = listener ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.ChatPlugin.SetChatHearingDistance(distance, listenerValue, (int)channel);
        }

        /// <summary>
        /// Retrieves the hearing distance for chat messages on the specified channel for a specific listener.
        /// </summary>
        /// <param name="listener">The listener object to query. If null or OBJECT_INVALID, returns server-wide setting.</param>
        /// <param name="channel">The chat channel to query. Only applicable for PlayerTalk and PlayerWhisper channels.</param>
        /// <returns>The hearing distance in meters for the specified listener and channel.</returns>
        /// <remarks>
        /// This returns the current hearing distance setting for the specified listener and channel.
        /// If the listener has no individual setting, returns the server-wide default.
        /// Only applies to PlayerTalk and PlayerWhisper channels; other channels return 0.
        /// Use SetChatHearingDistance() to modify these values.
        /// </remarks>
        public static float GetChatHearingDistance(uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            uint listenerValue = listener ?? OBJECT_INVALID;
            return global::NWN.Core.NWNX.ChatPlugin.GetChatHearingDistance(listenerValue, (int)channel);
        }
    }
}