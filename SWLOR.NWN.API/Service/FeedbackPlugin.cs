using System;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class FeedbackPlugin
    {
        private static IFeedbackPluginService _service = new FeedbackPluginService();

        internal static void SetService(IFeedbackPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IFeedbackPluginService.GetFeedbackMessageHidden"/>
        public static bool GetFeedbackMessageHidden(FeedbackMessageTypes messageType, uint? player = null) => 
            _service.GetFeedbackMessageHidden(messageType, player);

        /// <inheritdoc cref="IFeedbackPluginService.SetFeedbackMessageHidden"/>
        public static void SetFeedbackMessageHidden(FeedbackMessageTypes messageType, bool hide, uint? player = null) => 
            _service.SetFeedbackMessageHidden(messageType, hide, player);

        /// <inheritdoc cref="IFeedbackPluginService.GetCombatLogMessageHidden"/>
        public static bool GetCombatLogMessageHidden(CombatLogMessageType messageType, uint? player = null) => 
            _service.GetCombatLogMessageHidden(messageType, player);

        /// <inheritdoc cref="IFeedbackPluginService.SetCombatLogMessageHidden"/>
        public static void SetCombatLogMessageHidden(CombatLogMessageType messageType, bool hide, uint? player = null) => 
            _service.SetCombatLogMessageHidden(messageType, hide, player);

        /// <inheritdoc cref="IFeedbackPluginService.GetJournalUpdatedMessageHidden"/>
        public static bool GetJournalUpdatedMessageHidden(uint? player = null) => 
            _service.GetJournalUpdatedMessageHidden(player);

        /// <inheritdoc cref="IFeedbackPluginService.SetFeedbackMessageMode"/>
        public static void SetFeedbackMessageMode(bool whiteList) => _service.SetFeedbackMessageMode(whiteList);

        /// <inheritdoc cref="IFeedbackPluginService.SetCombatLogMessageMode"/>
        public static void SetCombatLogMessageMode(bool whiteList) => _service.SetCombatLogMessageMode(whiteList);
    }
}
