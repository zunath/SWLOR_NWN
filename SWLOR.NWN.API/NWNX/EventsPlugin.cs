namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive event system functionality for subscribing to, managing, and dispatching
    /// custom events throughout the game. This plugin enables script-to-script communication,
    /// event-driven programming patterns, and advanced event filtering and management capabilities.
    /// </summary>
    public static class EventsPlugin
    {
        /// <summary>
        /// Subscribes a script to receive notifications when a specific event occurs.
        /// </summary>
        /// <param name="evt">The name of the event to subscribe to. Can be custom events or NWNX system events.</param>
        /// <param name="script">The name of the script to execute when the event fires. Must be a valid script resource.</param>
        /// <remarks>
        /// This function registers a script to be called whenever the specified event occurs.
        /// Events can be dispatched by the NWNX plugin (see NWNX_EVENTS_EVENT_* constants) or by custom script code using SignalEvent().
        /// Multiple scripts can subscribe to the same event and will all be called when it fires.
        /// The script will receive the event target as OBJECT_SELF and can access event data using GetEventData().
        /// </remarks>
        public static void SubscribeEvent(string evt, string script)
        {
            global::NWN.Core.NWNX.EventsPlugin.SubscribeEvent(evt, script);
        }
        /// <summary>
        /// Removes a script's subscription to a specific event.
        /// </summary>
        /// <param name="evt">The name of the event to unsubscribe from. Must match the event name used in SubscribeEvent().</param>
        /// <param name="script">The name of the script to remove from the event subscription. Must match the script name used in SubscribeEvent().</param>
        /// <remarks>
        /// This function removes a previously registered script from receiving notifications for the specified event.
        /// The script will no longer be called when this event occurs.
        /// If the script was not subscribed to the event, this function has no effect.
        /// Other scripts subscribed to the same event will continue to receive notifications.
        /// </remarks>
        public static void UnsubscribeEvent(string evt, string script)
        {
            global::NWN.Core.NWNX.EventsPlugin.UnsubscribeEvent(evt, script);
        }

        /// <summary>
        /// Stores event data that can be accessed by event handler scripts.
        /// </summary>
        /// <param name="tag">The unique identifier for the event data. Used by GetEventData() to retrieve the data.</param>
        /// <param name="data">The data to store. Can be any string value including JSON, delimited data, or simple values.</param>
        /// <remarks>
        /// This function stores data that will be available to all scripts handling the next event.
        /// The data persists only for the duration of the next event dispatch and is automatically cleared afterward.
        /// This must be called BEFORE calling SignalEvent() for the data to be available to event handlers.
        /// Multiple data entries can be stored with different tags for the same event.
        /// </remarks>
        public static void PushEventData(string tag, string data)
        {
            global::NWN.Core.NWNX.EventsPlugin.PushEventData(tag, data);
        }

        /// <summary>
        /// Dispatches an event to all subscribed scripts and handlers.
        /// </summary>
        /// <param name="evt">The name of the event to dispatch. Must match an event that has active subscribers.</param>
        /// <param name="target">The target object for the event. This becomes OBJECT_SELF in all event handler scripts.</param>
        /// <returns>True if any scripts were subscribed to the event and were called, false if no subscribers exist.</returns>
        /// <remarks>
        /// This function triggers the event and calls all scripts that have subscribed to it.
        /// The target object becomes available as OBJECT_SELF in all event handler scripts.
        /// Event data pushed with PushEventData() will be available to all handlers via GetEventData().
        /// All subscribed scripts are called in the order they were subscribed.
        /// If no scripts are subscribed to the event, this function returns false and no scripts are executed.
        /// </remarks>
        public static bool SignalEvent(string evt, uint target)
        {
            var result = global::NWN.Core.NWNX.EventsPlugin.SignalEvent(evt, target);
            return result != 0;
        }

        /// <summary>
        /// Retrieves event data that was stored for the currently executing event handler.
        /// </summary>
        /// <param name="tag">The unique identifier for the event data. Must match a tag used in PushEventData().</param>
        /// <returns>The event data as a string, or empty string if no data exists for the specified tag.</returns>
        /// <remarks>
        /// This function retrieves data that was stored using PushEventData() before the event was dispatched.
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER SCRIPT.
        /// The data is only available during the execution of event handler scripts and is automatically cleared afterward.
        /// If the specified tag does not exist, an empty string is returned.
        /// </remarks>
        public static string GetEventData(string tag)
        {
            return global::NWN.Core.NWNX.EventsPlugin.GetEventData(tag);
        }

        /// <summary>
        /// Skips execution of the currently executing event.
        /// </summary>
        /// <remarks>
        /// If this is a NWNX event, that means that the base function call won't be called.
        /// This won't impact any other subscribers, nor dispatch for before / after functions.
        /// For example, if you are subscribing to NWNX_ON_EXAMINE_OBJECT_BEFORE, and you skip ...
        /// - The other subscribers will still be called.
        /// - The original function in the base game will be skipped.
        /// - The matching after event (NWNX_ON_EXAMINE_OBJECT_AFTER) will also be executed.
        /// 
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// ONLY WORKS WITH THE FOLLOWING EVENTS:
        /// - Feat events
        /// - Item events
        /// - Healer's Kit event
        /// - CombatMode events
        /// - Party events
        /// - Skill events
        /// - Map events
        /// - Listen/Spot Detection events
        /// - Polymorph events
        /// - DMAction events
        /// - Client connect event
        /// - Spell events
        /// - QuickChat events
        /// - Barter event (START only)
        /// - Trap events
        /// - Sticky Player Name event
        /// </remarks>
        public static void SkipEvent()
        {
            global::NWN.Core.NWNX.EventsPlugin.SkipEvent();
        }

        /// <summary>
        /// Set the return value of the event.
        /// </summary>
        /// <param name="data">The data to set as the event result.</param>
        /// <remarks>
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        /// ONLY WORKS WITH THE FOLLOWING EVENTS:
        /// - Healer's Kit event
        /// - Listen/Spot Detection events -> "1" or "0"
        /// - OnClientConnectBefore -> Reason for disconnect if skipped
        /// - Ammo Reload event -> Forced ammunition returned
        /// - Trap events -> "1" or "0"
        /// - Sticky Player Name event -> "1" or "0"
        /// </remarks>
        public static void SetEventResult(string data)
        {
            global::NWN.Core.NWNX.EventsPlugin.SetEventResult(data);
        }

        /// <summary>
        /// Returns the current event name.
        /// </summary>
        /// <returns>The current event name, or empty string on error.</returns>
        /// <remarks>THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.</remarks>
        public static string GetCurrentEvent()
        {
            return global::NWN.Core.NWNX.EventsPlugin.GetCurrentEvent();
        }

        /// <summary>
        /// Toggles DispatchListMode for sEvent+sScript.
        /// </summary>
        /// <param name="sEvent">The event name.</param>
        /// <param name="sScript">The script name.</param>
        /// <param name="bEnable">True to enable, false to disable.</param>
        /// <remarks>If enabled, sEvent for sScript will only be signalled if the target object is on its dispatch list.</remarks>
        public static void ToggleDispatchListMode(string sEvent, string sScript, bool bEnable)
        {
            global::NWN.Core.NWNX.EventsPlugin.ToggleDispatchListMode(sEvent, sScript, bEnable ? 1 : 0);
        }

        /// <summary>
        /// Add oObject to the dispatch list for sEvent+sScript.
        /// </summary>
        /// <param name="sEvent">The event name.</param>
        /// <param name="sScript">The script name.</param>
        /// <param name="oObject">The object to add to the dispatch list.</param>
        public static void AddObjectToDispatchList(string sEvent, string sScript, uint oObject)
        {
            global::NWN.Core.NWNX.EventsPlugin.AddObjectToDispatchList(sEvent, sScript, oObject);
        }

        /// <summary>
        /// Remove oObject from the dispatch list for sEvent+sScript.
        /// </summary>
        /// <param name="sEvent">The event name.</param>
        /// <param name="sScript">The script name.</param>
        /// <param name="oObject">The object to remove from the dispatch list.</param>
        public static void RemoveObjectFromDispatchList(string sEvent, string sScript, uint oObject)
        {
            global::NWN.Core.NWNX.EventsPlugin.RemoveObjectFromDispatchList(sEvent, sScript, oObject);
        }


        /// <summary>
        /// Toggle the whitelisting of IDs for sEvent.
        /// </summary>
        /// <param name="sEvent">The event name without _BEFORE / _AFTER.</param>
        /// <param name="bEnable">True to enable the whitelist, false to disable.</param>
        /// <remarks>If whitelisting is enabled, the event will only fire for IDs that are on its whitelist.</remarks>
        public static void ToggleIDWhitelist(string sEvent, bool bEnable)
        {
            global::NWN.Core.NWNX.EventsPlugin.ToggleIDWhitelist(sEvent, bEnable ? 1 : 0);
        }

        /// <summary>
        /// Add nID to the whitelist of sEvent.
        /// </summary>
        /// <param name="sEvent">The event name without _BEFORE / _AFTER.</param>
        /// <param name="nID">The ID.</param>
        /// <remarks>See ToggleIDWhitelist for valid events and ID types.</remarks>
        public static void AddIDToWhitelist(string sEvent, int nID)
        {
            global::NWN.Core.NWNX.EventsPlugin.AddIDToWhitelist(sEvent, nID);
        }

        /// <summary>
        /// Remove nID from the whitelist of sEvent.
        /// </summary>
        /// <param name="sEvent">The event name without _BEFORE / _AFTER.</param>
        /// <param name="nID">The ID.</param>
        /// <remarks>See ToggleIDWhitelist for valid events and ID types.</remarks>
        public static void RemoveIDFromWhitelist(string sEvent, int nID)
        {
            global::NWN.Core.NWNX.EventsPlugin.RemoveIDFromWhitelist(sEvent, nID);
        }
    }
}