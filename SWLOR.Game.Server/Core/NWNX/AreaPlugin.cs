using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;

namespace SWLOR.Game.Server.Core.NWNX
{
    public struct TileInfo
    {
        public int nID; /// The tile's ID
        public int nHeight; /// The tile's height
        public int nOrientation; /// The tile's orientation
        public int nGridX; /// The tile's grid x position
        public int nGridY; /// The tile's grid y position
    };

    public static class AreaPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Area";

        // Gets the number of players in area
        public static int GetNumberOfPlayersInArea(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNumberOfPlayersInArea");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Gets the creature that last entered area
        public static uint GetLastEntered(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLastEntered");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        // Gets the creature that last left area
        public static uint GetLastLeft(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLastLeft");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        // Get the PVP setting of area
        // Returns NWNX_AREA_PVP_SETTING_*
        public static PvPSetting GetPVPSetting(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPVPSetting");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (PvPSetting)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the PVP setting of area
        // pvpSetting = NWNX_AREA_PVP_SETTING_*
        public static void SetPVPSetting(uint area, PvPSetting pvpSetting)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPVPSetting");
            NWNCore.NativeFunctions.nwnxPushInt((int)pvpSetting);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get the spot modifier of area
        public static int GetAreaSpotModifier(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAreaSpotModifier");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the spot modifier of area
        public static void SetAreaSpotModifier(uint area, int spotModifier)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAreaSpotModifier");
            NWNCore.NativeFunctions.nwnxPushInt(spotModifier);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get the listen modifer of area
        public static int GetAreaListenModifier(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAreaListenModifier");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the listen modifier of area
        public static void SetAreaListenModifier(uint area, int listenModifier)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAreaListenModifier");
            NWNCore.NativeFunctions.nwnxPushInt(listenModifier);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Returns TRUE if resting is not allowed in area
        public static bool GetNoRestingAllowed(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNoRestingAllowed");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }

        // Set whether resting is allowed in area
        // TRUE: Resting not allowed
        // FALSE: Resting allowed
        public static void SetNoRestingAllowed(uint area, bool bNoRestingAllowed)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetNoRestingAllowed");
            NWNCore.NativeFunctions.nwnxPushInt(bNoRestingAllowed ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get the wind power in area
        public static int GetWindPower(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetWindPower");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the wind power in area
        // windPower = 0-2
        public static void SetWindPower(uint area, int windPower)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWindPower");
            NWNCore.NativeFunctions.nwnxPushInt(windPower);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get the weather chance of type in area
        // type = NWNX_AREA_WEATHER_CHANCE_*
        public static int GetWeatherChance(uint area, WeatherEffectType type)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetWeatherChance");
            NWNCore.NativeFunctions.nwnxPushInt((int)type);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the weather chance of type in area
        // type = NWNX_AREA_WEATHER_CHANCE_*
        // chance = 0-100
        public static void SetWeatherChance(uint area, WeatherEffectType type, int chance)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeatherChance");
            NWNCore.NativeFunctions.nwnxPushInt(chance);
            NWNCore.NativeFunctions.nwnxPushInt((int)type);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get the fog clip distance in area
        public static float GetFogClipDistance(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFogClipDistance");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopFloat();
        }

        // Set the fog clip distance in area
        public static void SetFogClipDistance(uint area, float distance)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFogClipDistance");
            NWNCore.NativeFunctions.nwnxPushFloat(distance);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }


        // Get the shadow opacity of area
        public static int GetShadowOpacity(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetShadowOpacity");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the shadow opacity of area
        // shadowOpacity = 0-100
        public static void SetShadowOpacity(uint area, int shadowOpacity)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetShadowOpacity");
            NWNCore.NativeFunctions.nwnxPushInt(shadowOpacity);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }


        // Get the day/night cycle of area
        // Returns NWNX_AREA_DAYNIGHTCYCLE_*
        public static DayNightCycle GetDayNightCycle(uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDayNightCycle");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (DayNightCycle)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the day/night cycle of area
        // type = NWNX_AREA_DAYNIGHTCYCLE_*
        public static void SetDayNightCycle(uint area, DayNightCycle type)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDayNightCycle");
            NWNCore.NativeFunctions.nwnxPushInt((int)type);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get the Sun/Moon Ambient/Diffuse colors of area
        // type = NWNX_AREA_COLOR_TYPE_*
        //
        // Returns FOG_COLOR_* or a custom value, -1 on error
        public static int GetSunMoonColors(uint area, AreaLightColorType type)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSunMoonColors");
            NWNCore.NativeFunctions.nwnxPushInt((int)type);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the Sun/Moon Ambient/Diffuse colors of area
        // type = NWNX_AREA_COLOR_TYPE_*
        // color = FOG_COLOR_*
        //
        // The color can also be represented as a hex RGB number if specific color shades are desired.
        // The format of a hex specified color would be 0xFFEEDD where
        // FF would represent the amount of red in the color
        // EE would represent the amount of green in the color
        // DD would represent the amount of blue in the color.
        public static void SetSunMoonColors(uint area, AreaLightColorType type, int color)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSunMoonColors");
            NWNCore.NativeFunctions.nwnxPushInt(color);
            NWNCore.NativeFunctions.nwnxPushInt((int)type);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Create and returns a transition (square shaped of specified size) at a location
        // Valid object types for the target are DOOR or WAYPOINT.
        // If a tag is specified the returning object will have that tag
        public static uint CreateTransition(uint area, uint target, float x, float y, float z, float size = 2.0f,
            string tag = "")
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CreateTransition");
            NWNCore.NativeFunctions.nwnxPushString(tag);
            NWNCore.NativeFunctions.nwnxPushFloat(size);
            NWNCore.NativeFunctions.nwnxPushFloat(z);
            NWNCore.NativeFunctions.nwnxPushFloat(y);
            NWNCore.NativeFunctions.nwnxPushFloat(x);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        // Get the state of a tile animation loop
        // nAnimLoop = 1-3
        public static int GetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTileAnimationLoop");
            NWNCore.NativeFunctions.nwnxPushInt(animLoop);
            NWNCore.NativeFunctions.nwnxPushFloat(tileY);
            NWNCore.NativeFunctions.nwnxPushFloat(tileX);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set the state of a tile animation loop
        // nAnimLoop = 1-3
        //
        // NOTE: Requires clients to re-enter the area for it to take effect
        public static void SetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop, bool enabled)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetTileAnimationLoop");
            NWNCore.NativeFunctions.nwnxPushInt(enabled ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(animLoop);
            NWNCore.NativeFunctions.nwnxPushFloat(tileY);
            NWNCore.NativeFunctions.nwnxPushFloat(tileX);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Create and return a generic trigger (square shaped of specified size) at a location.
        // oArea The area object.
        // fX, fY, fZ The position to create the trigger.
        // sTag If specified, the returned trigger will have this tag.
        // fSize The size of the square.
        // NWNX_Object_SetTriggerGeometry() if you wish to draw the trigger as something other than a square.
        public static uint CreateGenericTrigger(uint area, float x, float y, float z, string tag = "",
            float size = 1.0f)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CreateGenericTrigger");
            NWNCore.NativeFunctions.nwnxPushFloat(size);
            NWNCore.NativeFunctions.nwnxPushString(tag);
            NWNCore.NativeFunctions.nwnxPushFloat(z);
            NWNCore.NativeFunctions.nwnxPushFloat(y);
            NWNCore.NativeFunctions.nwnxPushFloat(x);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }


        /// @brief Add oObject to the ExportGIT exclusion list, objects on this list won't be exported when NWNX_Area_ExportGIT() is called.
        /// @param oObject The object to add
        public static void AddObjectToExclusionList(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddObjectToExclusionList");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Remove oObject from the ExportGIT exclusion list.
        /// @param oObject The object to add
        public static void RemoveObjectFromExclusionList(uint oObject)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveObjectFromExclusionList");
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Export the GIT of oArea to the UserDirectory/nwnx folder.
        /// @note Take care with local objects set on objects, they will likely not reference the same object after a server restart.
        /// @param oArea The area to export the GIT of.
        /// @param sFileName The filename, 16 characters or less. If left blank the resref of oArea will be used.
        /// @param bExportVarTable If TRUE, local variables set on oArea will be exported too.
        /// @param bExportUUID If TRUE, the UUID of oArea will be exported, if it has one.
        /// @param nObjectFilter One or more OBJECT_TYPE_* constants. These object will not be exported. For example OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR
        /// will not export creatures and doors. Use OBJECT_TYPE_ALL to filter all objects or 0 to export all objects.
        /// @return TRUE if exported successfully, FALSE if not.
        public static int ExportGIT(uint oArea, string sFileName = "", bool bExportVarTable = true, bool bExportUUID = true, int nObjectFilter = 0)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ExportGIT");
            NWNCore.NativeFunctions.nwnxPushInt(nObjectFilter);
            NWNCore.NativeFunctions.nwnxPushInt(bExportUUID ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(bExportVarTable ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushString(sFileName);
            NWNCore.NativeFunctions.nwnxPushObject(oArea);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }


        /// @brief Get the tile info of the tile at [fTileX, fTileY] in oArea.
        /// @param oArea The area name.
        /// @param fTileX, fTileY The coordinates of the tile.
        /// @return A NWNX_Area_TileInfo struct with tile info.
        public static TileInfo GetTileInfo(uint oArea, float fTileX, float fTileY)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTileInfo");
            NWNCore.NativeFunctions.nwnxPushFloat(fTileY);
            NWNCore.NativeFunctions.nwnxPushFloat(fTileX);
            NWNCore.NativeFunctions.nwnxPushObject(oArea);
            NWNCore.NativeFunctions.nwnxCallFunction();

            var str = new TileInfo
            {
                nGridY = NWNCore.NativeFunctions.nwnxPopInt(),
                nGridX = NWNCore.NativeFunctions.nwnxPopInt(),
                nOrientation = NWNCore.NativeFunctions.nwnxPopInt(),
                nHeight = NWNCore.NativeFunctions.nwnxPopInt(),
                nID = NWNCore.NativeFunctions.nwnxPopInt()
            };

            return str;
        }

        /// @brief Export the .are file of oArea to the UserDirectory/nwnx folder, or to the location of sAlias.
        /// @param oArea The area to export the .are file of.
        /// @param sFileName The filename, 16 characters or less and should be lowercase. This will also be the resref of the area.
        /// @param sNewName Optional new name of the area. Leave blank to use the current name.
        /// @param sNewTag Optional new tag of the area. Leave blank to use the current tag.
        /// @param sAlias The alias of the resource directory to add the .are file to. Default: UserDirectory/nwnx
        /// @return TRUE if exported successfully, FALSE if not.
        public static bool ExportARE(uint oArea, string sFileName, string sNewName = "", string sNewTag = "", string sAlias = "NWNX")
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ExportARE");
            NWNCore.NativeFunctions.nwnxPushString(sAlias);
            NWNCore.NativeFunctions.nwnxPushString(sNewTag);
            NWNCore.NativeFunctions.nwnxPushString(sNewName);
            NWNCore.NativeFunctions.nwnxPushString(sFileName);
            NWNCore.NativeFunctions.nwnxPushObject(oArea);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt() == 1;
        }

        /// @brief Get the ambient sound playing in an area during the day.
        /// @param oArea The area to get the sound of.
        /// @return The ambient soundtrack. See ambientsound.2da.
        public static int GetAmbientSoundDay(uint oArea)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAmbientSoundDay");
            NWNCore.NativeFunctions.nwnxPushObject(oArea);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Get the ambient sound playing in an area during the night.
        /// @param oArea The area to get the sound of.
        /// @return The ambient soundtrack. See ambientsound.2da.
        public static int GetAmbientSoundNight(uint oArea)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAmbientSoundNight");
            NWNCore.NativeFunctions.nwnxPushObject(oArea);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Get the volume of the ambient sound playing in an area during the day.
        /// @param oArea The area to get the sound volume of.
        /// @return The volume.
        public static int GetAmbientSoundDayVolume(uint oArea)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAmbientSoundDayVolume");
            NWNCore.NativeFunctions.nwnxPushObject(oArea);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Get the volume of the ambient sound playing in an area during the night.
        /// @param oArea The area to get the sound volume of.
        /// @return The volume.
        public static int GetAmbientSoundNightVolume(uint oArea)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAmbientSoundNightVolume");
            NWNCore.NativeFunctions.nwnxPushObject(oArea);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        /// @brief Create a sound object.
        /// @param oArea The area where to create the sound object.
        /// @param vPosition The area position where to create the sound object.
        /// @param sResRef The ResRef of the sound object.
        /// @return The sound object.
        public static uint CreateSoundObject(uint oArea, Vector3 vPosition, string sResRef)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CreateSoundObject");
            NWNCore.NativeFunctions.nwnxPushString(sResRef);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Z);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.X);
            NWNCore.NativeFunctions.nwnxPushObject(oArea);
            NWNCore.NativeFunctions.nwnxCallFunction();

            return NWNCore.NativeFunctions.nwnxPopObject();
        }
    }
}