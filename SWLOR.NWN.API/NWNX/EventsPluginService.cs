using SWLOR.NWN.API.Contracts;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive event system functionality for subscribing to, managing, and dispatching
    /// custom events throughout the game. This plugin enables script-to-script communication,
    /// event-driven programming patterns, and advanced event filtering and management capabilities.
    /// </summary>
    public class EventsPluginService : IEventsPluginService
    {
        /// <inheritdoc/>
        public void SubscribeEvent(string evt, string script)
        {
            global::NWN.Core.NWNX.EventsPlugin.SubscribeEvent(evt, script);
        }
        /// <inheritdoc/>
        public void UnsubscribeEvent(string evt, string script)
        {
            global::NWN.Core.NWNX.EventsPlugin.UnsubscribeEvent(evt, script);
        }

        /// <inheritdoc/>
        public void PushEventData(string tag, string data)
        {
            global::NWN.Core.NWNX.EventsPlugin.PushEventData(tag, data);
        }

        /// <inheritdoc/>
        public bool SignalEvent(string evt, uint target)
        {
            var result = global::NWN.Core.NWNX.EventsPlugin.SignalEvent(evt, target);
            return result != 0;
        }

        /// <inheritdoc/>
        public string GetEventData(string tag)
        {
            return global::NWN.Core.NWNX.EventsPlugin.GetEventData(tag);
        }

        /// <inheritdoc/>
        public void SkipEvent()
        {
            global::NWN.Core.NWNX.EventsPlugin.SkipEvent();
        }

        /// <inheritdoc/>
        public void SetEventResult(string data)
        {
            global::NWN.Core.NWNX.EventsPlugin.SetEventResult(data);
        }

        /// <inheritdoc/>
        public string GetCurrentEvent()
        {
            return global::NWN.Core.NWNX.EventsPlugin.GetCurrentEvent();
        }

        /// <inheritdoc/>
        public void ToggleDispatchListMode(string sEvent, string sScript, bool bEnable)
        {
            global::NWN.Core.NWNX.EventsPlugin.ToggleDispatchListMode(sEvent, sScript, bEnable ? 1 : 0);
        }

        /// <inheritdoc/>
        public void AddObjectToDispatchList(string sEvent, string sScript, uint oObject)
        {
            global::NWN.Core.NWNX.EventsPlugin.AddObjectToDispatchList(sEvent, sScript, oObject);
        }

        /// <inheritdoc/>
        public void RemoveObjectFromDispatchList(string sEvent, string sScript, uint oObject)
        {
            global::NWN.Core.NWNX.EventsPlugin.RemoveObjectFromDispatchList(sEvent, sScript, oObject);
        }


        /// <inheritdoc/>
        public void ToggleIDWhitelist(string sEvent, bool bEnable)
        {
            global::NWN.Core.NWNX.EventsPlugin.ToggleIDWhitelist(sEvent, bEnable ? 1 : 0);
        }

        /// <inheritdoc/>
        public void AddIDToWhitelist(string sEvent, int nID)
        {
            global::NWN.Core.NWNX.EventsPlugin.AddIDToWhitelist(sEvent, nID);
        }

        /// <inheritdoc/>
        public void RemoveIDFromWhitelist(string sEvent, int nID)
        {
            global::NWN.Core.NWNX.EventsPlugin.RemoveIDFromWhitelist(sEvent, nID);
        }
    }
}