using System;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Core;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class UtilPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Util";

        public static string GetCurrentScriptName(int depth = 0)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetCurrentScriptName");
            NWNXPInvoke.NWNXPushInt(depth);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        public static string GetAsciiTableString()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetAsciiTableString");
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        public static int Hash(string str)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "Hash");
            NWNXPInvoke.NWNXPushString(str);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        public static int GetModuleMTime()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetModuleMtime");
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        public static string GetCustomToken(int customTokenNumber)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "customTokenNumber");
            NWNXPInvoke.NWNXPushInt(customTokenNumber);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        public static ItemProperty EffectToItemProperty(Effect effect)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "EffectTypeCast");
            NWNCore.NativeFunctions.nwnxPushEffect(effect.Handle);
            NWNXPInvoke.NWNXCallFunction();
            return new ItemProperty(NWNCore.NativeFunctions.nwnxPopItemProperty());
        }

        public static Effect ItemPropertyToEffect(ItemProperty ip)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "EffectTypeCast");
            NWNCore.NativeFunctions.nwnxPushItemProperty(ip.Handle);
            NWNXPInvoke.NWNXCallFunction();
            return new Effect(NWNCore.NativeFunctions.nwnxPopEffect());
        }

        public static string StripColors(string str)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "StripColors");
            NWNXPInvoke.NWNXPushString(str);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        public static string GetEnvironmentVariable(string varname)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetEnvironmentVariable");
            NWNXPInvoke.NWNXPushString(varname);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopString();
        }

        public static int GetMinutesPerHour()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMinutesPerHour");
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        public static void SetMinutesPerHour(int minutes)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetMinutesPerHour");
            NWNXPInvoke.NWNXPushInt(minutes);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static string EncodeStringForURL(string url)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "EncodeStringForURL");
            NWNXPInvoke.NWNXPushString(url);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        public static string GetFirstResRef(ResRefType type, string regexFilter = "", bool moduleResourcesOnly = true)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetFirstResRef");
            NWNXPInvoke.NWNXPushInt(moduleResourcesOnly ? 1 : 0);
            NWNXPInvoke.NWNXPushString(regexFilter);
            NWNXPInvoke.NWNXPushInt((int)type);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        public static string GetNextResRef()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetNextResRef");
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        /// @brief Get the world time as calendar day and time of day.
        /// @note This function is useful for calculating effect expiry times.
        /// @param fAdjustment An adjustment in seconds, 0.0f will return the current world time,
        /// positive or negative values will return a world time in the future or past.
        /// @return A NWNX_Util_WorldTime struct with the calendar day and time of day.
        public static WorldTime GetWorldTime(float fAdjustment = 0.0f)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetWorldTime");

            NWNCore.NativeFunctions.nwnxPushFloat(fAdjustment);
            NWNXPInvoke.NWNXCallFunction();

            return new WorldTime
            {
                TimeOfDay = NWNXPInvoke.NWNXPopInt(),
                CalendarDay = NWNXPInvoke.NWNXPopInt()
            };
        }

        public struct WorldTime
        {
            public int CalendarDay { get; set; }
            public int TimeOfDay { get; set; }
        }


        public static void SetResourceOverride(int nResType, string sOldName, string sNewName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetResourceOverride");

            NWNXPInvoke.NWNXPushString(sNewName);
            NWNXPInvoke.NWNXPushString(sOldName);
            NWNXPInvoke.NWNXPushInt(nResType);

            NWNXPInvoke.NWNXCallFunction();
        }

        public static string GetResourceOverride(int nResType, string sName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetResourceOverride");

            NWNXPInvoke.NWNXPushString(sName);
            NWNXPInvoke.NWNXPushInt(nResType);
            NWNXPInvoke.NWNXCallFunction();

            return NWNXPInvoke.NWNXPopString();
        }

        /// @brief Create a door.
        /// @param sResRef The ResRef of the door.
        /// @param locLocation The location to create the door at.
        /// @param sNewTag An optional new tag for the door.
        /// @return The door, or OBJECT_INVALID on failure.
        public static uint CreateDoor(string sResRef, Location locLocation, string sNewTag)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "CreateDoor");

            var vPosition = GetPositionFromLocation(locLocation);

            NWNXPInvoke.NWNXPushString(sNewTag);
            NWNCore.NativeFunctions.nwnxPushFloat(GetFacingFromLocation(locLocation));
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Z);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.X);
            NWNXPInvoke.NWNXPushObject(GetAreaFromLocation(locLocation));
            NWNXPInvoke.NWNXPushString(sResRef);
            NWNXPInvoke.NWNXCallFunction();

            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        /// @brief Set the object that will be returned by GetItemActivator.
        /// @param oObject An object.
        public static void SetItemActivator(uint oObject)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetItemActivator");

            NWNXPInvoke.NWNXPushObject(oObject);
            NWNXPInvoke.NWNXCallFunction();
        }

        /// <summary>
        /// Get if a script param is set
        /// </summary>
        /// <param name="paramName">The script parameter name to check</param>
        /// <returns>true if script param is set, false if not or on error.</returns>
        public static bool GetScriptParamIsSet(string paramName)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetScriptParamIsSet");

            NWNXPInvoke.NWNXPushString(paramName);
            NWNXPInvoke.NWNXCallFunction();

            return Convert.ToBoolean(NWNXPInvoke.NWNXPopInt());
        }

        public static void SetDawnHour(int nDawnHour)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetDawnHour");

            NWNXPInvoke.NWNXPushInt(nDawnHour);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static void SetDuskHour(int nDuskHour)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetDuskHour");

            NWNXPInvoke.NWNXPushInt(nDuskHour);
            NWNXPInvoke.NWNXCallFunction();
        }

        public static HighResTimestamp GetHighResTimeStamp()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetHighResTimeStamp");
            NWNXPInvoke.NWNXCallFunction();

            return new HighResTimestamp
            {
                Microseconds = NWNXPInvoke.NWNXPopInt(),
                Seconds = NWNXPInvoke.NWNXPopInt()
            };
        }
}
}