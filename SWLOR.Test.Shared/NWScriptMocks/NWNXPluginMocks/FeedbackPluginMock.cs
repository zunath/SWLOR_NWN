using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the FeedbackPlugin for testing purposes.
    /// Provides comprehensive feedback message management functionality for controlling
    /// the visibility and display of various game messages including feedback messages,
    /// combat log messages, and journal updates.
    /// </summary>
    public class FeedbackPluginMock: IFeedbackPluginService
    {
        // Mock data storage
        private readonly Dictionary<FeedbackMessageTypes, bool> _globalFeedbackMessages = new();
        private readonly Dictionary<uint, Dictionary<FeedbackMessageTypes, bool>> _playerFeedbackMessages = new();
        private readonly Dictionary<CombatLogMessageType, bool> _globalCombatLogMessages = new();
        private readonly Dictionary<uint, Dictionary<CombatLogMessageType, bool>> _playerCombatLogMessages = new();
        private bool _globalJournalUpdatedMessage = false;
        private readonly Dictionary<uint, bool> _playerJournalUpdatedMessage = new();

        /// <summary>
        /// Determines whether a specific feedback message is currently hidden for a player.
        /// </summary>
        /// <param name="messageType">The feedback message type to check. See FeedbackMessageTypes enum for available types.</param>
        /// <param name="player">The player object to check, or null/OBJECT_INVALID for global setting.</param>
        /// <returns>True if the message is hidden for the specified player, false if visible.</returns>
        public bool GetFeedbackMessageHidden(FeedbackMessageTypes messageType, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            
            // Check player-specific setting first
            if (playerValue != OBJECT_INVALID && _playerFeedbackMessages.TryGetValue(playerValue, out var playerMessages) &&
                playerMessages.TryGetValue(messageType, out var playerHidden))
            {
                return playerHidden;
            }
            
            // Fall back to global setting
            return _globalFeedbackMessages.TryGetValue(messageType, out var globalHidden) && globalHidden;
        }

        /// <summary>
        /// Sets the visibility state of a specific feedback message for a player.
        /// </summary>
        /// <param name="messageType">The feedback message type to modify. See FeedbackMessageTypes enum for available types.</param>
        /// <param name="hide">True to hide the message, false to show it.</param>
        /// <param name="player">The player object to modify, or null/OBJECT_INVALID for global setting.</param>
        public void SetFeedbackMessageHidden(FeedbackMessageTypes messageType, bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            
            if (playerValue == OBJECT_INVALID)
            {
                _globalFeedbackMessages[messageType] = hide;
            }
            else
            {
                if (!_playerFeedbackMessages.ContainsKey(playerValue))
                {
                    _playerFeedbackMessages[playerValue] = new Dictionary<FeedbackMessageTypes, bool>();
                }
                _playerFeedbackMessages[playerValue][messageType] = hide;
            }
        }

        /// <summary>
        /// Determines whether a specific combat log message is currently hidden for a player.
        /// </summary>
        /// <param name="messageType">The combat log message type to check. See CombatLogMessageType enum for available types.</param>
        /// <param name="player">The player object to check, or null/OBJECT_INVALID for global setting.</param>
        /// <returns>True if the message is hidden for the specified player, false if visible.</returns>
        public bool GetCombatLogMessageHidden(CombatLogMessageType messageType, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            
            // Check player-specific setting first
            if (playerValue != OBJECT_INVALID && _playerCombatLogMessages.TryGetValue(playerValue, out var playerMessages) &&
                playerMessages.TryGetValue(messageType, out var playerHidden))
            {
                return playerHidden;
            }
            
            // Fall back to global setting
            return _globalCombatLogMessages.TryGetValue(messageType, out var globalHidden) && globalHidden;
        }

        /// <summary>
        /// Sets the visibility state of a specific combat log message for a player.
        /// </summary>
        /// <param name="messageType">The combat log message type to modify. See CombatLogMessageType enum for available types.</param>
        /// <param name="hide">True to hide the message, false to show it.</param>
        /// <param name="player">The player object to modify, or null/OBJECT_INVALID for global setting.</param>
        public void SetCombatLogMessageHidden(CombatLogMessageType messageType, bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            
            if (playerValue == OBJECT_INVALID)
            {
                _globalCombatLogMessages[messageType] = hide;
            }
            else
            {
                if (!_playerCombatLogMessages.ContainsKey(playerValue))
                {
                    _playerCombatLogMessages[playerValue] = new Dictionary<CombatLogMessageType, bool>();
                }
                _playerCombatLogMessages[playerValue][messageType] = hide;
            }
        }

        /// <summary>
        /// Determines whether the journal update notification message is currently hidden for a player.
        /// </summary>
        /// <param name="player">The player object to check, or null/OBJECT_INVALID for global setting.</param>
        /// <returns>True if the journal update message is hidden for the specified player, false if visible.</returns>
        public bool GetJournalUpdatedMessageHidden(uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            
            // Check player-specific setting first
            if (playerValue != OBJECT_INVALID && _playerJournalUpdatedMessage.TryGetValue(playerValue, out var playerHidden))
            {
                return playerHidden;
            }
            
            // Fall back to global setting
            return _globalJournalUpdatedMessage;
        }

        /// <summary>
        /// Sets the visibility state of the journal update notification message for a player.
        /// </summary>
        /// <param name="hide">True to hide the message, false to show it.</param>
        /// <param name="player">The player object to modify, or null/OBJECT_INVALID for global setting.</param>
        public void SetJournalUpdatedMessageHidden(bool hide, uint? player = null)
        {
            uint playerValue = player ?? OBJECT_INVALID;
            
            if (playerValue == OBJECT_INVALID)
            {
                _globalJournalUpdatedMessage = hide;
            }
            else
            {
                _playerJournalUpdatedMessage[playerValue] = hide;
            }
        }

        /// <summary>
        /// Configures the filtering mode for feedback messages system-wide.
        /// </summary>
        /// <param name="whiteList">True to enable whitelist mode (hide all messages by default), false for blacklist mode (show all by default).</param>
        public void SetFeedbackMessageMode(bool whiteList)
        {
            // Mock implementation - in real tests, this would set the global filtering mode
        }

        /// <summary>
        /// Configures the filtering mode for combat log messages system-wide.
        /// </summary>
        /// <param name="whiteList">True to enable whitelist mode (hide all messages by default), false for blacklist mode (show all by default).</param>
        public void SetCombatLogMessageMode(bool whiteList)
        {
            // Mock implementation - in real tests, this would set the global filtering mode
        }

        /// <summary>
        /// Gets all feedback message settings for a specific player.
        /// </summary>
        /// <param name="player">The player object to query.</param>
        /// <returns>A dictionary of feedback message types to their visibility state.</returns>
        public Dictionary<FeedbackMessageTypes, bool> GetPlayerFeedbackMessages(uint player)
        {
            var result = new Dictionary<FeedbackMessageTypes, bool>();
            
            // Add global settings
            foreach (var kvp in _globalFeedbackMessages)
            {
                result[kvp.Key] = kvp.Value;
            }
            
            // Override with player-specific settings
            if (_playerFeedbackMessages.TryGetValue(player, out var playerMessages))
            {
                foreach (var kvp in playerMessages)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets all combat log message settings for a specific player.
        /// </summary>
        /// <param name="player">The player object to query.</param>
        /// <returns>A dictionary of combat log message types to their visibility state.</returns>
        public Dictionary<CombatLogMessageType, bool> GetPlayerCombatLogMessages(uint player)
        {
            var result = new Dictionary<CombatLogMessageType, bool>();
            
            // Add global settings
            foreach (var kvp in _globalCombatLogMessages)
            {
                result[kvp.Key] = kvp.Value;
            }
            
            // Override with player-specific settings
            if (_playerCombatLogMessages.TryGetValue(player, out var playerMessages))
            {
                foreach (var kvp in playerMessages)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets all global feedback message settings.
        /// </summary>
        /// <returns>A dictionary of feedback message types to their visibility state.</returns>
        public Dictionary<FeedbackMessageTypes, bool> GetGlobalFeedbackMessages()
        {
            return new Dictionary<FeedbackMessageTypes, bool>(_globalFeedbackMessages);
        }

        /// <summary>
        /// Gets all global combat log message settings.
        /// </summary>
        /// <returns>A dictionary of combat log message types to their visibility state.</returns>
        public Dictionary<CombatLogMessageType, bool> GetGlobalCombatLogMessages()
        {
            return new Dictionary<CombatLogMessageType, bool>(_globalCombatLogMessages);
        }

        /// <summary>
        /// Gets the global journal updated message setting.
        /// </summary>
        /// <returns>True if the journal updated message is hidden globally, false if visible.</returns>
        public bool GetGlobalJournalUpdatedMessageHidden()
        {
            return _globalJournalUpdatedMessage;
        }

        /// <summary>
        /// Clears all feedback message settings for a specific player.
        /// </summary>
        /// <param name="player">The player object to clear settings for.</param>
        public void ClearPlayerFeedbackMessages(uint player)
        {
            _playerFeedbackMessages.Remove(player);
            _playerCombatLogMessages.Remove(player);
            _playerJournalUpdatedMessage.Remove(player);
        }

        /// <summary>
        /// Clears all global feedback message settings.
        /// </summary>
        public void ClearGlobalFeedbackMessages()
        {
            _globalFeedbackMessages.Clear();
            _globalCombatLogMessages.Clear();
            _globalJournalUpdatedMessage = false;
        }

        /// <summary>
        /// Gets the count of feedback message types that are hidden for a specific player.
        /// </summary>
        /// <param name="player">The player object to query.</param>
        /// <returns>The number of hidden feedback message types.</returns>
        public int GetHiddenFeedbackMessageCount(uint player)
        {
            var messages = GetPlayerFeedbackMessages(player);
            return messages.Values.Count(hidden => hidden);
        }

        /// <summary>
        /// Gets the count of combat log message types that are hidden for a specific player.
        /// </summary>
        /// <param name="player">The player object to query.</param>
        /// <returns>The number of hidden combat log message types.</returns>
        public int GetHiddenCombatLogMessageCount(uint player)
        {
            var messages = GetPlayerCombatLogMessages(player);
            return messages.Values.Count(hidden => hidden);
        }

        /// <summary>
        /// Gets the count of players with custom feedback message settings.
        /// </summary>
        /// <returns>The number of players with custom settings.</returns>
        public int GetPlayerCount()
        {
            return _playerFeedbackMessages.Count;
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _globalFeedbackMessages.Clear();
            _playerFeedbackMessages.Clear();
            _globalCombatLogMessages.Clear();
            _playerCombatLogMessages.Clear();
            _globalJournalUpdatedMessage = false;
            _playerJournalUpdatedMessage.Clear();
        }

        /// <summary>
        /// Gets all feedback message data for testing verification.
        /// </summary>
        /// <returns>A FeedbackMessageData object containing all settings.</returns>
        public FeedbackMessageData GetFeedbackMessageDataForTesting()
        {
            return new FeedbackMessageData
            {
                GlobalFeedbackMessages = new Dictionary<FeedbackMessageTypes, bool>(_globalFeedbackMessages),
                PlayerFeedbackMessages = new Dictionary<uint, Dictionary<FeedbackMessageTypes, bool>>(_playerFeedbackMessages),
                GlobalCombatLogMessages = new Dictionary<CombatLogMessageType, bool>(_globalCombatLogMessages),
                PlayerCombatLogMessages = new Dictionary<uint, Dictionary<CombatLogMessageType, bool>>(_playerCombatLogMessages),
                GlobalJournalUpdatedMessage = _globalJournalUpdatedMessage,
                PlayerJournalUpdatedMessage = new Dictionary<uint, bool>(_playerJournalUpdatedMessage)
            };
        }

        // Constants
        private const uint OBJECT_INVALID = 0x7F000000;

        // Helper classes
        public class FeedbackMessageData
        {
            public Dictionary<FeedbackMessageTypes, bool> GlobalFeedbackMessages { get; set; } = new();
            public Dictionary<uint, Dictionary<FeedbackMessageTypes, bool>> PlayerFeedbackMessages { get; set; } = new();
            public Dictionary<CombatLogMessageType, bool> GlobalCombatLogMessages { get; set; } = new();
            public Dictionary<uint, Dictionary<CombatLogMessageType, bool>> PlayerCombatLogMessages { get; set; } = new();
            public bool GlobalJournalUpdatedMessage { get; set; }
            public Dictionary<uint, bool> PlayerJournalUpdatedMessage { get; set; } = new();
        }
    }
}
