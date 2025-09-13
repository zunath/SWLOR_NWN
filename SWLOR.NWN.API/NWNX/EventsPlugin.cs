namespace SWLOR.NWN.API.NWNX
{
    public static class EventsPlugin
    {
        /// <summary>
        /// Scripts can subscribe to events.
        /// </summary>
        /// <param name="evt">The event name.</param>
        /// <param name="script">The script to call when the event fires.</param>
        /// <remarks>Some events are dispatched via the NWNX plugin (see NWNX_EVENTS_EVENT_* constants). Others can be signalled via script code via SignalEvent().</remarks>
        public static void SubscribeEvent(string evt, string script)
        {
            global::NWN.Core.NWNX.EventsPlugin.SubscribeEvent(evt, script);
        }
        /// <summary>
        /// Unsubscribe a script from an event.
        /// </summary>
        /// <param name="evt">The event name.</param>
        /// <param name="script">The script.</param>
        public static void UnsubscribeEvent(string evt, string script)
        {
            global::NWN.Core.NWNX.EventsPlugin.UnsubscribeEvent(evt, script);
        }

        /// <summary>
        /// Pushes event data at the provided tag, which subscribers can access with GetEventData.
        /// </summary>
        /// <param name="tag">The tag for the event data.</param>
        /// <param name="data">The data to push.</param>
        /// <remarks>This should be called BEFORE SignalEvent.</remarks>
        public static void PushEventData(string tag, string data)
        {
            global::NWN.Core.NWNX.EventsPlugin.PushEventData(tag, data);
        }

        /// <summary>
        /// Signals an event. This will dispatch a notification to all subscribed handlers.
        /// </summary>
        /// <param name="evt">The event name.</param>
        /// <param name="target">The target object.</param>
        /// <returns>True if anyone was subscribed to the event, false otherwise.</returns>
        /// <remarks>target will be available as OBJECT_SELF in subscribed event scripts.</remarks>
        public static bool SignalEvent(string evt, uint target)
        {
            var result = global::NWN.Core.NWNX.EventsPlugin.SignalEvent(evt, target);
            return result != 0;
        }

        /// <summary>
        /// Retrieves the event data for the currently executing script.
        /// </summary>
        /// <param name="tag">The tag for the event data.</param>
        /// <returns>The event data.</returns>
        /// <remarks>THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.</remarks>
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