using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public class UtilPluginService : IUtilPluginService
    {
        /// <inheritdoc/>
        public string GetCurrentScriptName(int depth = 0)
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetCurrentScriptName(depth);
        }

        /// <inheritdoc/>
        public string GetAsciiTableString()
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetAsciiTableString();
        }

        /// <inheritdoc/>
        public int Hash(string str)
        {
            return global::NWN.Core.NWNX.UtilPlugin.Hash(str);
        }

        /// <inheritdoc/>
        public int GetModuleMTime()
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetModuleMtime();
        }

        /// <inheritdoc/>
        public string GetCustomToken(int customTokenNumber)
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetCustomToken(customTokenNumber);
        }

        /// <inheritdoc/>
        public ItemProperty EffectToItemProperty(Effect effect)
        {
            var result = global::NWN.Core.NWNX.UtilPlugin.EffectToItemProperty(effect.Handle);
            return new ItemProperty(result);
        }

        /// <inheritdoc/>
        public Effect ItemPropertyToEffect(ItemProperty ip)
        {
            var result = global::NWN.Core.NWNX.UtilPlugin.ItemPropertyToEffect(ip.Handle);
            return new Effect(result);
        }

        /// <inheritdoc/>
        public string StripColors(string str)
        {
            return global::NWN.Core.NWNX.UtilPlugin.StripColors(str);
        }

        /// <inheritdoc/>
        public string GetEnvironmentVariable(string varname)
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetEnvironmentVariable(varname);
        }

        /// <inheritdoc/>
        public int GetMinutesPerHour()
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetMinutesPerHour();
        }

        /// <inheritdoc/>
        public void SetMinutesPerHour(int minutes)
        {
            global::NWN.Core.NWNX.UtilPlugin.SetMinutesPerHour(minutes);
        }

        /// <inheritdoc/>
        public string EncodeStringForURL(string url)
        {
            return global::NWN.Core.NWNX.UtilPlugin.EncodeStringForURL(url);
        }

        /// <inheritdoc/>
        public string GetFirstResRef(ResRefType type, string regexFilter = "", bool moduleResourcesOnly = true)
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetFirstResRef((int)type, regexFilter, moduleResourcesOnly ? 1 : 0);
        }

        /// <inheritdoc/>
        public string GetNextResRef()
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetNextResRef();
        }

        /// <inheritdoc/>
        public WorldTime GetWorldTime(float fAdjustment = 0.0f)
        {
            var coreResult = global::NWN.Core.NWNX.UtilPlugin.GetWorldTime(fAdjustment);
            return new WorldTime
            {
                TimeOfDay = coreResult.nTimeOfDay,
                CalendarDay = coreResult.nCalendarDay
            };
        }

        /// <summary>
        /// A world time struct containing calendar day and time of day.
        /// </summary>
        public struct WorldTime
        {
            /// <summary>
            /// The calendar day.
            /// </summary>
            public int CalendarDay { get; set; }
            
            /// <summary>
            /// The time of day.
            /// </summary>
            public int TimeOfDay { get; set; }
        }

        /// <summary>
        /// A high resolution timestamp struct containing seconds and microseconds.
        /// </summary>
        public struct HighResTimestamp
        {
            /// <summary>
            /// The seconds component of the timestamp.
            /// </summary>
            public int Seconds { get; set; }
            
            /// <summary>
            /// The microseconds component of the timestamp.
            /// </summary>
            public int Microseconds { get; set; }
        }


        /// <inheritdoc/>
        public void SetResourceOverride(int nResType, string sOldName, string sNewName)
        {
            global::NWN.Core.NWNX.UtilPlugin.SetResourceOverride(nResType, sOldName, sNewName);
        }

        /// <inheritdoc/>
        public string GetResourceOverride(int nResType, string sName)
        {
            return global::NWN.Core.NWNX.UtilPlugin.GetResourceOverride(nResType, sName);
        }

        /// <inheritdoc/>
        public uint CreateDoor(string sResRef, Location locLocation, string sNewTag)
        {
            return global::NWN.Core.NWNX.UtilPlugin.CreateDoor(sResRef, locLocation.Handle, sNewTag);
        }

        /// <inheritdoc/>
        public void SetItemActivator(uint oObject)
        {
            global::NWN.Core.NWNX.UtilPlugin.SetItemActivator(oObject);
        }

        /// <inheritdoc/>
        public bool GetScriptParamIsSet(string paramName)
        {
            int result = global::NWN.Core.NWNX.UtilPlugin.GetScriptParamIsSet(paramName);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetDawnHour(int nDawnHour)
        {
            global::NWN.Core.NWNX.UtilPlugin.SetDawnHour(nDawnHour);
        }

        /// <inheritdoc/>
        public void SetDuskHour(int nDuskHour)
        {
            global::NWN.Core.NWNX.UtilPlugin.SetDuskHour(nDuskHour);
        }

        /// <inheritdoc/>
        public HighResTimestamp GetHighResTimeStamp()
        {
            var coreResult = global::NWN.Core.NWNX.UtilPlugin.GetHighResTimeStamp();
            return new HighResTimestamp
            {
                Microseconds = coreResult.microseconds,
                Seconds = coreResult.seconds
            };
        }
}
}