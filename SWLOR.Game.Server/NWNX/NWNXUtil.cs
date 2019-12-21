using System;
using NWN;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXUtil
    {
        private const string NWNX_Util = "NWNX_Util";

        /// <summary>
        /// Gets the name of the currently executing script.
        /// If depth is > 0, it will return the name of the script that called this one via ExecuteScript().
        /// </summary>
        /// <param name="depth">depth to seek the executing script.</param>
        /// <returns>The name of the currently executing script.</returns>
        public static string GetCurrentScriptName(int depth = 0)
        {
            string sFunc = "GetCurrentScriptName";
            NWNX_PushArgumentInt(NWNX_Util, sFunc, depth);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Gets a string that contains the ascii table.
        /// The character at index 0 is a space.
        /// </summary>
        /// <returns>A string that contains all characters at their position (e.g. 'A' at 65).</returns>
        public static string GetAsciiTableString()
        {
            string sFunc = "GetAsciiTableString";
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Gets an integer hash of a string.
        /// The hashed string as an integer.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns></returns>
        public static int Hash(string str)
        {
            string sFunc = "Hash";
            NWNX_PushArgumentString(NWNX_Util, sFunc, str);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Gets the value of customTokenNumber.
        /// </summary>
        /// <param name="customTokenNumber">The token number to query.</param>
        /// <returns>The string representation of the token value.</returns>
        public static string GetCustomToken(int customTokenNumber)
        {
            string sFunc = "GetCustomToken";
            NWNX_PushArgumentInt(NWNX_Util, sFunc, customTokenNumber);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Convert an effect type to an itemproperty type.
        /// </summary>
        /// <param name="e">The effect to convert to an itemproperty.</param>
        /// <returns>The converted itemproperty.</returns>
        public static ItemProperty EffectToItemProperty(Effect e)
        {
            string sFunc = "EffectTypeCast";
            NWNX_PushArgumentEffect(NWNX_Util, sFunc, e);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueItemProperty(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Convert an itemproperty type to an effect type.
        /// </summary>
        /// <param name="ip">The itemproperty to convert to an effect.</param>
        /// <returns>The converted effect.</returns>
        public static Effect ItemPropertyToEffect(ItemProperty ip)
        {
            string sFunc = "EffectTypeCast";
            NWNX_PushArgumentItemProperty(NWNX_Util, sFunc, ip);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueEffect(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Generates a v4 UUID.
        /// </summary>
        /// <returns>A UUID string.</returns>
        public static string GenerateUUID()
        {
            string sFunc = "GenerateUUID";
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Strip any color codes from a string.
        /// </summary>
        /// <param name="str">The string to strip of color.</param>
        /// <returns>The new string without any color codes.</returns>
        public static string StripColors(string str)
        {
            string sFunc = "StripColors";
            NWNX_PushArgumentString(NWNX_Util, sFunc, str);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Determines if the supplied resref exists.
        /// </summary>
        /// <param name="resref">The resref to check.</param>
        /// <param name="type">The type of resref to check</param>
        /// <returns>true/false</returns>
        public static bool IsValidResRef(string resref, UtilResrefType type = UtilResrefType.Creature)
        {
            string sFunc = "IsValidResRef";
            NWNX_PushArgumentInt(NWNX_Util, sFunc, (int)type);
            NWNX_PushArgumentString(NWNX_Util, sFunc, resref);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return Convert.ToBoolean(NWNX_GetReturnValueInt(NWNX_Util, sFunc));
        }

        /// <summary>
        /// Retrieves an environment variable.
        /// </summary>
        /// <param name="sVarname">The environment variable to query.</param>
        /// <returns>The value of the environment variable.</returns>
        public static string GetEnvironmentVariable(string sVarname)
        {
            string sFunc = "GetEnvironmentVariable";
            NWNX_PushArgumentString(NWNX_Util, sFunc, sVarname);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Gets the module real life minutes per in game hour.
        /// </summary>
        /// <returns>The minutes per hour.</returns>
        public static int GetMinutesPerHour()
        {
            string sFunc = "GetMinutesPerHour";
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Set module real life minutes per in game hour.
        /// </summary>
        /// <param name="minutes">The minutes per hour.</param>
        public static void SetMinutesPerHour(int minutes)
        {
            string sFunc = "SetMinutesPerHour";
            NWNX_PushArgumentInt(NWNX_Util, sFunc, minutes);
            NWNX_CallFunction(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Encodes a string for usage in a URL.
        /// </summary>
        /// <param name="sURL">The string to encode for a URL.</param>
        /// <returns>The url encoded string.</returns>
        public static string EncodeStringForURL(string sURL)
        {
            string sFunc = "EncodeStringForURL";

            NWNX_PushArgumentString(NWNX_Util, sFunc, sURL);
            NWNX_CallFunction(NWNX_Util, sFunc);

            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Gets the row count for a 2da.
        /// </summary>
        /// <param name="str">The 2da to check (do not include the .2da).</param>
        /// <returns>The amount of rows in the 2da.</returns>
        public static int Get2DARowCount(string str)
        {
            string sFunc = "Get2DARowCount";
            NWNX_PushArgumentString(NWNX_Util, sFunc, str);
            NWNX_CallFunction(NWNX_Util, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Get the first resref of nType.
        /// </summary>
        /// <param name="nType">A @ref resref_types "Resref Type".</param>
        /// <param name="sRegexFilter">Lets you filter out resrefs using a regexfilter. For example: **nwnx_.\*** gets you all scripts prefixed with nwnx_ when using the NSS resref type.</param>
        /// <param name="bModuleResourcesOnly">If true only custom resources will be returned.</param>
        /// <returns>The first resref found or "" if none is found.</returns>
        public static string GetFirstResRef(int nType, string sRegexFilter = "", bool bModuleResourcesOnly = true)
        {
            string sFunc = "GetFirstResRef";

            NWNX_PushArgumentInt(NWNX_Util, sFunc, Convert.ToInt32(bModuleResourcesOnly));
            NWNX_PushArgumentString(NWNX_Util, sFunc, sRegexFilter);
            NWNX_PushArgumentInt(NWNX_Util, sFunc, nType);
            NWNX_CallFunction(NWNX_Util, sFunc);

            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Get the next resref.
        /// </summary>
        /// <returns>The next resref found or "" if none is found.</returns>
        public static string GetNextResRef()
        {
            string sFunc = "GetNextResRef";

            NWNX_CallFunction(NWNX_Util, sFunc);

            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Get the ticks per second of the server.
        /// Useful to dynamically detect lag and adjust behavior accordingly.
        /// </summary>
        /// <returns>The ticks per second.</returns>
        public static int GetServerTicksPerSecond()
        {
            string sFunc = "GetServerTicksPerSecond";

            NWNX_CallFunction(NWNX_Util, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Get the last created object.
        /// </summary>
        /// <param name="nObjectType">Does not take the NWScript OBJECT_TYPE_* constants. Use NWNX_Consts_TranslateNWScriptObjectType() to get their NWNX equivalent.</param>
        /// <param name="nNthLast">The nth last object created.</param>
        /// <returns>The last created object. On error, this returns OBJECT_INVALID.</returns>
        public static NWGameObject NWNX_Util_GetLastCreatedObject(int nObjectType, int nNthLast = 1)
        {
            string sFunc = "GetLastCreatedObject";

            NWNX_PushArgumentInt(NWNX_Util, sFunc, nNthLast);
            NWNX_PushArgumentInt(NWNX_Util, sFunc, nObjectType);
            NWNX_CallFunction(NWNX_Util, sFunc);

            return NWNX_GetReturnValueObject(NWNX_Util, sFunc);
        }


        /// <summary>
        /// Compiles and adds a script to the UserDirectory/nwnx folder.
        /// @Will override existing scripts that are in the module.
        /// </summary>
        /// <param name="sFileName">The script filename without extension, 16 or less characters</param>
        /// <param name="sScriptData">The script data to compile</param>
        /// <param name="bWrapIntoMain">Set to true to wrap sScriptData into void main(){}.</param>
        /// <returns>Empty string on success or the compilation error</returns>
        public static string AddScript(string sFileName, string sScriptData, bool bWrapIntoMain = false)
        {
            string sFunc = "AddScript";

            NWNX_PushArgumentInt(NWNX_Util, sFunc, bWrapIntoMain ? 1 : 0);
            NWNX_PushArgumentString(NWNX_Util, sFunc, sScriptData);
            NWNX_PushArgumentString(NWNX_Util, sFunc, sFileName);
            NWNX_CallFunction(NWNX_Util, sFunc);

            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }

        /// <summary>
        /// Gets the contents of a .nss script file as a string
        /// </summary>
        /// <param name="sScriptName">The name of the script to get the contents of.</param>
        /// <param name="nMaxLength">The max length of the return string, -1 to get everything</param>
        /// <returns>The script file contents or "" on error.</returns>
        public static string GetNSSContents(string sScriptName, int nMaxLength = -1)
        {
            string sFunc = "GetNSSContents";

            NWNX_PushArgumentInt(NWNX_Util, sFunc, nMaxLength);
            NWNX_PushArgumentString(NWNX_Util, sFunc, sScriptName);
            NWNX_CallFunction(NWNX_Util, sFunc);

            return NWNX_GetReturnValueString(NWNX_Util, sFunc);
        }
    }
}
