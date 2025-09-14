using System.Numerics;
using NWN.Core.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive area management functionality including player tracking, area properties,
    /// environmental settings, tile management, and area export capabilities.
    /// This plugin allows for dynamic modification of area properties and monitoring of area events.
    /// </summary>
    public static class AreaPlugin
    {
        /// <summary>
        /// Retrieves the current number of player characters currently in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The number of player characters currently in the area. Returns 0 if no players are present.</returns>
        /// <remarks>
        /// This count only includes player characters, not NPCs or other creatures.
        /// Useful for monitoring area population and implementing area-based restrictions.
        /// </remarks>
        public static int GetNumberOfPlayersInArea(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetNumberOfPlayersInArea(area);
        }

        /// <summary>
        /// Retrieves the creature object that most recently entered the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The object ID of the most recent creature to enter the area. Returns OBJECT_INVALID if no creature has entered.</returns>
        /// <remarks>
        /// This tracks the last creature to enter the area, including both players and NPCs.
        /// Useful for area entry events and monitoring area access.
        /// The tracking resets when the area is reloaded or the server restarts.
        /// </remarks>
        public static uint GetLastEntered(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetLastEntered(area);
        }

        /// <summary>
        /// Retrieves the creature object that most recently left the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The object ID of the most recent creature to leave the area. Returns OBJECT_INVALID if no creature has left.</returns>
        /// <remarks>
        /// This tracks the last creature to leave the area, including both players and NPCs.
        /// Useful for area exit events and monitoring area departures.
        /// The tracking resets when the area is reloaded or the server restarts.
        /// </remarks>
        public static uint GetLastLeft(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetLastLeft(area);
        }

        /// <summary>
        /// Retrieves the current PvP (Player versus Player) setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The current PvP setting for the area. See PvPSetting enum for possible values.</returns>
        /// <remarks>
        /// PvP settings control whether players can engage in combat with each other in the area.
        /// This affects the behavior of attack actions and combat mechanics.
        /// Use SetPVPSetting() to modify this value.
        /// </remarks>
        public static PvPSetting GetPVPSetting(uint area)
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.GetPVPSetting(area);
            return (PvPSetting)result;
        }

        /// <summary>
        /// Sets the PvP (Player versus Player) setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="pvpSetting">The new PvP setting to apply. See PvPSetting enum for available options.</param>
        /// <remarks>
        /// PvP settings control whether players can engage in combat with each other in the area.
        /// Changes take effect immediately and affect all creatures in the area.
        /// This setting overrides individual creature PvP settings within the area.
        /// </remarks>
        public static void SetPVPSetting(uint area, PvPSetting pvpSetting)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetPVPSetting(area, (int)pvpSetting);
        }

        /// <summary>
        /// Retrieves the current Spot skill modifier applied to all creatures in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The Spot skill modifier value. Positive values increase the skill, negative values decrease it.</returns>
        /// <remarks>
        /// This modifier is applied to all Spot skill checks made by creatures within the area.
        /// Useful for creating areas with enhanced or reduced visibility conditions.
        /// Use SetAreaSpotModifier() to modify this value.
        /// </remarks>
        public static int GetAreaSpotModifier(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAreaSpotModifier(area);
        }

        /// <summary>
        /// Sets the Spot skill modifier applied to all creatures in the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="spotModifier">The Spot skill modifier to apply. Positive values increase the skill, negative values decrease it.</param>
        /// <remarks>
        /// This modifier is applied to all Spot skill checks made by creatures within the area.
        /// Changes take effect immediately and affect all creatures in the area.
        /// Useful for creating areas with enhanced or reduced visibility conditions.
        /// </remarks>
        public static void SetAreaSpotModifier(uint area, int spotModifier)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetAreaSpotModifier(area, spotModifier);
        }

        /// <summary>
        /// Retrieves the current Listen skill modifier applied to all creatures in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The Listen skill modifier value. Positive values increase the skill, negative values decrease it.</returns>
        /// <remarks>
        /// This modifier is applied to all Listen skill checks made by creatures within the area.
        /// Useful for creating areas with enhanced or reduced hearing conditions.
        /// Use SetAreaListenModifier() to modify this value.
        /// </remarks>
        public static int GetAreaListenModifier(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAreaListenModifier(area);
        }

        /// <summary>
        /// Sets the Listen skill modifier applied to all creatures in the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="listenModifier">The Listen skill modifier to apply. Positive values increase the skill, negative values decrease it.</param>
        /// <remarks>
        /// This modifier is applied to all Listen skill checks made by creatures within the area.
        /// Changes take effect immediately and affect all creatures in the area.
        /// Useful for creating areas with enhanced or reduced hearing conditions.
        /// </remarks>
        public static void SetAreaListenModifier(uint area, int listenModifier)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetAreaListenModifier(area, listenModifier);
        }

        /// <summary>
        /// Checks whether resting is allowed in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>True if resting is disabled in the area, false if resting is allowed.</returns>
        /// <remarks>
        /// When resting is disabled, players cannot use the rest function while in this area.
        /// This is useful for dangerous or hostile areas where resting would be inappropriate.
        /// Use SetNoRestingAllowed() to modify this setting.
        /// </remarks>
        public static bool GetNoRestingAllowed(uint area)
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.GetNoRestingAllowed(area);
            return result != 0;
        }

        /// <summary>
        /// Sets whether resting is allowed in the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="bNoRestingAllowed">True to disable resting in the area, false to allow resting.</param>
        /// <remarks>
        /// When resting is disabled, players cannot use the rest function while in this area.
        /// Changes take effect immediately and affect all creatures in the area.
        /// This is useful for dangerous or hostile areas where resting would be inappropriate.
        /// </remarks>
        public static void SetNoRestingAllowed(uint area, bool bNoRestingAllowed)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetNoRestingAllowed(area, bNoRestingAllowed ? 1 : 0);
        }

        /// <summary>
        /// Retrieves the current wind power setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The wind power level for the area. Valid values are 0 (no wind), 1 (light wind), or 2 (strong wind).</returns>
        /// <remarks>
        /// Wind power affects the visual appearance and atmosphere of the area.
        /// Higher wind levels create more dramatic environmental effects.
        /// Use SetWindPower() to modify this value.
        /// </remarks>
        public static int GetWindPower(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetWindPower(area);
        }

        /// <summary>
        /// Sets the wind power level for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="windPower">The wind power level to set. Valid values are 0 (no wind), 1 (light wind), or 2 (strong wind).</param>
        /// <remarks>
        /// Wind power affects the visual appearance and atmosphere of the area.
        /// Changes take effect immediately and are visible to all players in the area.
        /// Higher wind levels create more dramatic environmental effects.
        /// </remarks>
        public static void SetWindPower(uint area, int windPower)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetWindPower(area, windPower);
        }

        /// <summary>
        /// Retrieves the percentage chance for a specific weather effect to occur in the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <param name="type">The weather effect type to query. See WeatherEffectType enum for available options.</param>
        /// <returns>The percentage chance (0-100) for the specified weather effect to occur.</returns>
        /// <remarks>
        /// Weather effects are randomly triggered based on these chance values.
        /// The sum of all weather chances should typically not exceed 100%.
        /// Use SetWeatherChance() to modify these values.
        /// </remarks>
        public static int GetWeatherChance(uint area, WeatherEffectType type)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetWeatherChance(area, (int)type);
        }

        /// <summary>
        /// Sets the percentage chance for a specific weather effect to occur in the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="type">The weather effect type to modify. See WeatherEffectType enum for available options.</param>
        /// <param name="chance">The percentage chance (0-100) for the weather effect to occur.</param>
        /// <remarks>
        /// Weather effects are randomly triggered based on these chance values.
        /// Changes take effect immediately and affect future weather generation.
        /// The sum of all weather chances should typically not exceed 100% to avoid conflicts.
        /// </remarks>
        public static void SetWeatherChance(uint area, WeatherEffectType type, int chance)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetWeatherChance(area, (int)type, chance);
        }

        /// <summary>
        /// Retrieves the current fog clip distance setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The fog clip distance in game units. Objects beyond this distance are not rendered.</returns>
        /// <remarks>
        /// Fog clip distance controls the maximum rendering distance for objects in the area.
        /// Lower values improve performance but reduce visibility range.
        /// Use SetFogClipDistance() to modify this value.
        /// </remarks>
        public static float GetFogClipDistance(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetFogClipDistance(area);
        }

        /// <summary>
        /// Sets the fog clip distance for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="distance">The new fog clip distance in game units. Objects beyond this distance will not be rendered.</param>
        /// <remarks>
        /// Fog clip distance controls the maximum rendering distance for objects in the area.
        /// Changes take effect immediately and are visible to all players in the area.
        /// Lower values improve performance but reduce visibility range.
        /// </remarks>
        public static void SetFogClipDistance(uint area, float distance)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetFogClipDistance(area, distance);
        }

        /// <summary>
        /// Retrieves the current shadow opacity setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The shadow opacity value for the area. Valid range is 0-100.</returns>
        /// <remarks>
        /// Shadow opacity controls the intensity of shadows cast in the area.
        /// Higher values create darker, more prominent shadows.
        /// Use SetShadowOpacity() to modify this value.
        /// </remarks>
        public static int GetShadowOpacity(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetShadowOpacity(area);
        }

        /// <summary>
        /// Sets the shadow opacity for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="shadowOpacity">The shadow opacity to set for the area. Valid range is 0-100.</param>
        /// <remarks>
        /// Shadow opacity controls the intensity of shadows cast in the area.
        /// Changes take effect immediately and are visible to all players in the area.
        /// Higher values create darker, more prominent shadows.
        /// </remarks>
        public static void SetShadowOpacity(uint area, int shadowOpacity)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetShadowOpacity(area, shadowOpacity);
        }

        /// <summary>
        /// Retrieves the current day/night cycle setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <returns>The current day/night cycle setting. See DayNightCycle enum for possible values.</returns>
        /// <remarks>
        /// Day/night cycle controls whether the area has dynamic lighting changes based on time.
        /// This affects the overall lighting and atmosphere of the area.
        /// Use SetDayNightCycle() to modify this setting.
        /// </remarks>
        public static DayNightCycle GetDayNightCycle(uint area)
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.GetDayNightCycle(area);
            return (DayNightCycle)result;
        }

        /// <summary>
        /// Sets the day/night cycle for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="type">The day/night cycle setting to apply. See DayNightCycle enum for available options.</param>
        /// <remarks>
        /// Day/night cycle controls whether the area has dynamic lighting changes based on time.
        /// Changes take effect immediately and affect the overall lighting and atmosphere.
        /// This setting can create immersive day/night transitions in the area.
        /// </remarks>
        public static void SetDayNightCycle(uint area, DayNightCycle type)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetDayNightCycle(area, (int)type);
        }

        /// <summary>
        /// Retrieves the current sun/moon ambient or diffuse color setting for the specified area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <param name="type">The sun/moon color type to query. See AreaLightColorType enum for available options.</param>
        /// <returns>The color value as a FOG_COLOR_* constant or custom hex value. Returns -1 on error.</returns>
        /// <remarks>
        /// Sun/moon colors affect the overall lighting and atmosphere of the area.
        /// These colors are used for ambient and diffuse lighting calculations.
        /// Use SetSunMoonColors() to modify these values.
        /// </remarks>
        public static int GetSunMoonColors(uint area, AreaLightColorType type)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetSunMoonColors(area, (int)type);
        }

        /// <summary>
        /// Sets the sun/moon ambient or diffuse color for the specified area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="type">The sun/moon color type to modify. See AreaLightColorType enum for available options.</param>
        /// <param name="color">The color value to set. Can be a FOG_COLOR_* constant or custom hex RGB value.</param>
        /// <remarks>
        /// Sun/moon colors affect the overall lighting and atmosphere of the area.
        /// Changes take effect immediately and are visible to all players in the area.
        /// The color can be represented as a hex RGB number for specific color shades.
        /// Format: 0xFFEEDD where FF=red, EE=green, DD=blue components.
        /// </remarks>
        public static void SetSunMoonColors(uint area, AreaLightColorType type, int color)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetSunMoonColors(area, (int)type, color);
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
        /// <remarks>
        /// Transitions are invisible triggers that can be used to create area connections or trigger events.
        /// The transition will be square-shaped with the specified size.
        /// Players walking into the transition area will trigger the transition to the target object.
        /// </remarks>
        public static uint CreateTransition(uint area, uint target, float x, float y, float z, float size = 2.0f,
            string tag = "")
        {
            return global::NWN.Core.NWNX.AreaPlugin.CreateTransition(area, target, x, y, z, size, tag);
        }

        /// <summary>
        /// Retrieves the current state of a specific tile animation loop in the area.
        /// </summary>
        /// <param name="area">The area object to query. Must be a valid area object.</param>
        /// <param name="tileX">The X coordinate of the tile to check.</param>
        /// <param name="tileY">The Y coordinate of the tile to check.</param>
        /// <param name="animLoop">The animation loop to check. Valid values are 1-3.</param>
        /// <returns>True if the specified animation loop is enabled, false if disabled.</returns>
        /// <remarks>
        /// Tile animations provide visual effects on specific tiles in the area.
        /// Each tile can have up to 3 animation loops that can be independently controlled.
        /// Use SetTileAnimationLoop() to modify these settings.
        /// </remarks>
        public static bool GetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop)
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.GetTileAnimationLoop(area, tileX, tileY, animLoop);
            return result != 0;
        }

        /// <summary>
        /// Sets the state of a specific tile animation loop in the area.
        /// </summary>
        /// <param name="area">The area object to modify. Must be a valid area object.</param>
        /// <param name="tileX">The X coordinate of the tile to modify.</param>
        /// <param name="tileY">The Y coordinate of the tile to modify.</param>
        /// <param name="animLoop">The animation loop to modify. Valid values are 1-3.</param>
        /// <param name="enabled">True to enable the animation loop, false to disable it.</param>
        /// <remarks>
        /// Tile animations provide visual effects on specific tiles in the area.
        /// Each tile can have up to 3 animation loops that can be independently controlled.
        /// Changes require clients to re-enter the area to take effect.
        /// This is useful for creating dynamic environmental effects.
        /// </remarks>
        public static void SetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop, bool enabled)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetTileAnimationLoop(area, tileX, tileY, animLoop, enabled ? 1 : 0);
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
        /// <remarks>
        /// Generic triggers are invisible objects that can detect when creatures enter their area.
        /// They can be used to trigger scripts, events, or other game mechanics.
        /// The trigger will be square-shaped with the specified size.
        /// </remarks>
        public static uint CreateGenericTrigger(uint area, float x, float y, float z, string tag = "",
            float size = 1.0f)
        {
            return global::NWN.Core.NWNX.AreaPlugin.CreateGenericTrigger(area, x, y, z, tag, size);
        }


        /// <summary>
        /// Adds an object to the ExportGIT exclusion list, preventing it from being exported.
        /// </summary>
        /// <param name="oObject">The object to add to the exclusion list. Must be a valid object.</param>
        /// <remarks>
        /// Objects on the exclusion list will not be included when ExportGIT() is called.
        /// This is useful for excluding temporary or dynamically created objects from area exports.
        /// Use RemoveObjectFromExclusionList() to remove objects from the exclusion list.
        /// </remarks>
        public static void AddObjectToExclusionList(uint oObject)
        {
            global::NWN.Core.NWNX.AreaPlugin.AddObjectToExclusionList(oObject);
        }

        /// <summary>
        /// Removes an object from the ExportGIT exclusion list, allowing it to be exported again.
        /// </summary>
        /// <param name="oObject">The object to remove from the exclusion list. Must be a valid object.</param>
        /// <remarks>
        /// Objects removed from the exclusion list will be included in future ExportGIT() calls.
        /// This allows previously excluded objects to be exported again.
        /// Use AddObjectToExclusionList() to add objects to the exclusion list.
        /// </remarks>
        public static void RemoveObjectFromExclusionList(uint oObject)
        {
            global::NWN.Core.NWNX.AreaPlugin.RemoveObjectFromExclusionList(oObject);
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
        /// <remarks>
        /// The GIT file contains all the objects, creatures, and data in the area.
        /// This is useful for backing up areas or transferring them between modules.
        /// Objects on the exclusion list (set via AddObjectToExclusionList) will not be exported.
        /// Local objects set on other objects may not reference the same object after a server restart.
        /// </remarks>
        public static bool ExportGIT(uint oArea, string sFileName = "", bool bExportVarTable = true, bool bExportUUID = true, int nObjectFilter = 0, string sAlias = "NWNX")
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.ExportGIT(oArea, sFileName, bExportVarTable ? 1 : 0, bExportUUID ? 1 : 0, nObjectFilter, sAlias);
            return result != 0;
        }


        /// <summary>
        /// Retrieves detailed information about a specific tile in the area.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <param name="fTileX">The X coordinate of the tile to examine.</param>
        /// <param name="fTileY">The Y coordinate of the tile to examine.</param>
        /// <returns>A TileInfo struct containing detailed information about the tile.</returns>
        /// <remarks>
        /// Tile information includes properties such as material type, walkability, and other tile-specific data.
        /// This is useful for pathfinding, area analysis, or custom tile-based mechanics.
        /// The coordinates should correspond to valid tile positions within the area.
        /// </remarks>
        public static TileInfo GetTileInfo(uint oArea, float fTileX, float fTileY)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetTileInfo(oArea, fTileX, fTileY);
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
        /// <remarks>
        /// The .are file contains the area's basic properties, terrain, and static elements.
        /// This is useful for backing up areas or transferring them between modules.
        /// The filename should be lowercase and will become the area's resource reference.
        /// </remarks>
        public static bool ExportARE(uint oArea, string sFileName, string sNewName = "", string sNewTag = "", string sAlias = "NWNX")
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.ExportARE(oArea, sFileName, sNewName, sNewTag, sAlias);
            return result != 0;
        }

        /// <summary>
        /// Retrieves the ambient sound that plays in the area during daytime.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The ambient sound track index. See ambientsound.2da for available sound tracks.</returns>
        /// <remarks>
        /// Ambient sounds provide atmospheric audio for the area.
        /// Different sounds can be set for day and night to create varied atmospheres.
        /// Use GetAmbientSoundDayVolume() to get the volume level.
        /// </remarks>
        public static int GetAmbientSoundDay(uint oArea)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAmbientSoundDay(oArea);
        }

        /// <summary>
        /// Retrieves the ambient sound that plays in the area during nighttime.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The ambient sound track index. See ambientsound.2da for available sound tracks.</returns>
        /// <remarks>
        /// Ambient sounds provide atmospheric audio for the area.
        /// Different sounds can be set for day and night to create varied atmospheres.
        /// Use GetAmbientSoundNightVolume() to get the volume level.
        /// </remarks>
        public static int GetAmbientSoundNight(uint oArea)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAmbientSoundNight(oArea);
        }

        /// <summary>
        /// Retrieves the volume level of the ambient sound that plays in the area during daytime.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The volume level of the daytime ambient sound.</returns>
        /// <remarks>
        /// Volume levels control how loud the ambient sound plays.
        /// This works in conjunction with the sound track set by GetAmbientSoundDay().
        /// Volume values typically range from 0 (silent) to 100 (maximum volume).
        /// </remarks>
        public static int GetAmbientSoundDayVolume(uint oArea)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAmbientSoundDayVolume(oArea);
        }

        /// <summary>
        /// Retrieves the volume level of the ambient sound that plays in the area during nighttime.
        /// </summary>
        /// <param name="oArea">The area object to query. Must be a valid area object.</param>
        /// <returns>The volume level of the nighttime ambient sound.</returns>
        /// <remarks>
        /// Volume levels control how loud the ambient sound plays.
        /// This works in conjunction with the sound track set by GetAmbientSoundNight().
        /// Volume values typically range from 0 (silent) to 100 (maximum volume).
        /// </remarks>
        public static int GetAmbientSoundNightVolume(uint oArea)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAmbientSoundNightVolume(oArea);
        }

        /// <summary>
        /// Creates a sound object at the specified position in the area.
        /// </summary>
        /// <param name="oArea">The area object where the sound will be created. Must be a valid area object.</param>
        /// <param name="vPosition">The 3D position where the sound object will be created.</param>
        /// <param name="sResRef">The resource reference of the sound file to play. Must be a valid sound resource.</param>
        /// <returns>The object ID of the created sound object. Returns OBJECT_INVALID on failure.</returns>
        /// <remarks>
        /// Sound objects play audio at a specific location in the area.
        /// They can be used to create localized audio effects, ambient sounds, or triggered audio.
        /// The sound will play continuously at the specified position.
        /// The ResRef should point to a valid sound file in the module's resources.
        /// </remarks>
        public static uint CreateSoundObject(uint oArea, Vector3 vPosition, string sResRef)
        {
            return global::NWN.Core.NWNX.AreaPlugin.CreateSoundObject(oArea, vPosition, sResRef);
        }
    }
}