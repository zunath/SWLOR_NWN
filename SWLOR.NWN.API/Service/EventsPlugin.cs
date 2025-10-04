using System;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.NWN.API.Service
{
    public static class EventsPlugin
    {
        private static IEventsPluginService _service = new EventsPluginService();

        internal static void SetService(IEventsPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IEventsPluginService.SubscribeEvent"/>
        public static void SubscribeEvent(string evt, string script) => _service.SubscribeEvent(evt, script);

        /// <inheritdoc cref="IEventsPluginService.UnsubscribeEvent"/>
        public static void UnsubscribeEvent(string evt, string script) => _service.UnsubscribeEvent(evt, script);

        /// <inheritdoc cref="IEventsPluginService.PushEventData"/>
        public static void PushEventData(string tag, string data) => _service.PushEventData(tag, data);

        /// <inheritdoc cref="IEventsPluginService.SignalEvent"/>
        public static bool SignalEvent(string evt, uint target) => _service.SignalEvent(evt, target);

        /// <inheritdoc cref="IEventsPluginService.GetEventData"/>
        public static string GetEventData(string tag) => _service.GetEventData(tag);

        /// <inheritdoc cref="IEventsPluginService.SkipEvent"/>
        public static void SkipEvent() => _service.SkipEvent();

        /// <inheritdoc cref="IEventsPluginService.SetEventResult"/>
        public static void SetEventResult(string data) => _service.SetEventResult(data);

        /// <inheritdoc cref="IEventsPluginService.GetCurrentEvent"/>
        public static string GetCurrentEvent() => _service.GetCurrentEvent();

        /// <inheritdoc cref="IEventsPluginService.ToggleDispatchListMode"/>
        public static void ToggleDispatchListMode(string sEvent, string sScript, bool bEnable) => 
            _service.ToggleDispatchListMode(sEvent, sScript, bEnable);

        /// <inheritdoc cref="IEventsPluginService.AddObjectToDispatchList"/>
        public static void AddObjectToDispatchList(string sEvent, string sScript, uint oObject) => 
            _service.AddObjectToDispatchList(sEvent, sScript, oObject);

        /// <inheritdoc cref="IEventsPluginService.RemoveObjectFromDispatchList"/>
        public static void RemoveObjectFromDispatchList(string sEvent, string sScript, uint oObject) => 
            _service.RemoveObjectFromDispatchList(sEvent, sScript, oObject);

        /// <inheritdoc cref="IEventsPluginService.ToggleIDWhitelist"/>
        public static void ToggleIDWhitelist(string sEvent, bool bEnable) => _service.ToggleIDWhitelist(sEvent, bEnable);

        /// <inheritdoc cref="IEventsPluginService.AddIDToWhitelist"/>
        public static void AddIDToWhitelist(string sEvent, int nID) => _service.AddIDToWhitelist(sEvent, nID);

        /// <inheritdoc cref="IEventsPluginService.RemoveIDFromWhitelist"/>
        public static void RemoveIDFromWhitelist(string sEvent, int nID) => _service.RemoveIDFromWhitelist(sEvent, nID);
    }
}
