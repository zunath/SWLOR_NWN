using System.Numerics;
using NWN.Core.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;

namespace SWLOR.NWN.API.NWNX
{
    public static class AreaPlugin
    {
        /// <summary>
        /// Gets the number of players in area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The player count for the area.</returns>
        public static int GetNumberOfPlayersInArea(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetNumberOfPlayersInArea(area);
        }

        /// <summary>
        /// Gets the creature that last entered area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The most recent creature to enter the area.</returns>
        public static uint GetLastEntered(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetLastEntered(area);
        }

        /// <summary>
        /// Gets the creature that last left area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The most recent creature to leave the area.</returns>
        public static uint GetLastLeft(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetLastLeft(area);
        }

        /// <summary>
        /// Get the PVP setting of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>Returns the PVP Setting for the area.</returns>
        public static PvPSetting GetPVPSetting(uint area)
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.GetPVPSetting(area);
            return (PvPSetting)result;
        }

        /// <summary>
        /// Set the PVP setting of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="pvpSetting">One of the PVP Settings.</param>
        public static void SetPVPSetting(uint area, PvPSetting pvpSetting)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetPVPSetting(area, (int)pvpSetting);
        }

        /// <summary>
        /// Get the spot modifier of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The value of the Spot skill modifier for this area.</returns>
        public static int GetAreaSpotModifier(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAreaSpotModifier(area);
        }

        /// <summary>
        /// Set the spot modifier of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="spotModifier">The modifier to the Spot skill for this area.</param>
        public static void SetAreaSpotModifier(uint area, int spotModifier)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetAreaSpotModifier(area, spotModifier);
        }

        /// <summary>
        /// Get the listen modifier of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The value of the Listen skill modifier for this area.</returns>
        public static int GetAreaListenModifier(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAreaListenModifier(area);
        }

        /// <summary>
        /// Set the listen modifier of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="listenModifier">The modifier to the Listen skill for this area.</param>
        public static void SetAreaListenModifier(uint area, int listenModifier)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetAreaListenModifier(area, listenModifier);
        }

        /// <summary>
        /// Checks the No Resting area flag.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>True if resting is not allowed in area.</returns>
        public static bool GetNoRestingAllowed(uint area)
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.GetNoRestingAllowed(area);
            return result != 0;
        }

        /// <summary>
        /// Set whether to disable resting in the area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="bNoRestingAllowed">True to disable resting in the area.</param>
        public static void SetNoRestingAllowed(uint area, bool bNoRestingAllowed)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetNoRestingAllowed(area, bNoRestingAllowed ? 1 : 0);
        }

        /// <summary>
        /// Get the wind power in area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The wind power for the area. (0-2)</returns>
        public static int GetWindPower(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetWindPower(area);
        }

        /// <summary>
        /// Set the wind power in area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="windPower">Set to 0, 1 or 2.</param>
        public static void SetWindPower(uint area, int windPower)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetWindPower(area, windPower);
        }

        /// <summary>
        /// Get the weather chance of type in area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="type">A Weather Setting.</param>
        /// <returns>The percentage chance for the weather type. (0-100)</returns>
        public static int GetWeatherChance(uint area, WeatherEffectType type)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetWeatherChance(area, (int)type);
        }

        /// <summary>
        /// Set the weather chance of type in area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="type">A Weather Setting.</param>
        /// <param name="chance">The chance this weather event occurs.</param>
        public static void SetWeatherChance(uint area, WeatherEffectType type, int chance)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetWeatherChance(area, (int)type, chance);
        }

        /// <summary>
        /// Get the fog clip distance in area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The fog clip distance.</returns>
        public static float GetFogClipDistance(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetFogClipDistance(area);
        }

        /// <summary>
        /// Set the fog clip distance in area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="distance">The new fog clip distance.</param>
        public static void SetFogClipDistance(uint area, float distance)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetFogClipDistance(area, distance);
        }


        /// <summary>
        /// Get the shadow opacity of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The shadow opacity for the area. (0-100)</returns>
        public static int GetShadowOpacity(uint area)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetShadowOpacity(area);
        }

        /// <summary>
        /// Set the shadow opacity of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="shadowOpacity">The shadow opacity to set for the area (0-100).</param>
        public static void SetShadowOpacity(uint area, int shadowOpacity)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetShadowOpacity(area, shadowOpacity);
        }


        /// <summary>
        /// Get the day/night cycle of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <returns>The Day Night Cycle Setting.</returns>
        public static DayNightCycle GetDayNightCycle(uint area)
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.GetDayNightCycle(area);
            return (DayNightCycle)result;
        }

        /// <summary>
        /// Set the day/night cycle of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="type">A Day Night Cycle Setting.</param>
        public static void SetDayNightCycle(uint area, DayNightCycle type)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetDayNightCycle(area, (int)type);
        }

        /// <summary>
        /// Get the Sun/Moon Ambient/Diffuse colors of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="type">A Sun/Moon Color Setting.</param>
        /// <returns>A FOG_COLOR_* or a custom value, -1 on error.</returns>
        public static int GetSunMoonColors(uint area, AreaLightColorType type)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetSunMoonColors(area, (int)type);
        }

        /// <summary>
        /// Set the Sun/Moon Ambient/Diffuse colors of area.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="type">A Sun/Moon Color Setting.</param>
        /// <param name="color">A FOG_COLOR_*.</param>
        /// <remarks>
        /// The color can also be represented as a hex RGB number if specific color shades are desired.
        /// The format of a hex specified color would be 0xFFEEDD where
        /// FF would represent the amount of red in the color
        /// EE would represent the amount of green in the color
        /// DD would represent the amount of blue in the color.
        /// </remarks>
        public static void SetSunMoonColors(uint area, AreaLightColorType type, int color)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetSunMoonColors(area, (int)type, color);
        }

        /// <summary>
        /// Create and returns a transition (square shaped of specified size) at a location.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="target">A door or waypoint object.</param>
        /// <param name="x">The X position to create the transition.</param>
        /// <param name="y">The Y position to create the transition.</param>
        /// <param name="z">The Z position to create the transition.</param>
        /// <param name="size">The size of the square.</param>
        /// <param name="tag">If specified, the returning object will have this tag.</param>
        /// <returns>The created transition object.</returns>
        public static uint CreateTransition(uint area, uint target, float x, float y, float z, float size = 2.0f,
            string tag = "")
        {
            return global::NWN.Core.NWNX.AreaPlugin.CreateTransition(area, target, x, y, z, size, tag);
        }

        /// <summary>
        /// Get the state of a tile animation loop.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="tileX">The X coordinate of the tile.</param>
        /// <param name="tileY">The Y coordinate of the tile.</param>
        /// <param name="animLoop">The loop to check. (1-3)</param>
        /// <returns>True if the loop is enabled.</returns>
        public static bool GetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop)
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.GetTileAnimationLoop(area, tileX, tileY, animLoop);
            return result != 0;
        }

        /// <summary>
        /// Set the state of a tile animation loop.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="tileX">The X coordinate of the tile.</param>
        /// <param name="tileY">The Y coordinate of the tile.</param>
        /// <param name="animLoop">The loop to set (1-3).</param>
        /// <param name="enabled">True or false.</param>
        /// <remarks>Requires clients to re-enter the area for it to take effect.</remarks>
        public static void SetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop, bool enabled)
        {
            global::NWN.Core.NWNX.AreaPlugin.SetTileAnimationLoop(area, tileX, tileY, animLoop, enabled ? 1 : 0);
        }

        /// <summary>
        /// Create and return a generic trigger (square shaped of specified size) at a location.
        /// </summary>
        /// <param name="area">The area object.</param>
        /// <param name="x">The X position to create the trigger.</param>
        /// <param name="y">The Y position to create the trigger.</param>
        /// <param name="z">The Z position to create the trigger.</param>
        /// <param name="tag">If specified, the returned trigger will have this tag.</param>
        /// <param name="size">The size of the square.</param>
        /// <returns>The created trigger object.</returns>
        public static uint CreateGenericTrigger(uint area, float x, float y, float z, string tag = "",
            float size = 1.0f)
        {
            return global::NWN.Core.NWNX.AreaPlugin.CreateGenericTrigger(area, x, y, z, tag, size);
        }


        /// <summary>
        /// Add object to the ExportGIT exclusion list, objects on this list won't be exported when ExportGIT() is called.
        /// </summary>
        /// <param name="oObject">The object to add.</param>
        public static void AddObjectToExclusionList(uint oObject)
        {
            global::NWN.Core.NWNX.AreaPlugin.AddObjectToExclusionList(oObject);
        }

        /// <summary>
        /// Remove object from the ExportGIT exclusion list.
        /// </summary>
        /// <param name="oObject">The object to remove.</param>
        public static void RemoveObjectFromExclusionList(uint oObject)
        {
            global::NWN.Core.NWNX.AreaPlugin.RemoveObjectFromExclusionList(oObject);
        }

        /// <summary>
        /// Export the GIT of area to the UserDirectory/nwnx folder.
        /// </summary>
        /// <param name="oArea">The area to export the GIT of.</param>
        /// <param name="sFileName">The filename, 16 characters or less. If left blank the resref of area will be used.</param>
        /// <param name="bExportVarTable">If true, local variables set on area will be exported too.</param>
        /// <param name="bExportUUID">If true, the UUID of area will be exported, if it has one.</param>
        /// <param name="nObjectFilter">One or more OBJECT_TYPE_* constants. These objects will not be exported.</param>
        /// <param name="sAlias">The alias of the resource directory to add the .git file to. Default: UserDirectory/nwnx</param>
        /// <returns>True if exported successfully, false if not.</returns>
        /// <remarks>Take care with local objects set on objects, they will likely not reference the same object after a server restart.</remarks>
        public static bool ExportGIT(uint oArea, string sFileName = "", bool bExportVarTable = true, bool bExportUUID = true, int nObjectFilter = 0, string sAlias = "NWNX")
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.ExportGIT(oArea, sFileName, bExportVarTable ? 1 : 0, bExportUUID ? 1 : 0, nObjectFilter, sAlias);
            return result != 0;
        }


        /// <summary>
        /// Get the tile info of the tile at [fTileX, fTileY] in area.
        /// </summary>
        /// <param name="oArea">The area name.</param>
        /// <param name="fTileX">The X coordinate of the tile.</param>
        /// <param name="fTileY">The Y coordinate of the tile.</param>
        /// <returns>A TileInfo struct with tile info.</returns>
        public static TileInfo GetTileInfo(uint oArea, float fTileX, float fTileY)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetTileInfo(oArea, fTileX, fTileY);
        }

        /// <summary>
        /// Export the .are file of area to the UserDirectory/nwnx folder, or to the location of alias.
        /// </summary>
        /// <param name="oArea">The area to export the .are file of.</param>
        /// <param name="sFileName">The filename, 16 characters or less and should be lowercase. This will also be the resref of the area.</param>
        /// <param name="sNewName">Optional new name of the area. Leave blank to use the current name.</param>
        /// <param name="sNewTag">Optional new tag of the area. Leave blank to use the current tag.</param>
        /// <param name="sAlias">The alias of the resource directory to add the .are file to. Default: UserDirectory/nwnx</param>
        /// <returns>True if exported successfully, false if not.</returns>
        public static bool ExportARE(uint oArea, string sFileName, string sNewName = "", string sNewTag = "", string sAlias = "NWNX")
        {
            int result = global::NWN.Core.NWNX.AreaPlugin.ExportARE(oArea, sFileName, sNewName, sNewTag, sAlias);
            return result != 0;
        }

        /// <summary>
        /// Get the ambient sound playing in an area during the day.
        /// </summary>
        /// <param name="oArea">The area to get the sound of.</param>
        /// <returns>The ambient soundtrack. See ambientsound.2da.</returns>
        public static int GetAmbientSoundDay(uint oArea)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAmbientSoundDay(oArea);
        }

        /// <summary>
        /// Get the ambient sound playing in an area during the night.
        /// </summary>
        /// <param name="oArea">The area to get the sound of.</param>
        /// <returns>The ambient soundtrack. See ambientsound.2da.</returns>
        public static int GetAmbientSoundNight(uint oArea)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAmbientSoundNight(oArea);
        }

        /// <summary>
        /// Get the volume of the ambient sound playing in an area during the day.
        /// </summary>
        /// <param name="oArea">The area to get the sound volume of.</param>
        /// <returns>The volume.</returns>
        public static int GetAmbientSoundDayVolume(uint oArea)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAmbientSoundDayVolume(oArea);
        }

        /// <summary>
        /// Get the volume of the ambient sound playing in an area during the night.
        /// </summary>
        /// <param name="oArea">The area to get the sound volume of.</param>
        /// <returns>The volume.</returns>
        public static int GetAmbientSoundNightVolume(uint oArea)
        {
            return global::NWN.Core.NWNX.AreaPlugin.GetAmbientSoundNightVolume(oArea);
        }

        /// <summary>
        /// Create a sound object.
        /// </summary>
        /// <param name="oArea">The area where to create the sound object.</param>
        /// <param name="vPosition">The area position where to create the sound object.</param>
        /// <param name="sResRef">The ResRef of the sound object.</param>
        /// <returns>The sound object.</returns>
        public static uint CreateSoundObject(uint oArea, Vector3 vPosition, string sResRef)
        {
            return global::NWN.Core.NWNX.AreaPlugin.CreateSoundObject(oArea, vPosition, sResRef);
        }
    }
}