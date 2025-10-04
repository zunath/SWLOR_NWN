using System;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class UtilPlugin
    {
        private static IUtilPluginService _service = new UtilPluginService();

        internal static void SetService(IUtilPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IUtilPluginService.GetCurrentScriptName"/>
        public static string GetCurrentScriptName(int depth = 0) => _service.GetCurrentScriptName(depth);

        /// <inheritdoc cref="IUtilPluginService.GetAsciiTableString"/>
        public static string GetAsciiTableString() => _service.GetAsciiTableString();

        /// <inheritdoc cref="IUtilPluginService.Hash"/>
        public static int Hash(string str) => _service.Hash(str);

        /// <inheritdoc cref="IUtilPluginService.GetModuleMTime"/>
        public static int GetModuleMTime() => _service.GetModuleMTime();

        /// <inheritdoc cref="IUtilPluginService.GetCustomToken"/>
        public static string GetCustomToken(int customTokenNumber) => _service.GetCustomToken(customTokenNumber);

        /// <inheritdoc cref="IUtilPluginService.EffectToItemProperty"/>
        public static ItemProperty EffectToItemProperty(Effect effect) => _service.EffectToItemProperty(effect);

        /// <inheritdoc cref="IUtilPluginService.ItemPropertyToEffect"/>
        public static Effect ItemPropertyToEffect(ItemProperty ip) => _service.ItemPropertyToEffect(ip);

        /// <inheritdoc cref="IUtilPluginService.StripColors"/>
        public static string StripColors(string str) => _service.StripColors(str);

        /// <inheritdoc cref="IUtilPluginService.GetEnvironmentVariable"/>
        public static string GetEnvironmentVariable(string varname) => _service.GetEnvironmentVariable(varname);

        /// <inheritdoc cref="IUtilPluginService.GetMinutesPerHour"/>
        public static int GetMinutesPerHour() => _service.GetMinutesPerHour();

        /// <inheritdoc cref="IUtilPluginService.SetMinutesPerHour"/>
        public static void SetMinutesPerHour(int minutes) => _service.SetMinutesPerHour(minutes);

        /// <inheritdoc cref="IUtilPluginService.EncodeStringForURL"/>
        public static string EncodeStringForURL(string url) => _service.EncodeStringForURL(url);

        /// <inheritdoc cref="IUtilPluginService.GetFirstResRef"/>
        public static string GetFirstResRef(ResRefType type, string regexFilter = "", bool moduleResourcesOnly = true) => _service.GetFirstResRef(type, regexFilter, moduleResourcesOnly);

        /// <inheritdoc cref="IUtilPluginService.GetNextResRef"/>
        public static string GetNextResRef() => _service.GetNextResRef();

        /// <inheritdoc cref="IUtilPluginService.GetWorldTime"/>
        public static UtilPluginService.WorldTime GetWorldTime(float fAdjustment = 0.0f) => _service.GetWorldTime(fAdjustment);

        /// <inheritdoc cref="IUtilPluginService.SetResourceOverride"/>
        public static void SetResourceOverride(int nResType, string sOldName, string sNewName) => _service.SetResourceOverride(nResType, sOldName, sNewName);

        /// <inheritdoc cref="IUtilPluginService.GetResourceOverride"/>
        public static string GetResourceOverride(int nResType, string sName) => _service.GetResourceOverride(nResType, sName);

        /// <inheritdoc cref="IUtilPluginService.CreateDoor"/>
        public static uint CreateDoor(string sResRef, Location locLocation, string sNewTag) => _service.CreateDoor(sResRef, locLocation, sNewTag);

        /// <inheritdoc cref="IUtilPluginService.SetItemActivator"/>
        public static void SetItemActivator(uint oObject) => _service.SetItemActivator(oObject);

        /// <inheritdoc cref="IUtilPluginService.GetScriptParamIsSet"/>
        public static bool GetScriptParamIsSet(string paramName) => _service.GetScriptParamIsSet(paramName);

        /// <inheritdoc cref="IUtilPluginService.SetDawnHour"/>
        public static void SetDawnHour(int nDawnHour) => _service.SetDawnHour(nDawnHour);

        /// <inheritdoc cref="IUtilPluginService.SetDuskHour"/>
        public static void SetDuskHour(int nDuskHour) => _service.SetDuskHour(nDuskHour);

        /// <inheritdoc cref="IUtilPluginService.GetHighResTimeStamp"/>
        public static UtilPluginService.HighResTimestamp GetHighResTimeStamp() => _service.GetHighResTimeStamp();
    }
}
