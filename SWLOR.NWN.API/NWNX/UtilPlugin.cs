using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWNX
{
    public static class UtilPlugin
    {
        /// <summary>
        /// Gets the name of the currently executing script.
        /// </summary>
        /// <param name="depth">If depth is > 0, it will return the name of the script that called this one via ExecuteScript().</param>
        /// <returns>The name of the currently executing script.</returns>
        public static string GetCurrentScriptName(int depth = 0)
        {
            return UtilPluginRuntime.Current.GetCurrentScriptName(depth);
        }

        /// <summary>
        /// Gets a string that contains the ascii table.
        /// </summary>
        /// <remarks>The character at index 0 is a space.</remarks>
        /// <returns>A string that contains all characters at their position (e.g. 'A' at 65).</returns>
        public static string GetAsciiTableString()
        {
            return UtilPluginRuntime.Current.GetAsciiTableString();
        }

        /// <summary>
        /// Gets an integer hash of a string.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>The hashed string as an integer.</returns>
        public static int Hash(string str)
        {
            return UtilPluginRuntime.Current.Hash(str);
        }

        /// <summary>
        /// Gets the last modified timestamp (mtime) of the module file in seconds.
        /// </summary>
        /// <returns>The mtime of the module file.</returns>
        public static int GetModuleMTime()
        {
            return UtilPluginRuntime.Current.GetModuleMTime();
        }

        /// <summary>
        /// Gets the value of customTokenNumber.
        /// </summary>
        /// <param name="customTokenNumber">The token number to query.</param>
        /// <returns>The string representation of the token value.</returns>
        public static string GetCustomToken(int customTokenNumber)
        {
            return UtilPluginRuntime.Current.GetCustomToken(customTokenNumber);
        }

        /// <summary>
        /// Convert an effect type to an itemproperty type.
        /// </summary>
        /// <param name="effect">The effect to convert to an itemproperty.</param>
        /// <returns>The converted itemproperty.</returns>
        public static ItemProperty EffectToItemProperty(Effect effect)
        {
            return UtilPluginRuntime.Current.EffectToItemProperty(effect);
        }

        /// <summary>
        /// Convert an itemproperty type to an effect type.
        /// </summary>
        /// <param name="ip">The itemproperty to convert to an effect.</param>
        /// <returns>The converted effect.</returns>
        public static Effect ItemPropertyToEffect(ItemProperty ip)
        {
            return UtilPluginRuntime.Current.ItemPropertyToEffect(ip);
        }

        /// <summary>
        /// Strip any color codes from a string.
        /// </summary>
        /// <param name="str">The string to strip of color.</param>
        /// <returns>The new string without any color codes.</returns>
        public static string StripColors(string str)
        {
            return UtilPluginRuntime.Current.StripColors(str);
        }

        /// <summary>
        /// Retrieves an environment variable.
        /// </summary>
        /// <param name="varname">The environment variable to query.</param>
        /// <returns>The value of the environment variable.</returns>
        public static string GetEnvironmentVariable(string varname)
        {
            return UtilPluginRuntime.Current.GetEnvironmentVariable(varname);
        }

        /// <summary>
        /// Gets the module real life minutes per in game hour.
        /// </summary>
        /// <returns>The minutes per hour.</returns>
        public static int GetMinutesPerHour()
        {
            return UtilPluginRuntime.Current.GetMinutesPerHour();
        }

        /// <summary>
        /// Set module real life minutes per in game hour.
        /// </summary>
        /// <param name="minutes">The minutes per hour.</param>
        public static void SetMinutesPerHour(int minutes)
        {
            UtilPluginRuntime.Current.SetMinutesPerHour(minutes);
        }

        /// <summary>
        /// Encodes a string for usage in a URL.
        /// </summary>
        /// <param name="url">The string to encode for a URL.</param>
        /// <returns>The url encoded string.</returns>
        public static string EncodeStringForURL(string url)
        {
            return UtilPluginRuntime.Current.EncodeStringForURL(url);
        }

        /// <summary>
        /// Get the first resref of nType.
        /// </summary>
        /// <param name="type">A Resref Type.</param>
        /// <param name="regexFilter">Lets you filter out resrefs using a regexfilter. For example: **nwnx_.*** gets you all scripts prefixed with nwnx_ when using the NSS resref type.</param>
        /// <param name="moduleResourcesOnly">If TRUE only custom resources will be returned.</param>
        /// <returns>The first resref found or "" if none is found.</returns>
        public static string GetFirstResRef(ResRefType type, string regexFilter = "", bool moduleResourcesOnly = true)
        {
            return UtilPluginRuntime.Current.GetFirstResRef(type, regexFilter, moduleResourcesOnly);
        }

        /// <summary>
        /// Get the next resref.
        /// </summary>
        /// <returns>The next resref found or "" if none is found.</returns>
        public static string GetNextResRef()
        {
            return UtilPluginRuntime.Current.GetNextResRef();
        }

        /// <summary>
        /// Get the world time as calendar day and time of day.
        /// </summary>
        /// <remarks>This function is useful for calculating effect expiry times.</remarks>
        /// <param name="fAdjustment">An adjustment in seconds, 0.0f will return the current world time, positive or negative values will return a world time in the future or past.</param>
        /// <returns>A WorldTime struct with the calendar day and time of day.</returns>
        public static WorldTime GetWorldTime(float fAdjustment = 0.0f)
        {
            return UtilPluginRuntime.Current.GetWorldTime(fAdjustment);
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


        /// <summary>
        /// Set a server-side resource override.
        /// </summary>
        /// <param name="nResType">A Resref Type.</param>
        /// <param name="sOldName">The old resource name, 16 characters or less.</param>
        /// <param name="sNewName">The new resource name or "" to clear a previous override, 16 characters or less.</param>
        public static void SetResourceOverride(int nResType, string sOldName, string sNewName)
        {
            UtilPluginRuntime.Current.SetResourceOverride(nResType, sOldName, sNewName);
        }

        /// <summary>
        /// Get a server-side resource override.
        /// </summary>
        /// <param name="nResType">A Resref Type.</param>
        /// <param name="sName">The name of the resource, 16 characters or less.</param>
        /// <returns>The resource override, or "" if one is not set.</returns>
        public static string GetResourceOverride(int nResType, string sName)
        {
            return UtilPluginRuntime.Current.GetResourceOverride(nResType, sName);
        }

        /// <summary>
        /// Create a door.
        /// </summary>
        /// <param name="sResRef">The ResRef of the door.</param>
        /// <param name="locLocation">The location to create the door at.</param>
        /// <param name="sNewTag">An optional new tag for the door.</param>
        /// <returns>The door, or OBJECT_INVALID on failure.</returns>
        public static uint CreateDoor(string sResRef, Location locLocation, string sNewTag)
        {
            return UtilPluginRuntime.Current.CreateDoor(sResRef, locLocation, sNewTag);
        }

        /// <summary>
        /// Set the object that will be returned by GetItemActivator.
        /// </summary>
        /// <param name="oObject">An object.</param>
        public static void SetItemActivator(uint oObject)
        {
            UtilPluginRuntime.Current.SetItemActivator(oObject);
        }

        /// <summary>
        /// Get if a script param is set.
        /// </summary>
        /// <param name="paramName">The script parameter name to check.</param>
        /// <returns>true if script param is set, false if not or on error.</returns>
        public static bool GetScriptParamIsSet(string paramName)
        {
            return UtilPluginRuntime.Current.GetScriptParamIsSet(paramName);
        }

        /// <summary>
        /// Set the module dawn hour.
        /// </summary>
        /// <param name="nDawnHour">The new dawn hour</param>
        public static void SetDawnHour(int nDawnHour)
        {
            UtilPluginRuntime.Current.SetDawnHour(nDawnHour);
        }

        /// <summary>
        /// Set the module dusk hour.
        /// </summary>
        /// <param name="nDuskHour">The new dusk hour</param>
        public static void SetDuskHour(int nDuskHour)
        {
            UtilPluginRuntime.Current.SetDuskHour(nDuskHour);
        }

        /// <summary>
        /// Returns the number of microseconds since midnight on January 1, 1970.
        /// </summary>
        /// <returns>A HighResTimestamp struct with seconds and microseconds.</returns>
        public static HighResTimestamp GetHighResTimeStamp()
        {
            return UtilPluginRuntime.Current.GetHighResTimeStamp();
        }
}
}
