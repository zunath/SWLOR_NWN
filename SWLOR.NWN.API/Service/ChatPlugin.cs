using System;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class ChatPlugin
    {
        private static IChatPluginService _service = new ChatPluginService();

        internal static void SetService(IChatPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IChatPluginService.SendMessage"/>
        public static int SendMessage(ChatChannelType channel, string message, uint sender, uint target) => 
            _service.SendMessage(channel, message, sender, target);

        /// <inheritdoc cref="IChatPluginService.RegisterChatScript"/>
        public static void RegisterChatScript(string script) => _service.RegisterChatScript(script);

        /// <inheritdoc cref="IChatPluginService.SkipMessage"/>
        public static void SkipMessage() => _service.SkipMessage();

        /// <inheritdoc cref="IChatPluginService.GetChannel"/>
        public static ChatChannelType GetChannel() => _service.GetChannel();

        /// <inheritdoc cref="IChatPluginService.GetMessage"/>
        public static string GetMessage() => _service.GetMessage();

        /// <inheritdoc cref="IChatPluginService.GetSender"/>
        public static uint GetSender() => _service.GetSender();

        /// <inheritdoc cref="IChatPluginService.GetTarget"/>
        public static uint GetTarget() => _service.GetTarget();

        /// <inheritdoc cref="IChatPluginService.SetChatHearingDistance"/>
        public static void SetChatHearingDistance(float distance, uint? listener = null, ChatChannelType channel = ChatChannelType.PlayerTalk) => 
            _service.SetChatHearingDistance(distance, listener, channel);

        /// <inheritdoc cref="IChatPluginService.GetChatHearingDistance"/>
        public static float GetChatHearingDistance(uint? listener = null, ChatChannelType channel = ChatChannelType.PlayerTalk) => 
            _service.GetChatHearingDistance(listener, channel);
    }
}
