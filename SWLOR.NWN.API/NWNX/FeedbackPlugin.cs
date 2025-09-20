using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive feedback message management functionality for controlling
    /// the visibility and display of various game messages including feedback messages,
    /// combat log messages, and journal updates. This plugin allows for fine-grained
    /// control over what messages players see during gameplay.
    /// </summary>
    public static class FeedbackPlugin
    {
        /// <summary>
        /// Determines whether a specific feedback message is currently hidden for a player.
        /// </summary>
        /// <param name="messageType">The feedback message type to check. See FeedbackMessageTypes enum for available types.</param>
        /// <param name="player">The player object to check, or null/OBJECT_INVALID for global setting.</param>
        /// <returns>True if the message is hidden for the specified player, false if visible.</returns>
        /// <remarks>
        /// This function checks the visibility state of a specific feedback message type.
        /// If player is null or OBJECT_INVALID, it returns the global setting for all players.
        /// Personal settings override global settings when both are configured.
        /// </remarks>
        public static bool GetFeedbackMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetFeedbackMessageHidden((int)messageType, playerValue);
            return result != 0;
        }

        /// <summary>
        /// Sets the visibility state of a specific feedback message for a player.
        /// </summary>
        /// <param name="messageType">The feedback message type to modify. See FeedbackMessageTypes enum for available types.</param>
        /// <param name="hide">True to hide the message, false to show it.</param>
        /// <param name="player">The player object to modify, or null/OBJECT_INVALID for global setting.</param>
        /// <remarks>
        /// This function controls whether a specific feedback message is visible to a player.
        /// Personal settings always override global settings - if a global setting hides a message
        /// but the player's personal setting shows it, the message will be visible to that player.
        /// Changes take effect immediately and persist until modified again.
        /// </remarks>
        public static void SetFeedbackMessageHidden(FeedbackMessageTypes messageType, bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.FeedbackPlugin.SetFeedbackMessageHidden((int)messageType, hide ? 1 : 0, playerValue);
        }

        /// <summary>
        /// Determines whether a specific combat log message is currently hidden for a player.
        /// </summary>
        /// <param name="messageType">The combat log message type to check. See CombatLogMessageType enum for available types.</param>
        /// <param name="player">The player object to check, or null/OBJECT_INVALID for global setting.</param>
        /// <returns>True if the message is hidden for the specified player, false if visible.</returns>
        /// <remarks>
        /// This function checks the visibility state of a specific combat log message type.
        /// Combat log messages appear in the combat log window during gameplay.
        /// If player is null or OBJECT_INVALID, it returns the global setting for all players.
        /// Personal settings override global settings when both are configured.
        /// </remarks>
        public static bool GetCombatLogMessageHidden(CombatLogMessageType messageType, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetCombatLogMessageHidden((int)messageType, playerValue);
            return result != 0;
        }

        /// <summary>
        /// Sets the visibility state of a specific combat log message for a player.
        /// </summary>
        /// <param name="messageType">The combat log message type to modify. See FeedbackMessageTypes enum for available types.</param>
        /// <param name="hide">True to hide the message, false to show it.</param>
        /// <param name="player">The player object to modify, or null/OBJECT_INVALID for global setting.</param>
        /// <remarks>
        /// This function controls whether a specific combat log message is visible to a player.
        /// Combat log messages appear in the combat log window during gameplay.
        /// Personal settings always override global settings - if a global setting hides a message
        /// but the player's personal setting shows it, the message will be visible to that player.
        /// Changes take effect immediately and persist until modified again.
        /// </remarks>
        public static void SetCombatLogMessageHidden(CombatLogMessageType messageType, bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            global::NWN.Core.NWNX.FeedbackPlugin.SetCombatLogMessageHidden((int)messageType, hide ? 1 : 0, playerValue);
        }

        /// <summary>
        /// Determines whether the journal update notification message is currently hidden for a player.
        /// </summary>
        /// <param name="player">The player object to check, or null/OBJECT_INVALID for global setting.</param>
        /// <returns>True if the journal update message is hidden for the specified player, false if visible.</returns>
        /// <remarks>
        /// This function checks the visibility state of the journal update notification message.
        /// This message appears when journal entries are added or modified during gameplay.
        /// If player is null or OBJECT_INVALID, it returns the global setting for all players.
        /// Personal settings override global settings when both are configured.
        /// </remarks>
        public static bool GetJournalUpdatedMessageHidden(uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            var result = global::NWN.Core.NWNX.FeedbackPlugin.GetJournalUpdatedMessageHidden(playerValue);
            return result != 0;
        }

        /// <summary>
        /// Configures the filtering mode for feedback messages system-wide.
        /// </summary>
        /// <param name="whiteList">True to enable whitelist mode (hide all messages by default), false for blacklist mode (show all by default).</param>
        /// <remarks>
        /// This function sets the global filtering mode for all feedback messages.
        /// In whitelist mode, all feedback messages are hidden by default and must be explicitly enabled.
        /// In blacklist mode, all feedback messages are shown by default and must be explicitly hidden.
        /// This setting affects all players and overrides individual message visibility settings.
        /// Changes take effect immediately and persist until modified again.
        /// </remarks>
        public static void SetFeedbackMessageMode(bool whiteList)
        {
            global::NWN.Core.NWNX.FeedbackPlugin.SetFeedbackMessageMode(whiteList ? 1 : 0);
        }

        /// <summary>
        /// Configures the filtering mode for combat log messages system-wide.
        /// </summary>
        /// <param name="whiteList">True to enable whitelist mode (hide all messages by default), false for blacklist mode (show all by default).</param>
        /// <remarks>
        /// This function sets the global filtering mode for all combat log messages.
        /// In whitelist mode, all combat log messages are hidden by default and must be explicitly enabled.
        /// In blacklist mode, all combat log messages are shown by default and must be explicitly hidden.
        /// This setting affects all players and overrides individual message visibility settings.
        /// If using whitelist mode, ensure NWNX_FEEDBACK_COMBATLOG_FEEDBACK is whitelisted for feedback messages to work.
        /// Changes take effect immediately and persist until modified again.
        /// </remarks>
        public static void SetCombatLogMessageMode(bool whiteList)
        {
            global::NWN.Core.NWNX.FeedbackPlugin.SetCombatLogMessageMode(whiteList ? 1 : 0);
        }
    }
}