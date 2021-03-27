namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Events
    {
        private const string PLUGIN_NAME = "NWNX_Events";

        // Scripts can subscribe to events.
        // Some events are dispatched via the NWNX plugin (see NWNX_EVENTS_EVENT_* constants).
        // Others can be signalled via script code (see NWNX_Events_SignalEvent).
        public static void SubscribeEvent(string evt, string script)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SubscribeEvent");
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxPushString(evt);
            Internal.NativeFunctions.nwnxCallFunction();
        }
        public static void UnsubscribeEvent(string evt, string script)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "UnsubscribeEvent");
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxPushString(evt);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Pushes event data at the provided tag, which subscribers can access with GetEventData.
        // This should be called BEFORE SignalEvent.
        public static void PushEventData(string tag, string data)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PushEventData");
            Internal.NativeFunctions.nwnxPushString(data);
            Internal.NativeFunctions.nwnxPushString(tag);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Signals an event. This will dispatch a notification to all subscribed handlers.
        // Returns true if anyone was subscribed to the event, false otherwise.
        public static int SignalEvent(string evt, uint target)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SignalEvent");
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushString(evt);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Retrieves the event data for the currently executing script.
        /// THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        public static string GetEventData(string tag)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEventData");
            Internal.NativeFunctions.nwnxPushString(tag);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
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
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SkipEvent");
            Internal.NativeFunctions.nwnxCallFunction();
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
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetEventResult");
            Internal.NativeFunctions.nwnxPushString(data);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Returns the current event name
        // THIS SHOULD ONLY BE CALLED FROM WITHIN AN EVENT HANDLER.
        public static string GetCurrentEvent()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentEvent");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Toggles DispatchListMode for sEvent+sScript
        // If enabled, sEvent for sScript will only be signalled if the target object is on its dispatch list.
        public static void ToggleDispatchListMode(string sEvent, string sScript, int bEnable)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ToggleDispatchListMode");
            Internal.NativeFunctions.nwnxPushInt(bEnable);
            Internal.NativeFunctions.nwnxPushString(sScript);
            Internal.NativeFunctions.nwnxPushString(sEvent);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Add oObject to the dispatch list for sEvent+sScript.
        public static void AddObjectToDispatchList(string sEvent, string sScript, uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddObjectToDispatchList");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxPushString(sScript);
            Internal.NativeFunctions.nwnxPushString(sEvent);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Remove oObject from the dispatch list for sEvent+sScript.
        public static void RemoveObjectFromDispatchList(string sEvent, string sScript, uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveObjectFromDispatchList");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxPushString(sScript);
            Internal.NativeFunctions.nwnxPushString(sEvent);
            Internal.NativeFunctions.nwnxCallFunction();
        }


        public static void ToggleIDWhitelist(string sEvent, bool bEnable)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ToggleIDWhitelist");
            Internal.NativeFunctions.nwnxPushInt(bEnable ? 1 : 0);
            Internal.NativeFunctions.nwnxPushString(sEvent);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void AddIDToWhitelist(string sEvent, int nID)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddIDToWhitelist");
            Internal.NativeFunctions.nwnxPushInt(nID);
            Internal.NativeFunctions.nwnxPushString(sEvent);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void RemoveIDFromWhitelist(string sEvent, int nID)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveIDFromWhitelist");
            Internal.NativeFunctions.nwnxPushInt(nID);
            Internal.NativeFunctions.nwnxPushString(sEvent);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}