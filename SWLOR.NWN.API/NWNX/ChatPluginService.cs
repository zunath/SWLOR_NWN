using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive chat system functionality including message sending, receiving, filtering,
    /// and custom chat channel management. This plugin allows for advanced chat system customization
    /// and monitoring of all chat communications.
    /// </summary>
    public class ChatPluginService : IChatPluginService
    {
        /// <inheritdoc/>
        public int SendMessage(ChatChannelType channel, string message, uint sender, uint target)
        {
            return global::NWN.Core.NWNX.ChatPlugin.SendMessage((int)channel, message, sender, target);
        }

        /// <inheritdoc/>
        public void RegisterChatScript(string script)
        {
            global::NWN.Core.NWNX.ChatPlugin.RegisterChatScript(script);
        }

        /// <inheritdoc/>
        public void SkipMessage()
        {
            global::NWN.Core.NWNX.ChatPlugin.SkipMessage();
        }

        /// <inheritdoc/>
        public ChatChannelType GetChannel()
        {
            int result = global::NWN.Core.NWNX.ChatPlugin.GetChannel();
            return (ChatChannelType)result;
        }

        /// <inheritdoc/>
        public string GetMessage()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetMessage();
        }

        /// <inheritdoc/>
        public uint GetSender()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetSender();
        }

        /// <inheritdoc/>
        public uint GetTarget()
        {
            return global::NWN.Core.NWNX.ChatPlugin.GetTarget();
        }

        /// <inheritdoc/>
        public void SetChatHearingDistance(
            float distance, 
            uint? listener = null,
            ChatChannelType channel = ChatChannelType.PlayerTalk)
        {
            uint listenerValue = listener ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.ChatPlugin.SetChatHearingDistance(distance, listenerValue, (int)channel);
        }

        /// <inheritdoc/>
        public float GetChatHearingDistance(uint? listener = null,
            ChatChannelType channel = ChatChannelType.PlayerTalk)
        {
            uint listenerValue = listener ?? OBJECT_INVALID;
            return global::NWN.Core.NWNX.ChatPlugin.GetChatHearingDistance(listenerValue, (int)channel);
        }
    }
}