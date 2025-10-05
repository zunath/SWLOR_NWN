using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive feedback message management functionality for controlling
    /// the visibility and display of various game messages including feedback messages,
    /// combat log messages, and journal updates. This plugin allows for fine-grained
    /// control over what messages players see during gameplay.
    /// </summary>
    public class FeedbackPluginService : IFeedbackPluginService
    {
        /// <inheritdoc/>
        public bool GetFeedbackMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetFeedbackMessageHidden((int)messageType, playerValue);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetFeedbackMessageHidden(FeedbackMessageTypes messageType, bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.FeedbackPlugin.SetFeedbackMessageHidden((int)messageType, hide ? 1 : 0, playerValue);
        }

        /// <inheritdoc/>
        public bool GetCombatLogMessageHidden(CombatLogMessageType messageType, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetCombatLogMessageHidden((int)messageType, playerValue);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetCombatLogMessageHidden(CombatLogMessageType messageType, bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.FeedbackPlugin.SetCombatLogMessageHidden((int)messageType, hide ? 1 : 0, playerValue);
        }

        /// <inheritdoc/>
        public bool GetJournalUpdatedMessageHidden(uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetJournalUpdatedMessageHidden(playerValue);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetFeedbackMessageMode(bool whiteList)
        {
            global::NWN.Core.NWNX.FeedbackPlugin.SetFeedbackMessageMode(whiteList ? 1 : 0);
        }

        /// <inheritdoc/>
        public void SetCombatLogMessageMode(bool whiteList)
        {
            global::NWN.Core.NWNX.FeedbackPlugin.SetCombatLogMessageMode(whiteList ? 1 : 0);
        }
    }
}