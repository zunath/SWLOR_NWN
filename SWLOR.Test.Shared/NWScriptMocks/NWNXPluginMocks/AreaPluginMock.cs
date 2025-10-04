using System.Numerics;
using NWN.Core.NWNX;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the AreaPlugin for testing purposes.
    /// Provides comprehensive area management functionality including player tracking, area properties,
    /// environmental settings, tile management, and area export capabilities.
    /// </summary>
    public class AreaPluginMock: IAreaPluginService
    {
        // Mock data storage
        private readonly Dictionary<uint, AreaData> _areaData = new();
        private readonly Dictionary<uint, List<uint>> _areaPlayers = new();
        private readonly Dictionary<uint, uint> _lastEntered = new();
        private readonly Dictionary<uint, uint> _lastLeft = new();
        private readonly Dictionary<uint, List<uint>> _exclusionList = new();

        /// <summary>
        /// Retrieves the current number of player characters currently in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The number of player characters currently in the area. Returns 0 if no players are present.</returns>
        public int GetNumberOfPlayersInArea(uint area)
        {
            return _areaPlayers.TryGetValue(area, out var players) ? players.Count : 0;
        }

        /// <summary>
        /// Retrieves the creature object that most recently entered the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The object ID of the most recent creature to enter the area. Returns OBJECT_INVALID if no creature has entered.</returns>
        public uint GetLastEntered(uint area)
        {
            return _lastEntered.TryGetValue(area, out var lastEntered) ? lastEntered : OBJECT_INVALID;
        }

        /// <summary>
        /// Retrieves the creature object that most recently left the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The object ID of the most recent creature to leave the area. Returns OBJECT_INVALID if no creature has left.</returns>
        public uint GetLastLeft(uint area)
        {
            return _lastLeft.TryGetValue(area, out var lastLeft) ? lastLeft : OBJECT_INVALID;
        }

        /// <summary>
        /// Retrieves the current PvP (Player versus Player) setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The current PvP setting for the area. See PvPSetting enum for possible values.</returns>
        public PvPSettingType GetPVPSetting(uint area)
        {
            return GetAreaData(area).PvPSetting;
        }

        /// <summary>
        /// Sets the PvP (Player versus Player) setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="pvpSetting">The new PvP setting to apply. See PvPSetting enum for available options.</param>
        public void SetPVPSetting(uint area, PvPSettingType pvpSetting)
        {
            GetAreaData(area).PvPSetting = pvpSetting;
        }

        /// <summary>
        /// Retrieves the current Spot skill modifier applied to all creatures in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The Spot skill modifier value. Positive values increase the skill, negative values decrease it.</returns>
        public int GetAreaSpotModifier(uint area)
        {
            return GetAreaData(area).SpotModifier;
        }

        /// <summary>
        /// Sets the Spot skill modifier applied to all creatures in the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="spotModifier">The Spot skill modifier to apply. Positive values increase the skill, negative values decrease it.</param>
        public void SetAreaSpotModifier(uint area, int spotModifier)
        {
            GetAreaData(area).SpotModifier = spotModifier;
        }

        /// <summary>
        /// Retrieves the current Listen skill modifier applied to all creatures in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The Listen skill modifier value. Positive values increase the skill, negative values decrease it.</returns>
        public int GetAreaListenModifier(uint area)
        {
            return GetAreaData(area).ListenModifier;
        }

        /// <summary>
        /// Sets the Listen skill modifier applied to all creatures in the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="listenModifier">The Listen skill modifier to apply. Positive values increase the skill, negative values decrease it.</param>
        public void SetAreaListenModifier(uint area, int listenModifier)
        {
            GetAreaData(area).ListenModifier = listenModifier;
        }

        /// <summary>
        /// Checks whether resting is allowed in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>True if resting is disabled in the area, false if resting is allowed.</returns>
        public bool GetNoRestingAllowed(uint area)
        {
            return GetAreaData(area).NoRestingAllowed;
        }

        /// <summary>
        /// Sets whether resting is allowed in the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="bNoRestingAllowed">True to disable resting in the area, false to allow resting.</param>
        public void SetNoRestingAllowed(uint area, bool bNoRestingAllowed)
        {
            GetAreaData(area).NoRestingAllowed = bNoRestingAllowed;
        }

        /// <summary>
        /// Retrieves the current wind power setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The wind power level for the area. Valid values are 0 (no wind), 1 (light wind), or 2 (strong wind).</returns>
        public int GetWindPower(uint area)
        {
            return GetAreaData(area).WindPower;
        }

        /// <summary>
        /// Sets the wind power level for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="windPower">The wind power level to set. Valid values are 0 (no wind), 1 (light wind), or 2 (strong wind).</param>
        public void SetWindPower(uint area, int windPower)
        {
            GetAreaData(area).WindPower = Math.Max(0, Math.Min(2, windPower));
        }

        /// <summary>
        /// Retrieves the percentage chance for a specific weather effect to occur in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <param name="type">The weather effect type to query. See WeatherEffectType enum for available options.</param>
        /// <returns>The percentage chance (0-100) for the specified weather effect to occur.</returns>
        public int GetWeatherChance(uint area, WeatherEffectType type)
        {
            return GetAreaData(area).WeatherChances.TryGetValue(type, out var chance) ? chance : 0;
        }

        /// <summary>
        /// Sets the percentage chance for a specific weather effect to occur in the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="type">The weather effect type to modify. See WeatherEffectType enum for available options.</param>
        /// <param name="chance">The percentage chance (0-100) for the weather effect to occur.</param>
        public void SetWeatherChance(uint area, WeatherEffectType type, int chance)
        {
            GetAreaData(area).WeatherChances[type] = Math.Max(0, Math.Min(100, chance));
        }

        /// <summary>
        /// Retrieves the current fog clip distance setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The fog clip distance in game units. Objects beyond this distance are not rendered.</returns>
        public float GetFogClipDistance(uint area)
        {
            return GetAreaData(area).FogClipDistance;
        }

        /// <summary>
        /// Sets the fog clip distance for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="distance">The new fog clip distance in game units. Objects beyond this distance will not be rendered.</param>
        public void SetFogClipDistance(uint area, float distance)
        {
            GetAreaData(area).FogClipDistance = Math.Max(0, distance);
        }

        /// <summary>
        /// Retrieves the current shadow opacity setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The shadow opacity value for the area. Valid range is 0-100.</returns>
        public int GetShadowOpacity(uint area)
        {
            return GetAreaData(area).ShadowOpacity;
        }

        /// <summary>
        /// Sets the shadow opacity for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="shadowOpacity">The shadow opacity to set for the area. Valid range is 0-100.</param>
        public void SetShadowOpacity(uint area, int shadowOpacity)
        {
            GetAreaData(area).ShadowOpacity = Math.Max(0, Math.Min(100, shadowOpacity));
        }

        /// <summary>
        /// Retrieves the current day/night cycle setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The current day/night cycle setting. See DayNightCycle enum for possible values.</returns>
        public AreaDayNightCycleType GetDayNightCycle(uint area)
        {
            return GetAreaData(area).DayNightCycle;
        }

        /// <summary>
        /// Sets the day/night cycle for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="type">The day/night cycle setting to apply. See DayNightCycle enum for available options.</param>
        public void SetDayNightCycle(uint area, AreaDayNightCycleType type)
        {
            GetAreaData(area).DayNightCycle = type;
        }

        /// <summary>
        /// Retrieves the current sun/moon ambient or diffuse color setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <param name="type">The sun/moon color type to query. See AreaLightColorType enum for available options.</param>
        /// <returns>The color value as a FOG_COLOR_* constant or custom hex value. Returns -1 on error.</returns>
        public int GetSunMoonColors(uint area, AreaLightColorType type)
        {
            return GetAreaData(area).SunMoonColors.TryGetValue(type, out var color) ? color : -1;
        }

        /// <summary>
        /// Sets the sun/moon ambient or diffuse color for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="type">The sun/moon color type to modify. See AreaLightColorType enum for available options.</param>
        /// <param name="color">The color value to set. Can be a FOG_COLOR_* constant or custom hex RGB value.</param>
        public void SetSunMoonColors(uint area, AreaLightColorType type, int color)
        {
            GetAreaData(area).SunMoonColors[type] = color;
        }

        /// <summary>
        /// Creates a transition object (square-shaped trigger) at the specified location in the area.
        /// </summary>
        /// <param name="area">The area object where the transition will be created. Must be a valid area object.</param>
        /// <param name="target">The target object for the transition. Must be a door or waypoint object.</param>
        /// <param name="x">The X coordinate where the transition will be created.</param>
        /// <param name="y">The Y coordinate where the transition will be created.</param>
        /// <param name="z">The Z coordinate where the transition will be created.</param>
        /// <param name="size">The size of the square transition area. Default is 2.0f.</param>
        /// <param name="tag">Optional tag to assign to the created transition object. Default is empty string.</param>
        /// <returns>The object ID of the created transition. Returns OBJECT_INVALID on failure.</returns>
        public uint CreateTransition(uint area, uint target, float x, float y, float z, float size = 2.0f, string tag = "")
        {
            // Mock implementation - returns a mock object ID
            var transitionId = (uint)(0x7F000000 + _areaData.Count + 1000);
            return transitionId;
        }

        /// <summary>
        /// Retrieves the current state of a specific tile animation loop in the area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <param name="tileX">The X coordinate of the tile to check.</param>
        /// <param name="tileY">The Y coordinate of the tile to check.</param>
        /// <param name="animLoop">The animation loop to check. Valid values are 1-3.</param>
        /// <returns>True if the specified animation loop is enabled, false if disabled.</returns>
        public bool GetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop)
        {
            var areaData = GetAreaData(area);
            var key = $"{tileX},{tileY},{animLoop}";
            return areaData.TileAnimationLoops.TryGetValue(key, out var enabled) && enabled;
        }

        /// <summary>
        /// Sets the state of a specific tile animation loop in the area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="tileX">The X coordinate of the tile to modify.</param>
        /// <param name="tileY">The Y coordinate of the tile to modify.</param>
        /// <param name="animLoop">The animation loop to modify. Valid values are 1-3.</param>
        /// <param name="enabled">True to enable the animation loop, false to disable it.</param>
        public void SetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop, bool enabled)
        {
            var areaData = GetAreaData(area);
            var key = $"{tileX},{tileY},{animLoop}";
            areaData.TileAnimationLoops[key] = enabled;
        }

        /// <summary>
        /// Creates a generic trigger object (square-shaped) at the specified location in the area.
        /// </summary>
        /// <param name="area">The area object where the trigger will be created. Must be a valid area object.</param>
        /// <param name="x">The X coordinate where the trigger will be created.</param>
        /// <param name="y">The Y coordinate where the trigger will be created.</param>
        /// <param name="z">The Z coordinate where the trigger will be created.</param>
        /// <param name="tag">Optional tag to assign to the created trigger object. Default is empty string.</param>
        /// <param name="size">The size of the square trigger area. Default is 1.0f.</param>
        /// <returns>The object ID of the created trigger. Returns OBJECT_INVALID on failure.</returns>
        public uint CreateGenericTrigger(uint area, float x, float y, float z, string tag = "", float size = 1.0f)
        {
            // Mock implementation - returns a mock object ID
            var triggerId = (uint)(0x7F000000 + _areaData.Count + 2000);
            return triggerId;
        }

        /// <summary>
        /// Adds an object to the ExportGIT exclusion list, preventing it from being exported.
        /// </summary>
        /// <param name="oObject">The object to add to the exclusion list. Must be a valid object.</param>
        public void AddObjectToExclusionList(uint oObject)
        {
            if (!_exclusionList.ContainsKey(0)) // Global exclusion list
            {
                _exclusionList[0] = new List<uint>();
            }
            
            if (!_exclusionList[0].Contains(oObject))
            {
                _exclusionList[0].Add(oObject);
            }
        }

        /// <summary>
        /// Removes an object from the ExportGIT exclusion list, allowing it to be exported again.
        /// </summary>
        /// <param name="oObject">The object to remove from the exclusion list. Must be a valid object.</param>
        public void RemoveObjectFromExclusionList(uint oObject)
        {
            if (_exclusionList.TryGetValue(0, out var exclusionList))
            {
                exclusionList.Remove(oObject);
            }
        }

        /// <summary>
        /// Exports the Game Instance Template (GIT) of the specified area to a file.
        /// </summary>
        /// <param name="oArea">The area object to export. Must be a valid area object.</param>
        /// <param name="sFileName">The filename for the exported .git file. Must be 16 characters or less. If empty, uses the area's resref.</param>
        /// <param name="bExportVarTable">If true, includes local variables set on the area in the export.</param>
        /// <param name="bExportUUID">If true, includes the area's UUID in the export if it has one.</param>
        /// <param name="nObjectFilter">Bitmask of OBJECT_TYPE_* constants. Objects of these types will not be exported.</param>
        /// <param name="sAlias">The resource directory alias where the .git file will be saved. Default is "NWNX".</param>
        /// <returns>True if the export was successful, false if it failed.</returns>
        public bool ExportGIT(uint oArea, string sFileName = "", bool bExportVarTable = true, bool bExportUUID = true, int nObjectFilter = 0, string sAlias = "NWNX")
        {
            // Mock implementation - always returns true for testing
            return true;
        }

        /// <summary>
        /// Retrieves detailed information about a specific tile in the area.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <param name="fTileX">The X coordinate of the tile to examine.</param>
        /// <param name="fTileY">The Y coordinate of the tile to examine.</param>
        /// <returns>A TileInfo struct containing detailed information about the tile.</returns>
        public TileInfo GetTileInfo(uint oArea, float fTileX, float fTileY)
        {
            // Mock implementation - returns default tile info
            return new TileInfo();
        }

        /// <summary>
        /// Exports the .are file of the specified area to a file.
        /// </summary>
        /// <param name="oArea">The area object to export. Must be a valid area object.</param>
        /// <param name="sFileName">The filename for the exported .are file. Must be 16 characters or less and lowercase. This becomes the area's resref.</param>
        /// <param name="sNewName">Optional new name for the area. If empty, uses the current area name.</param>
        /// <param name="sNewTag">Optional new tag for the area. If empty, uses the current area tag.</param>
        /// <param name="sAlias">The resource directory alias where the .are file will be saved. Default is "NWNX".</param>
        /// <returns>True if the export was successful, false if it failed.</returns>
        public bool ExportARE(uint oArea, string sFileName, string sNewName = "", string sNewTag = "", string sAlias = "NWNX")
        {
            // Mock implementation - always returns true for testing
            return true;
        }

        /// <summary>
        /// Retrieves the ambient sound that plays in the area during daytime.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The ambient sound track index. See ambientsound.2da for available sound tracks.</returns>
        public int GetAmbientSoundDay(uint oArea)
        {
            return GetAreaData(oArea).AmbientSoundDay;
        }

        /// <summary>
        /// Retrieves the ambient sound that plays in the area during nighttime.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The ambient sound track index. See ambientsound.2da for available sound tracks.</returns>
        public int GetAmbientSoundNight(uint oArea)
        {
            return GetAreaData(oArea).AmbientSoundNight;
        }

        /// <summary>
        /// Retrieves the volume level of the ambient sound that plays in the area during daytime.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The volume level of the daytime ambient sound.</returns>
        public int GetAmbientSoundDayVolume(uint oArea)
        {
            return GetAreaData(oArea).AmbientSoundDayVolume;
        }

        /// <summary>
        /// Retrieves the volume level of the ambient sound that plays in the area during nighttime.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The volume level of the nighttime ambient sound.</returns>
        public int GetAmbientSoundNightVolume(uint oArea)
        {
            return GetAreaData(oArea).AmbientSoundNightVolume;
        }

        /// <summary>
        /// Creates a sound object at the specified position in the area.
        /// </summary>
        /// <param name="oArea">The area object where the sound will be created. Must be a valid area object.</param>
        /// <param name="vPosition">The 3D position where the sound object will be created.</param>
        /// <param name="sResRef">The resource reference of the sound file to play. Must be a valid sound resource.</param>
        /// <returns>The object ID of the created sound object. Returns OBJECT_INVALID on failure.</returns>
        public uint CreateSoundObject(uint oArea, Vector3 vPosition, string sResRef)
        {
            // Mock implementation - returns a mock object ID
            var soundId = (uint)(0x7F000000 + _areaData.Count + 3000);
            return soundId;
        }

        /// <summary>
        /// Rotates the entire area by the specified number of 90-degree increments.
        /// </summary>
        /// <param name="oArea">The area object to rotate. Must be a valid area object.</param>
        /// <param name="nRotation">How many 90 degrees clockwise to rotate (1-3).</param>
        public void RotateArea(uint oArea, int nRotation)
        {
            // Mock implementation - in real tests, this would rotate the area
        }

        /// <summary>
        /// Retrieves detailed information about a tile by its index in the area.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <param name="nTileIndex">The index of the tile to examine. Must be within the area's tile range.</param>
        /// <returns>A TileInfo struct containing detailed information about the tile.</returns>
        public TileInfo GetTileInfoByTileIndex(uint oArea, int nTileIndex)
        {
            // Mock implementation - returns default tile info
            return new TileInfo();
        }

        /// <summary>
        /// Checks if a path exists between two points in the area.
        /// </summary>
        /// <param name="oArea">The area object to test in. Must be a valid area object.</param>
        /// <param name="vStart">The starting position for the path test.</param>
        /// <param name="vEnd">The ending position for the path test.</param>
        /// <param name="nMaxDepth">The max depth of the DFS tree. A good value is AreaWidth * AreaHeight.</param>
        /// <returns>True if a path exists between the points, false if no path is available.</returns>
        public bool GetPathExists(uint oArea, Vector3 vStart, Vector3 vEnd, int nMaxDepth)
        {
            // Mock implementation - always returns true for testing
            return true;
        }

        /// <summary>
        /// Retrieves the current area flags for the specified area.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The area flags as a bitmask. See AREA_FLAG_* constants for flag meanings.</returns>
        public int GetAreaFlags(uint oArea)
        {
            return GetAreaData(oArea).AreaFlags;
        }

        /// <summary>
        /// Sets the area flags for the specified area.
        /// </summary>
        /// <param name="oArea">The area object to modify. Must be a valid area object.</param>
        /// <param name="nFlags">The area flags to set as a bitmask. See AREA_FLAG_* constants for flag meanings.</param>
        public void SetAreaFlags(uint oArea, int nFlags)
        {
            GetAreaData(oArea).AreaFlags = nFlags;
        }

        /// <summary>
        /// Retrieves the current wind settings for the specified area.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>An AreaWind struct containing the wind direction, magnitude, and other wind properties.</returns>
        public AreaWind GetAreaWind(uint oArea)
        {
            return GetAreaData(oArea).Wind;
        }

        /// <summary>
        /// Sets the default object UI discovery mask for the specified area.
        /// </summary>
        /// <param name="oArea">The area object to modify, or OBJECT_INVALID to set a global mask for all areas. Per area masks will override the global mask.</param>
        /// <param name="nObjectTypes">A mask of OBJECT_TYPE_* constants or OBJECT_TYPE_ALL for all suitable object types. Currently only works on Creatures, Doors (Hilite only), Items and Useable Placeables.</param>
        /// <param name="nMask">The UI discovery mask to set. See OBJECT_UI_DISCOVERY_* constants for mask values.</param>
        /// <param name="bForceUpdate">If true, will update the discovery mask of ALL objects in the area or module (if oArea == OBJECT_INVALID), according to the current mask. Use with care.</param>
        public void SetDefaultObjectUiDiscoveryMask(uint oArea, int nObjectTypes, int nMask, bool bForceUpdate = false)
        {
            // Mock implementation - in real tests, this would set the UI discovery mask
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _areaData.Clear();
            _areaPlayers.Clear();
            _lastEntered.Clear();
            _lastLeft.Clear();
            _exclusionList.Clear();
        }

        /// <summary>
        /// Simulates a player entering an area for testing.
        /// </summary>
        /// <param name="area">The area the player is entering.</param>
        /// <param name="player">The player object entering the area.</param>
        public void SimulatePlayerEnter(uint area, uint player)
        {
            if (!_areaPlayers.ContainsKey(area))
            {
                _areaPlayers[area] = new List<uint>();
            }
            
            if (!_areaPlayers[area].Contains(player))
            {
                _areaPlayers[area].Add(player);
            }
            
            _lastEntered[area] = player;
        }

        /// <summary>
        /// Simulates a player leaving an area for testing.
        /// </summary>
        /// <param name="area">The area the player is leaving.</param>
        /// <param name="player">The player object leaving the area.</param>
        public void SimulatePlayerLeave(uint area, uint player)
        {
            if (_areaPlayers.TryGetValue(area, out var players))
            {
                players.Remove(player);
            }
            
            _lastLeft[area] = player;
        }

        /// <summary>
        /// Gets the area data for the specified area, creating it if it doesn't exist.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The area data for the specified area.</returns>
        private AreaData GetAreaData(uint area)
        {
            if (!_areaData.TryGetValue(area, out var data))
            {
                data = new AreaData();
                _areaData[area] = data;
            }
            return data;
        }

        // Constants
        private const uint OBJECT_INVALID = 0x7F000000;

        // Helper classes
        private class AreaData
        {
            public PvPSettingType PvPSetting { get; set; } = PvPSettingType.FullPvP;
            public int SpotModifier { get; set; } = 0;
            public int ListenModifier { get; set; } = 0;
            public bool NoRestingAllowed { get; set; } = false;
            public int WindPower { get; set; } = 0;
            public Dictionary<WeatherEffectType, int> WeatherChances { get; set; } = new();
            public float FogClipDistance { get; set; } = 100.0f;
            public int ShadowOpacity { get; set; } = 50;
            public AreaDayNightCycleType DayNightCycle { get; set; } = AreaDayNightCycleType.AlwaysBright;
            public Dictionary<AreaLightColorType, int> SunMoonColors { get; set; } = new();
            public Dictionary<string, bool> TileAnimationLoops { get; set; } = new();
            public int AmbientSoundDay { get; set; } = 0;
            public int AmbientSoundNight { get; set; } = 0;
            public int AmbientSoundDayVolume { get; set; } = 50;
            public int AmbientSoundNightVolume { get; set; } = 50;
            public int AreaFlags { get; set; } = 0;
            public AreaWind Wind { get; set; } = new AreaWind();
        }

    }
}
