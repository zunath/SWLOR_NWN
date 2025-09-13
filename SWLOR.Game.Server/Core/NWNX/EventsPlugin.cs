namespace SWLOR.Game.Server.Core.NWNX
{
    public static class EventsPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Events";

        // Scripts can subscribe to events.
        // Some events are dispatched via the NWNX plugin (see NWNX_EVENTS_EVENT_* constants).
        // Others can be signalled via script code (see NWNX_Events_SignalEvent).
        public static void SubscribeEvent(string evt, string script)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SubscribeEvent");
            NWNXPInvoke.NWNXPushString(script);
            NWNXPInvoke.NWNXPushString(evt);
            NWNXPInvoke.NWNXCallFunction();
        }
        public static void UnsubscribeEvent(string evt, string script)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "UnsubscribeEvent");
            NWNXPInvoke.NWNXPushString(script);
            NWNXPInvoke.NWNXPushString(evt);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Pushes event data at the provided tag, which subscribers can access with GetEventData.
        // This should be called BEFORE SignalEvent.
        public static void PushEventData(string tag, string data)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "PushEventData");
            NWNXPInvoke.NWNXPushString(data);
            NWNXPInvoke.NWNXPushString(tag);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Signals an event. This will dispatch a notification to all subscribed handlers.
        // Returns true if anyone was subscribed to the event, false otherwise.
        public static int SignalEvent(string evt, uint target)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SignalEvent");
            NWNXPInvoke.NWNXPushObject(target);
            NWNXPInvoke.NWNXPushString(evt);
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Retrieves the event data for the currently executing script.
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        public static string GetEventData(string tag)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetEventData");
            NWNXPInvoke.NWNXPushString(tag);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        // Skips execution of the currently executing event.
        // If this is a NWNX event, that means that the base function call won't be called.
        // This won't impact any other subscribers, nor dispatch for before / after functions.
        // For example, if you are subscribing to NWNX_ON_EXAMINE_OBJECT_BEFORE, and you skip ...
        // - The other subscribers will still be called.
        // - The original function in the base game will be skipped.
        // - The matching after event (NWNX_ON_EXAMINE_OBJECT_AFTER) will also be executed.
        //
        // THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        // ONLY WORKS WITH THE FOLLOWING EVENTS:
        // - Feat events
        // - Item events
        // - Healer's Kit event
        // - CombatMode events
        // - Party events
        // - Skill events
        // - Map events
        // - Listen/Spot Detection events
        // - Polymorph events
        // - DMAction events
        // - Client connect event
        // - Spell events
        // - QuickChat events
        // - Barter event (START only)
        // - Trap events
        // - Sticky Player Name event
        public static void SkipEvent()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SkipEvent");
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set the return value of the event.
        // THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        // ONLY WORKS WITH THE FOLLOWING EVENTS:
        // - Healer's Kit event
        // - Listen/Spot Detection events -> "1" or "0"
        // - OnClientConnectBefore -> Reason for disconnect if skipped
        // - Ammo Reload event -> Forced ammunition returned
        // - Trap events -> "1" or "0"
        // - Sticky Player Name event -> "1" or "0"
        public static void SetEventResult(string data)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetEventResult");
            NWNXPInvoke.NWNXPushString(data);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Returns the current event name
        // THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        public static string GetCurrentEvent()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCurrentEvent");
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        // Toggles DispatchListMode for sEvent+sScript
        // If enabled, sEvent for sScript will only be signalled if the target object is on its dispatch list.
        public static void ToggleDispatchListMode(string sEvent, string sScript, int bEnable)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "ToggleDispatchListMode");
            NWNXPInvoke.NWNXPushInt(bEnable);
            NWNXPInvoke.NWNXPushString(sScript);
            NWNXPInvoke.NWNXPushString(sEvent);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Add oObject to the dispatch list for sEvent+sScript.
        public static void AddObjectToDispatchList(string sEvent, string sScript, uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AddObjectToDispatchList");
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXPushString(sScript);
            NWNXPInvoke.NWNXPushString(sEvent);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Remove oObject from the dispatch list for sEvent+sScript.
        public static void RemoveObjectFromDispatchList(string sEvent, string sScript, uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RemoveObjectFromDispatchList");
            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXPushString(sScript);
            NWNXPInvoke.NWNXPushString(sEvent);
            NWNXPInvoke.NWNXCallFunction();
        }


        public static void ToggleIDWhitelist(string sEvent, bool bEnable)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "ToggleIDWhitelist");
            NWNXPInvoke.NWNXPushInt(bEnable ? 1 : 0);
            NWNXPInvoke.NWNXPushString(sEvent);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void AddIDToWhitelist(string sEvent, int nID)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "AddIDToWhitelist");
            NWNXPInvoke.NWNXPushInt(nID);
            NWNXPInvoke.NWNXPushString(sEvent);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void RemoveIDFromWhitelist(string sEvent, int nID)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RemoveIDFromWhitelist");
            NWNXPInvoke.NWNXPushInt(nID);
            NWNXPInvoke.NWNXPushString(sEvent);
            NWNXPInvoke.NWNXCallFunction();
        }
    }
}