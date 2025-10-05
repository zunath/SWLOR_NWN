using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the ChatPlugin for testing purposes.
    /// Provides comprehensive chat system functionality including message sending, receiving, filtering,
    /// and custom chat channel management.
    /// </summary>
    public class ChatPluginMock: IChatPluginService
    {
        // Mock data storage
        private readonly List<ChatMessage> _messageHistory = new();
        private string _registeredChatScript = "";
        private bool _skipNextMessage = false;
        private ChatMessage? _currentMessage = null;
        private readonly Dictionary<uint, Dictionary<ChatChannelType, float>> _hearingDistances = new();
        private float _defaultHearingDistance = 20.0f;

        /// <summary>
        /// Sends a chat message through the specified channel to the target recipient.
        /// </summary>
        /// <param name="channel">The chat channel to send the message through. See ChatChannel enum for available channels.</param>
        /// <param name="message">The message content to send. Cannot be null or empty.</param>
        /// <param name="sender">The object ID of the message sender. Must be a valid object.</param>
        /// <param name="target">The object ID of the message recipient. Use OBJECT_INVALID to broadcast to all eligible targets.</param>
        /// <returns>1 if the message was sent successfully, 0 if it failed.</returns>
        public int SendMessage(ChatChannelType channel, string message, uint sender, uint target)
        {
            if (string.IsNullOrEmpty(message))
                return 0;

            var chatMessage = new ChatMessage
            {
                Channel = channel,
                Message = message,
                Sender = sender,
                Target = target,
                Timestamp = DateTime.UtcNow
            };

            _messageHistory.Add(chatMessage);

            // If there's a registered chat script, simulate processing
            if (!string.IsNullOrEmpty(_registeredChatScript))
            {
                _currentMessage = chatMessage;
                _skipNextMessage = false;
                
                // Simulate script execution - in real tests, this would call the actual script
                ProcessChatMessage(chatMessage);
            }

            return 1;
        }

        /// <summary>
        /// Registers a script to handle all incoming chat messages and events.
        /// </summary>
        /// <param name="script">The name of the script to register for handling chat events. Must be a valid script name.</param>
        public void RegisterChatScript(string script)
        {
            _registeredChatScript = script ?? "";
        }

        /// <summary>
        /// Prevents the current chat message from being processed and displayed.
        /// </summary>
        public void SkipMessage()
        {
            _skipNextMessage = true;
        }

        /// <summary>
        /// Retrieves the chat channel of the current message being processed.
        /// </summary>
        /// <returns>The chat channel of the current message. See ChatChannel enum for possible values.</returns>
        public ChatChannelType GetChannel()
        {
            return _currentMessage?.Channel ?? ChatChannelType.PlayerTalk;
        }

        /// <summary>
        /// Retrieves the content of the current message being processed.
        /// </summary>
        /// <returns>The message content as a string. Returns empty string if no message is being processed.</returns>
        public string GetMessage()
        {
            return _currentMessage?.Message ?? "";
        }

        /// <summary>
        /// Retrieves the sender of the current message being processed.
        /// </summary>
        /// <returns>The object ID of the message sender. Returns OBJECT_INVALID if no sender is available.</returns>
        public uint GetSender()
        {
            return _currentMessage?.Sender ?? OBJECT_INVALID;
        }

        /// <summary>
        /// Retrieves the target recipient of the current message being processed.
        /// </summary>
        /// <returns>The object ID of the message target. Returns OBJECT_INVALID if the message is broadcast to all eligible targets.</returns>
        public uint GetTarget()
        {
            return _currentMessage?.Target ?? OBJECT_INVALID;
        }

        /// <summary>
        /// Sets the hearing distance for chat messages on the specified channel for a specific listener.
        /// </summary>
        /// <param name="distance">The hearing distance in meters. Must be a positive value.</param>
        /// <param name="listener">The listener object to set the distance for. If null or OBJECT_INVALID, sets server-wide default.</param>
        /// <param name="channel">The chat channel to modify. Only applicable for PlayerTalk and PlayerWhisper channels.</param>
        public void SetChatHearingDistance(
            float distance, 
            uint? listener = null,
            ChatChannelType channel = ChatChannelType.PlayerTalk)
        {
            if (listener == null || listener == OBJECT_INVALID)
            {
                _defaultHearingDistance = Math.Max(0, distance);
            }
            else
            {
                if (!_hearingDistances.ContainsKey(listener.Value))
                {
                    _hearingDistances[listener.Value] = new Dictionary<ChatChannelType, float>();
                }
                
                _hearingDistances[listener.Value][channel] = Math.Max(0, distance);
            }
        }

        /// <summary>
        /// Retrieves the hearing distance for chat messages on the specified channel for a specific listener.
        /// </summary>
        /// <param name="listener">The listener object to query. If null or OBJECT_INVALID, returns server-wide setting.</param>
        /// <param name="channel">The chat channel to query. Only applicable for PlayerTalk and PlayerWhisper channels.</param>
        /// <returns>The hearing distance in meters for the specified listener and channel.</returns>
        public float GetChatHearingDistance(uint? listener = null,
            ChatChannelType channel = ChatChannelType.PlayerTalk)
        {
            if (listener == null || listener == OBJECT_INVALID)
            {
                return _defaultHearingDistance;
            }

            if (_hearingDistances.TryGetValue(listener.Value, out var distances) &&
                distances.TryGetValue(channel, out var distance))
            {
                return distance;
            }

            return _defaultHearingDistance;
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _messageHistory.Clear();
            _registeredChatScript = "";
            _skipNextMessage = false;
            _currentMessage = null;
            _hearingDistances.Clear();
            _defaultHearingDistance = 20.0f;
        }

        /// <summary>
        /// Gets the message history for testing verification.
        /// </summary>
        /// <returns>A list of all chat messages sent through the mock.</returns>
        public List<ChatMessage> GetMessageHistory()
        {
            return new List<ChatMessage>(_messageHistory);
        }

        /// <summary>
        /// Gets the registered chat script for testing verification.
        /// </summary>
        /// <returns>The name of the registered chat script.</returns>
        public string GetRegisteredChatScript()
        {
            return _registeredChatScript;
        }

        /// <summary>
        /// Gets whether the next message should be skipped for testing verification.
        /// </summary>
        /// <returns>True if the next message should be skipped, false otherwise.</returns>
        public bool GetSkipNextMessage()
        {
            return _skipNextMessage;
        }

        /// <summary>
        /// Gets the current message being processed for testing verification.
        /// </summary>
        /// <returns>The current message being processed, or null if none.</returns>
        public ChatMessage? GetCurrentMessage()
        {
            return _currentMessage;
        }

        /// <summary>
        /// Simulates processing a chat message through the registered script.
        /// </summary>
        /// <param name="message">The message to process.</param>
        private void ProcessChatMessage(ChatMessage message)
        {
            // Mock implementation - in real tests, this would call the actual script
            // For now, we just simulate the processing without actually calling a script
        }

        // Constants
        private const uint OBJECT_INVALID = 0x7F000000;

        // Helper classes
        public class ChatMessage
        {
            public ChatChannelType Channel { get; set; }
            public string Message { get; set; } = "";
            public uint Sender { get; set; }
            public uint Target { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
