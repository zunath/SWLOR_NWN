using System;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class UtilPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Util";

        public static string GetCurrentScriptName(int depth = 0)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetCurrentScriptName");
            NWNCore.NativeFunctions.nwnxPushInt(depth);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static string GetAsciiTableString()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAsciiTableString");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static int Hash(string str)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "Hash");
            NWNCore.NativeFunctions.nwnxPushString(str);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static int GetModuleMTime()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetModuleMtime");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static string GetCustomToken(int customTokenNumber)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "customTokenNumber");
            NWNCore.NativeFunctions.nwnxPushInt(customTokenNumber);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static ItemProperty EffectToItemProperty(Effect effect)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "EffectTypeCast");
            NWNCore.NativeFunctions.nwnxPushEffect(effect.Handle);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return new ItemProperty(NWNCore.NativeFunctions.nwnxPopItemProperty());
        }

        public static Effect ItemPropertyToEffect(ItemProperty ip)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "EffectTypeCast");
            NWNCore.NativeFunctions.nwnxPushItemProperty(ip.Handle);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return new Effect(NWNCore.NativeFunctions.nwnxPopEffect());
        }

        public static string StripColors(string str)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "StripColors");
            NWNCore.NativeFunctions.nwnxPushString(str);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static string GetEnvironmentVariable(string varname)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEnvironmentVariable");
            NWNCore.NativeFunctions.nwnxPushString(varname);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static int GetMinutesPerHour()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMinutesPerHour");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        public static void SetMinutesPerHour(int minutes)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetMinutesPerHour");
            NWNCore.NativeFunctions.nwnxPushInt(minutes);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static string EncodeStringForURL(string url)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "EncodeStringForURL");
            NWNCore.NativeFunctions.nwnxPushString(url);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static string GetFirstResRef(ResRefType type, string regexFilter = "", bool moduleResourcesOnly = true)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFirstResRef");
            NWNCore.NativeFunctions.nwnxPushInt(moduleResourcesOnly ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushString(regexFilter);
            NWNCore.NativeFunctions.nwnxPushInt((int)type);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        public static string GetNextResRef()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNextResRef");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        /// @brief Get the world time as calendar day and time of day.
        /// @note This function is useful for calculating effect expiry times.
        /// @param fAdjustment An adjustment in seconds, 0.0f will return the current world time,
        /// positive or negative values will return a world time in the future or past.
        /// @return A NWNX_Util_WorldTime struct with the calendar day and time of day.
        public static WorldTime GetWorldTime(float fAdjustment = 0.0f)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetWorldTime");

            NWNCore.NativeFunctions.nwnxPushFloat(fAdjustment);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return new WorldTime
            {
                TimeOfDay = NWNCore.NativeFunctions.nwnxPopInt(),
                CalendarDay = NWNCore.NativeFunctions.nwnxPopInt()
            };
        }

        public struct WorldTime
        {
            public int CalendarDay { get; set; }
            public int TimeOfDay { get; set; }
        }


        public static void SetResourceOverride(int nResType, string sOldName, string sNewName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetResourceOverride");

            NWNCore.NativeFunctions.nwnxPushString(sNewName);
            NWNCore.NativeFunctions.nwnxPushString(sOldName);
            NWNCore.NativeFunctions.nwnxPushInt(nResType);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static string GetResourceOverride(int nResType, string sName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetResourceOverride");

            NWNCore.NativeFunctions.nwnxPushString(sName);
            NWNCore.NativeFunctions.nwnxPushInt(nResType);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopString();
        }

        /// @brief Create a door.
        /// @param sResRef The ResRef of the door.
        /// @param locLocation The location to create the door at.
        /// @param sNewTag An optional new tag for the door.
        /// @return The door, or OBJECT_INVALID on failure.
        public static uint CreateDoor(string sResRef, Location locLocation, string sNewTag)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CreateDoor");

            var vPosition = NWScript.NWScript.GetPositionFromLocation(locLocation);

            NWNCore.NativeFunctions.nwnxPushString(sNewTag);
            NWNCore.NativeFunctions.nwnxPushFloat(NWScript.NWScript.GetFacingFromLocation(locLocation));
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Z);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.X);
            NWNCore.NativeFunctions.nwnxPushObject(NWScript.NWScript.GetAreaFromLocation(locLocation));
            NWNCore.NativeFunctions.nwnxPushString(sResRef);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        /// @brief Set the object that will be returned by GetItemActivator.
        /// @param oObject An object.
        public static void SetItemActivator(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetItemActivator");

            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// <summary>
        /// Get if a script param is set
        /// </summary>
        /// <param name="paramName">The script parameter name to check</param>
        /// <returns>true if script param is set, false if not or on error.</returns>
        public static bool GetScriptParamIsSet(string paramName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetScriptParamIsSet");

            NWNCore.NativeFunctions.nwnxPushString(paramName);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return Convert.ToBoolean(NWNCore.NativeFunctions.nwnxPopInt());
        }

        public static void SetDawnHour(int nDawnHour)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDawnHour");

            NWNCore.NativeFunctions.nwnxPushInt(nDawnHour);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetDuskHour(int nDuskHour)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDuskHour");

            NWNCore.NativeFunctions.nwnxPushInt(nDuskHour);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static HighResTimestamp GetHighResTimeStamp()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetHighResTimeStamp");
            NWNCore.NativeFunctions.nwnxCallFunction();

            return new HighResTimestamp
            {
                Microseconds = NWNCore.NativeFunctions.nwnxPopInt(),
                Seconds = NWNCore.NativeFunctions.nwnxPopInt()
            };
        }
}
}