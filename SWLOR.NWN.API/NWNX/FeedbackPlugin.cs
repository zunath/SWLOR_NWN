using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public static class FeedbackPlugin
    {
        /// <summary>
        /// Gets if feedback message is hidden.
        /// </summary>
        /// <param name="messageType">The message identifier from Feedback Messages.</param>
        /// <param name="player">The PC or OBJECT_INVALID for a global setting.</param>
        /// <returns>True if the message is hidden.</returns>
        public static bool GetFeedbackMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetFeedbackMessageHidden((int)messageType, playerValue);
            return result != 0;
        }

        /// <summary>
        /// Sets if feedback message is hidden.
        /// </summary>
        /// <param name="messageType">The message identifier.</param>
        /// <param name="hide">True to hide, false to show.</param>
        /// <param name="player">The PC or OBJECT_INVALID for a global setting.</param>
        /// <remarks>Personal state overrides the global state which means if a global state is set to true but the personal state is set to false, the message will be shown to the PC.</remarks>
        public static void SetFeedbackMessageHidden(FeedbackMessageTypes messageType, bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.FeedbackPlugin.SetFeedbackMessageHidden((int)messageType, hide ? 1 : 0, playerValue);
        }

        /// <summary>
        /// Gets if combat log message is hidden.
        /// </summary>
        /// <param name="messageType">The message identifier from Combat Log Messages.</param>
        /// <param name="player">The PC or OBJECT_INVALID for a global setting.</param>
        /// <returns>True if the message is hidden.</returns>
        public static bool GetCombatLogMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetCombatLogMessageHidden((int)messageType, playerValue);
            return result != 0;
        }

        /// <summary>
        /// Sets if combat log message is hidden.
        /// </summary>
        /// <param name="messageType">The message identifier.</param>
        /// <param name="hide">True to hide, false to show.</param>
        /// <param name="player">The PC or OBJECT_INVALID for a global setting.</param>
        /// <remarks>Personal state overrides the global state which means if a global state is set to true but the personal state is set to false, the message will be shown to the PC.</remarks>
        public static void SetCombatLogMessageHidden(CombatLogMessageType messageType, bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.FeedbackPlugin.SetCombatLogMessageHidden((int)messageType, hide ? 1 : 0, playerValue);
        }

        /// <summary>
        /// Gets if the journal update message is hidden.
        /// </summary>
        /// <param name="player">The PC or OBJECT_INVALID for a global setting.</param>
        /// <returns>True if the message is hidden.</returns>
        public static bool GetJournalUpdatedMessageHidden(uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetJournalUpdatedMessageHidden(playerValue);
            return result != 0;
        }

        /// <summary>
        /// Set whether to use a blacklist or whitelist mode for feedback messages.
        /// </summary>
        /// <param name="whiteList">True for all messages to be hidden by default, default false.</param>
        public static void SetFeedbackMessageMode(bool whiteList)
        {
            global::NWN.Core.NWNX.FeedbackPlugin.SetFeedbackMessageMode(whiteList ? 1 : 0);
        }

        /// <summary>
        /// Set whether to use a blacklist or whitelist mode for combat log messages.
        /// </summary>
        /// <param name="whiteList">True for all messages to be hidden by default, default false.</param>
        /// <remarks>If using Whitelist, be sure to whitelist NWNX_FEEDBACK_COMBATLOG_FEEDBACK for feedback messages to work.</remarks>
        public static void SetCombatLogMessageMode(bool whiteList)
        {
            global::NWN.Core.NWNX.FeedbackPlugin.SetCombatLogMessageMode(whiteList ? 1 : 0);
        }
    }
}