using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the EventsPlugin for testing purposes.
    /// Provides comprehensive event system functionality for subscribing to, managing, and dispatching
    /// custom events throughout the game.
    /// </summary>
    public class EventsPluginMock: IEventsPluginService
    {
        // Mock data storage
        private readonly Dictionary<string, List<string>> _eventSubscriptions = new();
        private readonly Dictionary<string, string> _eventData = new();
        private readonly List<EventDispatch> _eventHistory = new();
        private string _currentEvent = "";
        private uint _currentEventTarget = OBJECT_INVALID;

        /// <summary>
        /// Subscribes a script to receive notifications when a specific event occurs.
        /// </summary>
        /// <param name="evt">The name of the event to subscribe to. Can be custom events or NWNX system events.</param>
        /// <param name="script">The name of the script to execute when the event fires. Must be a valid script resource.</param>
        public void SubscribeEvent(string evt, string script)
        {
            if (string.IsNullOrEmpty(evt) || string.IsNullOrEmpty(script))
                return;

            if (!_eventSubscriptions.ContainsKey(evt))
            {
                _eventSubscriptions[evt] = new List<string>();
            }

            if (!_eventSubscriptions[evt].Contains(script))
            {
                _eventSubscriptions[evt].Add(script);
            }
        }

        /// <summary>
        /// Removes a script's subscription to a specific event.
        /// </summary>
        /// <param name="evt">The name of the event to unsubscribe from. Must match the event name used in SubscribeEvent().</param>
        /// <param name="script">The name of the script to remove from the event subscription. Must match the script name used in SubscribeEvent().</param>
        public void UnsubscribeEvent(string evt, string script)
        {
            if (string.IsNullOrEmpty(evt) || string.IsNullOrEmpty(script))
                return;

            if (_eventSubscriptions.TryGetValue(evt, out var scripts))
            {
                scripts.Remove(script);
                
                // Remove the event entry if no more scripts are subscribed
                if (scripts.Count == 0)
                {
                    _eventSubscriptions.Remove(evt);
                }
            }
        }

        /// <summary>
        /// Stores event data that can be accessed by event handler scripts.
        /// </summary>
        /// <param name="tag">The unique identifier for the event data. Used by GetEventData() to retrieve the data.</param>
        /// <param name="data">The data to store. Can be any string value including JSON, delimited data, or simple values.</param>
        public void PushEventData(string tag, string data)
        {
            if (string.IsNullOrEmpty(tag))
                return;

            _eventData[tag] = data ?? "";
        }

        /// <summary>
        /// Retrieves event data that was stored using SetEventData().
        /// </summary>
        /// <param name="tag">The unique identifier for the event data. Must match the tag used in SetEventData().</param>
        /// <returns>The stored event data as a string. Returns empty string if no data was found for the tag.</returns>
        public string GetEventData(string tag)
        {
            return _eventData.TryGetValue(tag ?? "", out var data) ? data : "";
        }

        /// <summary>
        /// Dispatches an event to all subscribed scripts.
        /// </summary>
        /// <param name="evt">The name of the event to dispatch. Must match an event that has been subscribed to.</param>
        /// <param name="target">The object that will be set as OBJECT_SELF for the event handler scripts.</param>
        /// <returns>True if any scripts were subscribed to the event and were called, false if no subscribers exist.</returns>
        public bool SignalEvent(string evt, uint target)
        {
            if (string.IsNullOrEmpty(evt))
                return false;

            var dispatch = new EventDispatch
            {
                EventName = evt,
                Target = target,
                Timestamp = DateTime.UtcNow,
                Data = new Dictionary<string, string>(_eventData)
            };

            _eventHistory.Add(dispatch);
            _currentEvent = evt;
            _currentEventTarget = target;

            // Simulate calling subscribed scripts
            bool hasSubscribers = false;
            if (_eventSubscriptions.TryGetValue(evt, out var scripts))
            {
                hasSubscribers = scripts.Count > 0;
                foreach (var script in scripts)
                {
                    // Mock implementation - in real tests, this would call the actual script
                    SimulateScriptExecution(script, target);
                }
            }

            // Clear event data after dispatch
            _eventData.Clear();
            return hasSubscribers;
        }

        /// <summary>
        /// Pushes an event to the event queue for later processing.
        /// </summary>
        /// <param name="evt">The name of the event to queue.</param>
        /// <param name="target">The target object for the event.</param>
        /// <param name="delay">The delay in milliseconds before the event is processed.</param>
        public void PushEvent(string evt, uint target, int delay = 0)
        {
            if (string.IsNullOrEmpty(evt))
                return;

            // Mock implementation - in real tests, this would queue the event
            // For now, we'll just dispatch it immediately
            SignalEvent(evt, target);
        }

        /// <summary>
        /// Gets the current event being processed.
        /// </summary>
        /// <returns>The name of the current event, or empty string if no event is being processed.</returns>
        public string GetCurrentEvent()
        {
            return _currentEvent;
        }

        /// <summary>
        /// Gets the current event target.
        /// </summary>
        /// <returns>The object ID of the current event target, or OBJECT_INVALID if no event is being processed.</returns>
        public uint GetCurrentEventTarget()
        {
            return _currentEventTarget;
        }

        /// <summary>
        /// Skips execution of the currently executing event.
        /// </summary>
        public void SkipEvent()
        {
            // Mock implementation - in real tests, this would skip the event
        }

        /// <summary>
        /// Set the return value of the event.
        /// </summary>
        /// <param name="data">The data to set as the event result.</param>
        public void SetEventResult(string data)
        {
            // Mock implementation - in real tests, this would set the event result
        }

        /// <summary>
        /// Toggles DispatchListMode for sEvent+sScript.
        /// </summary>
        /// <param name="sEvent">The event name.</param>
        /// <param name="sScript">The script name.</param>
        /// <param name="bEnable">True to enable, false to disable.</param>
        public void ToggleDispatchListMode(string sEvent, string sScript, bool bEnable)
        {
            // Mock implementation - in real tests, this would toggle dispatch list mode
        }

        /// <summary>
        /// Add oObject to the dispatch list for sEvent+sScript.
        /// </summary>
        /// <param name="sEvent">The event name.</param>
        /// <param name="sScript">The script name.</param>
        /// <param name="oObject">The object to add to the dispatch list.</param>
        public void AddObjectToDispatchList(string sEvent, string sScript, uint oObject)
        {
            // Mock implementation - in real tests, this would add object to dispatch list
        }

        /// <summary>
        /// Remove oObject from the dispatch list for sEvent+sScript.
        /// </summary>
        /// <param name="sEvent">The event name.</param>
        /// <param name="sScript">The script name.</param>
        /// <param name="oObject">The object to remove from the dispatch list.</param>
        public void RemoveObjectFromDispatchList(string sEvent, string sScript, uint oObject)
        {
            // Mock implementation - in real tests, this would remove object from dispatch list
        }

        /// <summary>
        /// Toggle the whitelisting of IDs for sEvent.
        /// </summary>
        /// <param name="sEvent">The event name without _BEFORE / _AFTER.</param>
        /// <param name="bEnable">True to enable the whitelist, false to disable.</param>
        public void ToggleIDWhitelist(string sEvent, bool bEnable)
        {
            // Mock implementation - in real tests, this would toggle ID whitelist
        }

        /// <summary>
        /// Add nID to the whitelist of sEvent.
        /// </summary>
        /// <param name="sEvent">The event name without _BEFORE / _AFTER.</param>
        /// <param name="nID">The ID.</param>
        public void AddIDToWhitelist(string sEvent, int nID)
        {
            // Mock implementation - in real tests, this would add ID to whitelist
        }

        /// <summary>
        /// Remove nID from the whitelist of sEvent.
        /// </summary>
        /// <param name="sEvent">The event name without _BEFORE / _AFTER.</param>
        /// <param name="nID">The ID.</param>
        public void RemoveIDFromWhitelist(string sEvent, int nID)
        {
            // Mock implementation - in real tests, this would remove ID from whitelist
        }

        /// <summary>
        /// Clears all event data.
        /// </summary>
        public void ClearEventData()
        {
            _eventData.Clear();
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _eventSubscriptions.Clear();
            _eventData.Clear();
            _eventHistory.Clear();
            _currentEvent = "";
            _currentEventTarget = OBJECT_INVALID;
        }

        /// <summary>
        /// Gets the event history for testing verification.
        /// </summary>
        /// <returns>A list of all events that have been dispatched.</returns>
        public List<EventDispatch> GetEventHistory()
        {
            return new List<EventDispatch>(_eventHistory);
        }

        /// <summary>
        /// Gets all event subscriptions for testing verification.
        /// </summary>
        /// <returns>A dictionary of event names to their subscribed scripts.</returns>
        public Dictionary<string, List<string>> GetEventSubscriptions()
        {
            return new Dictionary<string, List<string>>(_eventSubscriptions);
        }

        /// <summary>
        /// Gets the current event data for testing verification.
        /// </summary>
        /// <returns>A dictionary of event data tags to their values.</returns>
        public Dictionary<string, string> GetCurrentEventData()
        {
            return new Dictionary<string, string>(_eventData);
        }

        /// <summary>
        /// Simulates script execution for testing purposes.
        /// </summary>
        /// <param name="scriptName">The name of the script to execute.</param>
        /// <param name="target">The target object for the script.</param>
        private void SimulateScriptExecution(string scriptName, uint target)
        {
            // Mock implementation - in real tests, this would call the actual script
            // For now, we just log that the script would be called
        }

        // Constants
        private const uint OBJECT_INVALID = 0x7F000000;

        // Helper classes
        public class EventDispatch
        {
            public string EventName { get; set; } = "";
            public uint Target { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, string> Data { get; set; } = new();
        }
    }
}
