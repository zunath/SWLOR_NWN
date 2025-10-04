using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX;

public interface IUtilPluginService
{
    /// <summary>
    /// Gets the name of the currently executing script.
    /// </summary>
    /// <param name="depth">If depth is > 0, it will return the name of the script that called this one via ExecuteScript().</param>
    /// <returns>The name of the currently executing script.</returns>
    string GetCurrentScriptName(int depth = 0);

    /// <summary>
    /// Gets a string that contains the ascii table.
    /// </summary>
    /// <remarks>The character at index 0 is a space.</remarks>
    /// <returns>A string that contains all characters at their position (e.g. 'A' at 65).</returns>
    string GetAsciiTableString();

    /// <summary>
    /// Gets an integer hash of a string.
    /// </summary>
    /// <param name="str">The string to hash.</param>
    /// <returns>The hashed string as an integer.</returns>
    int Hash(string str);

    /// <summary>
    /// Gets the last modified timestamp (mtime) of the module file in seconds.
    /// </summary>
    /// <returns>The mtime of the module file.</returns>
    int GetModuleMTime();

    /// <summary>
    /// Gets the value of customTokenNumber.
    /// </summary>
    /// <param name="customTokenNumber">The token number to query.</param>
    /// <returns>The string representation of the token value.</returns>
    string GetCustomToken(int customTokenNumber);

    /// <summary>
    /// Convert an effect type to an itemproperty type.
    /// </summary>
    /// <param name="effect">The effect to convert to an itemproperty.</param>
    /// <returns>The converted itemproperty.</returns>
    ItemProperty EffectToItemProperty(Effect effect);

    /// <summary>
    /// Convert an itemproperty type to an effect type.
    /// </summary>
    /// <param name="ip">The itemproperty to convert to an effect.</param>
    /// <returns>The converted effect.</returns>
    Effect ItemPropertyToEffect(ItemProperty ip);

    /// <summary>
    /// Strip any color codes from a string.
    /// </summary>
    /// <param name="str">The string to strip of color.</param>
    /// <returns>The new string without any color codes.</returns>
    string StripColors(string str);

    /// <summary>
    /// Retrieves an environment variable.
    /// </summary>
    /// <param name="varname">The environment variable to query.</param>
    /// <returns>The value of the environment variable.</returns>
    string GetEnvironmentVariable(string varname);

    /// <summary>
    /// Gets the module real life minutes per in game hour.
    /// </summary>
    /// <returns>The minutes per hour.</returns>
    int GetMinutesPerHour();

    /// <summary>
    /// Set module real life minutes per in game hour.
    /// </summary>
    /// <param name="minutes">The minutes per hour.</param>
    void SetMinutesPerHour(int minutes);

    /// <summary>
    /// Encodes a string for usage in a URL.
    /// </summary>
    /// <param name="url">The string to encode for a URL.</param>
    /// <returns>The url encoded string.</returns>
    string EncodeStringForURL(string url);

    /// <summary>
    /// Get the first resref of nType.
    /// </summary>
    /// <param name="type">A Resref Type.</param>
    /// <param name="regexFilter">Lets you filter out resrefs using a regexfilter. For example: **nwnx_.*** gets you all scripts prefixed with nwnx_ when using the NSS resref type.</param>
    /// <param name="moduleResourcesOnly">If TRUE only custom resources will be returned.</param>
    /// <returns>The first resref found or "" if none is found.</returns>
    string GetFirstResRef(ResRefType type, string regexFilter = "", bool moduleResourcesOnly = true);

    /// <summary>
    /// Get the next resref.
    /// </summary>
    /// <returns>The next resref found or "" if none is found.</returns>
    string GetNextResRef();

    /// <summary>
    /// Get the world time as calendar day and time of day.
    /// </summary>
    /// <remarks>This function is useful for calculating effect expiry times.</remarks>
    /// <param name="fAdjustment">An adjustment in seconds, 0.0f will return the current world time, positive or negative values will return a world time in the future or past.</param>
    /// <returns>A WorldTime struct with the calendar day and time of day.</returns>
    UtilPluginService.WorldTime GetWorldTime(float fAdjustment = 0.0f);

    /// <summary>
    /// Set a server-side resource override.
    /// </summary>
    /// <param name="nResType">A Resref Type.</param>
    /// <param name="sOldName">The old resource name, 16 characters or less.</param>
    /// <param name="sNewName">The new resource name or "" to clear a previous override, 16 characters or less.</param>
    void SetResourceOverride(int nResType, string sOldName, string sNewName);

    /// <summary>
    /// Get a server-side resource override.
    /// </summary>
    /// <param name="nResType">A Resref Type.</param>
    /// <param name="sName">The name of the resource, 16 characters or less.</param>
    /// <returns>The resource override, or "" if one is not set.</returns>
    string GetResourceOverride(int nResType, string sName);

    /// <summary>
    /// Create a door.
    /// </summary>
    /// <param name="sResRef">The ResRef of the door.</param>
    /// <param name="locLocation">The location to create the door at.</param>
    /// <param name="sNewTag">An optional new tag for the door.</param>
    /// <returns>The door, or OBJECT_INVALID on failure.</returns>
    uint CreateDoor(string sResRef, Location locLocation, string sNewTag);

    /// <summary>
    /// Set the object that will be returned by GetItemActivator.
    /// </summary>
    /// <param name="oObject">An object.</param>
    void SetItemActivator(uint oObject);

    /// <summary>
    /// Get if a script param is set.
    /// </summary>
    /// <param name="paramName">The script parameter name to check.</param>
    /// <returns>true if script param is set, false if not or on error.</returns>
    bool GetScriptParamIsSet(string paramName);

    /// <summary>
    /// Set the module dawn hour.
    /// </summary>
    /// <param name="nDawnHour">The new dawn hour</param>
    void SetDawnHour(int nDawnHour);

    /// <summary>
    /// Set the module dusk hour.
    /// </summary>
    /// <param name="nDuskHour">The new dusk hour</param>
    void SetDuskHour(int nDuskHour);

    /// <summary>
    /// Returns the number of microseconds since midnight on January 1, 1970.
    /// </summary>
    /// <returns>A HighResTimestamp struct with seconds and microseconds.</returns>
    UtilPluginService.HighResTimestamp GetHighResTimeStamp();
}