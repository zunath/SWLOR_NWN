using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the UtilPlugin for testing purposes.
    /// Provides utility functions including script management, hashing, time operations,
    /// and resource management.
    /// </summary>
    public class UtilPluginMock: IUtilPluginService
    {
        // Mock data storage
        private readonly List<string> _scriptCallStack = new();
        private readonly Dictionary<string, string> _customTokens = new();
        private readonly Dictionary<string, string> _environmentVariables = new();
        private readonly Dictionary<string, string> _resourceOverrides = new();
        private int _minutesPerHour = 60;
        private int _dawnHour = 6;
        private int _duskHour = 18;
        private uint _itemActivator = 0;
        private readonly Dictionary<string, bool> _scriptParams = new();
        private readonly List<string> _resRefList = new();
        private int _resRefIndex = 0;

        /// <summary>
        /// Gets the name of the currently executing script.
        /// </summary>
        /// <param name="depth">If depth is > 0, it will return the name of the script that called this one via ExecuteScript().</param>
        /// <returns>The name of the currently executing script.</returns>
        public string GetCurrentScriptName(int depth = 0)
        {
            if (depth < 0 || depth >= _scriptCallStack.Count)
                return string.Empty;
            
            return _scriptCallStack[_scriptCallStack.Count - 1 - depth];
        }

        /// <summary>
        /// Gets a string that contains the ascii table.
        /// </summary>
        /// <returns>A string that contains all characters at their position (e.g. 'A' at 65).</returns>
        public string GetAsciiTableString()
        {
            var asciiTable = new char[256];
            for (int i = 0; i < 256; i++)
            {
                asciiTable[i] = (char)i;
            }
            return new string(asciiTable);
        }

        /// <summary>
        /// Gets an integer hash of a string.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>The hashed string as an integer.</returns>
        public int Hash(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            
            int hash = 0;
            foreach (char c in str)
            {
                hash = (hash * 31) + c;
            }
            return Math.Abs(hash);
        }

        /// <summary>
        /// Gets the last modified timestamp (mtime) of the module file in seconds.
        /// </summary>
        /// <returns>The mtime of the module file.</returns>
        public int GetModuleMTime()
        {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// Gets the value of customTokenNumber.
        /// </summary>
        /// <param name="customTokenNumber">The token number to query.</param>
        /// <returns>The string representation of the token value.</returns>
        public string GetCustomToken(int customTokenNumber)
        {
            return _customTokens.TryGetValue(customTokenNumber.ToString(), out var value) ? value : string.Empty;
        }

        /// <summary>
        /// Convert an effect type to an itemproperty type.
        /// </summary>
        /// <param name="effect">The effect to convert to an itemproperty.</param>
        /// <returns>The converted itemproperty.</returns>
        public ItemProperty EffectToItemProperty(Effect effect)
        {
            // Mock conversion - return a new ItemProperty with a mock handle
            return new ItemProperty(1000 + effect.Handle);
        }

        /// <summary>
        /// Convert an itemproperty type to an effect type.
        /// </summary>
        /// <param name="ip">The itemproperty to convert to an effect.</param>
        /// <returns>The converted effect.</returns>
        public Effect ItemPropertyToEffect(ItemProperty ip)
        {
            // Mock conversion - return a new Effect with a mock handle
            return new Effect(2000 + ip.Handle);
        }

        /// <summary>
        /// Strip any color codes from a string.
        /// </summary>
        /// <param name="str">The string to strip of color.</param>
        /// <returns>The new string without any color codes.</returns>
        public string StripColors(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            
            // Simple color code removal - remove patterns like <c>text</c>
            return System.Text.RegularExpressions.Regex.Replace(str, @"<c[^>]*>.*?</c>", string.Empty);
        }

        /// <summary>
        /// Retrieves an environment variable.
        /// </summary>
        /// <param name="varname">The environment variable to query.</param>
        /// <returns>The value of the environment variable.</returns>
        public string GetEnvironmentVariable(string varname)
        {
            return _environmentVariables.TryGetValue(varname, out var value) ? value : string.Empty;
        }

        /// <summary>
        /// Gets the module real life minutes per in game hour.
        /// </summary>
        /// <returns>The minutes per hour.</returns>
        public int GetMinutesPerHour()
        {
            return _minutesPerHour;
        }

        /// <summary>
        /// Set module real life minutes per in game hour.
        /// </summary>
        /// <param name="minutes">The minutes per hour.</param>
        public void SetMinutesPerHour(int minutes)
        {
            _minutesPerHour = Math.Max(1, minutes);
        }

        /// <summary>
        /// Encodes a string for usage in a URL.
        /// </summary>
        /// <param name="url">The string to encode for a URL.</param>
        /// <returns>The url encoded string.</returns>
        public string EncodeStringForURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;
            
            return Uri.EscapeDataString(url);
        }

        /// <summary>
        /// Get the first resref of nType.
        /// </summary>
        /// <param name="type">A Resref Type.</param>
        /// <param name="regexFilter">Lets you filter out resrefs using a regexfilter.</param>
        /// <param name="moduleResourcesOnly">If TRUE only custom resources will be returned.</param>
        /// <returns>The first resref found or "" if none is found.</returns>
        public string GetFirstResRef(ResRefType type, string regexFilter = "", bool moduleResourcesOnly = true)
        {
            _resRefList.Clear();
            _resRefIndex = 0;
            
            // Mock some sample resrefs
            _resRefList.AddRange(new[]
            {
                "test_script",
                "sample_item",
                "mock_creature",
                "example_area"
            });
            
            return _resRefList.Count > 0 ? _resRefList[0] : string.Empty;
        }

        /// <summary>
        /// Get the next resref.
        /// </summary>
        /// <returns>The next resref found or "" if none is found.</returns>
        public string GetNextResRef()
        {
            _resRefIndex++;
            return _resRefIndex < _resRefList.Count ? _resRefList[_resRefIndex] : string.Empty;
        }

        /// <summary>
        /// Get the world time as calendar day and time of day.
        /// </summary>
        /// <param name="fAdjustment">An adjustment in seconds, 0.0f will return the current world time, positive or negative values will return a world time in the future or past.</param>
        /// <returns>A WorldTime struct with the calendar day and time of day.</returns>
        public UtilPluginService.WorldTime GetWorldTime(float fAdjustment = 0.0f)
        {
            var now = DateTime.UtcNow.AddSeconds(fAdjustment);
            var calendarDay = (int)(now - new DateTime(1970, 1, 1)).TotalDays;
            var timeOfDay = now.Hour * 100 + now.Minute; // Format as HHMM
            
            return new UtilPluginService.WorldTime
            {
                TimeOfDay = timeOfDay,
                CalendarDay = calendarDay
            };
        }

        /// <summary>
        /// Set a server-side resource override.
        /// </summary>
        /// <param name="nResType">A Resref Type.</param>
        /// <param name="sOldName">The old resource name, 16 characters or less.</param>
        /// <param name="sNewName">The new resource name or "" to clear a previous override, 16 characters or less.</param>
        public void SetResourceOverride(int nResType, string sOldName, string sNewName)
        {
            var key = $"{nResType}:{sOldName}";
            if (string.IsNullOrEmpty(sNewName))
            {
                _resourceOverrides.Remove(key);
            }
            else
            {
                _resourceOverrides[key] = sNewName;
            }
        }

        /// <summary>
        /// Get a server-side resource override.
        /// </summary>
        /// <param name="nResType">A Resref Type.</param>
        /// <param name="sName">The name of the resource, 16 characters or less.</param>
        /// <returns>The resource override, or "" if one is not set.</returns>
        public string GetResourceOverride(int nResType, string sName)
        {
            var key = $"{nResType}:{sName}";
            return _resourceOverrides.TryGetValue(key, out var value) ? value : string.Empty;
        }

        /// <summary>
        /// Create a door.
        /// </summary>
        /// <param name="sResRef">The ResRef of the door.</param>
        /// <param name="locLocation">The location to create the door at.</param>
        /// <param name="sNewTag">An optional new tag for the door.</param>
        /// <returns>The door, or OBJECT_INVALID on failure.</returns>
        public uint CreateDoor(string sResRef, Location locLocation, string sNewTag)
        {
            // Mock door creation - return a mock object ID
            return 1000 + (uint)Hash(sResRef + locLocation.ToString() + sNewTag);
        }

        /// <summary>
        /// Set the object that will be returned by GetItemActivator.
        /// </summary>
        /// <param name="oObject">An object.</param>
        public void SetItemActivator(uint oObject)
        {
            _itemActivator = oObject;
        }

        /// <summary>
        /// Get if a script param is set.
        /// </summary>
        /// <param name="paramName">The script parameter name to check.</param>
        /// <returns>true if script param is set, false if not or on error.</returns>
        public bool GetScriptParamIsSet(string paramName)
        {
            return _scriptParams.TryGetValue(paramName, out var isSet) && isSet;
        }

        /// <summary>
        /// Set the module dawn hour.
        /// </summary>
        /// <param name="nDawnHour">The new dawn hour</param>
        public void SetDawnHour(int nDawnHour)
        {
            _dawnHour = Math.Max(0, Math.Min(23, nDawnHour));
        }

        /// <summary>
        /// Set the module dusk hour.
        /// </summary>
        /// <param name="nDuskHour">The new dusk hour</param>
        public void SetDuskHour(int nDuskHour)
        {
            _duskHour = Math.Max(0, Math.Min(23, nDuskHour));
        }

        /// <summary>
        /// Returns the number of microseconds since midnight on January 1, 1970.
        /// </summary>
        /// <returns>A HighResTimestamp struct with seconds and microseconds.</returns>
        public UtilPluginService.HighResTimestamp GetHighResTimeStamp()
        {
            var now = DateTime.UtcNow;
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var elapsed = now - epoch;
            
            return new UtilPluginService.HighResTimestamp
            {
                Microseconds = (int)((elapsed.TotalMilliseconds % 1000) * 1000),
                Seconds = (int)elapsed.TotalSeconds
            };
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _scriptCallStack.Clear();
            _customTokens.Clear();
            _environmentVariables.Clear();
            _resourceOverrides.Clear();
            _minutesPerHour = 60;
            _dawnHour = 6;
            _duskHour = 18;
            _itemActivator = 0;
            _scriptParams.Clear();
            _resRefList.Clear();
            _resRefIndex = 0;
        }

        /// <summary>
        /// Sets a custom token for testing.
        /// </summary>
        /// <param name="tokenNumber">The token number.</param>
        /// <param name="value">The token value.</param>
        public void SetCustomToken(int tokenNumber, string value)
        {
            _customTokens[tokenNumber.ToString()] = value;
        }

        /// <summary>
        /// Sets an environment variable for testing.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The variable value.</param>
        public void SetEnvironmentVariable(string name, string value)
        {
            _environmentVariables[name] = value;
        }

        /// <summary>
        /// Sets a script parameter for testing.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <param name="isSet">Whether the parameter is set.</param>
        public void SetScriptParam(string paramName, bool isSet)
        {
            _scriptParams[paramName] = isSet;
        }

        /// <summary>
        /// Pushes a script name onto the call stack for testing.
        /// </summary>
        /// <param name="scriptName">The script name.</param>
        public void PushScript(string scriptName)
        {
            _scriptCallStack.Add(scriptName);
        }

        /// <summary>
        /// Pops a script name from the call stack for testing.
        /// </summary>
        public void PopScript()
        {
            if (_scriptCallStack.Count > 0)
            {
                _scriptCallStack.RemoveAt(_scriptCallStack.Count - 1);
            }
        }

        /// <summary>
        /// Gets all util data for testing verification.
        /// </summary>
        /// <returns>A UtilData object containing all settings.</returns>
        public UtilData GetUtilDataForTesting()
        {
            return new UtilData
            {
                ScriptCallStack = new List<string>(_scriptCallStack),
                CustomTokens = new Dictionary<string, string>(_customTokens),
                EnvironmentVariables = new Dictionary<string, string>(_environmentVariables),
                ResourceOverrides = new Dictionary<string, string>(_resourceOverrides),
                MinutesPerHour = _minutesPerHour,
                DawnHour = _dawnHour,
                DuskHour = _duskHour,
                ItemActivator = _itemActivator,
                ScriptParams = new Dictionary<string, bool>(_scriptParams),
                ResRefList = new List<string>(_resRefList),
                ResRefIndex = _resRefIndex
            };
        }

        // Helper classes
        public class UtilData
        {
            public List<string> ScriptCallStack { get; set; } = new();
            public Dictionary<string, string> CustomTokens { get; set; } = new();
            public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
            public Dictionary<string, string> ResourceOverrides { get; set; } = new();
            public int MinutesPerHour { get; set; }
            public int DawnHour { get; set; }
            public int DuskHour { get; set; }
            public uint ItemActivator { get; set; }
            public Dictionary<string, bool> ScriptParams { get; set; } = new();
            public List<string> ResRefList { get; set; } = new();
            public int ResRefIndex { get; set; }
        }

    }
}
