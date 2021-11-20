﻿using SWLOR.Game.Server.NWN;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXArea
    {
        private const string PLUGIN_NAME = "NWNX_Area";

        // Gets the number of players in area
        public static int GetNumberOfPlayersInArea(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNumberOfPlayersInArea");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Gets the creature that last entered area
        public static uint GetLastEntered(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLastEntered");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }

        // Gets the creature that last left area
        public static uint GetLastLeft(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLastLeft");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }

        // Get the PVP setting of area
        // Returns NWNX_AREA_PVP_SETTING_*
        public static PvPSetting GetPVPSetting(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPVPSetting");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return (PvPSetting)Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the PVP setting of area
        // pvpSetting = NWNX_AREA_PVP_SETTING_*
        public static void SetPVPSetting(uint area, PvPSetting pvpSetting)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPVPSetting");
            Internal.NativeFunctions.nwnxPushInt((int)pvpSetting);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the spot modifier of area
        public static int GetAreaSpotModifier(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAreaSpotModifier");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the spot modifier of area
        public static void SetAreaSpotModifier(uint area, int spotModifier)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAreaSpotModifier");
            Internal.NativeFunctions.nwnxPushInt(spotModifier);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the listen modifer of area
        public static int GetAreaListenModifier(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAreaListenModifier");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the listen modifier of area
        public static void SetAreaListenModifier(uint area, int listenModifier)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAreaListenModifier");
            Internal.NativeFunctions.nwnxPushInt(listenModifier);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Returns TRUE if resting is not allowed in area
        public static bool GetNoRestingAllowed(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetNoRestingAllowed");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt() == 1;
        }

        // Set whether resting is allowed in area
        // TRUE: Resting not allowed
        // FALSE: Resting allowed
        public static void SetNoRestingAllowed(uint area, bool bNoRestingAllowed)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetNoRestingAllowed");
            Internal.NativeFunctions.nwnxPushInt(bNoRestingAllowed ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the wind power in area
        public static int GetWindPower(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetWindPower");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the wind power in area
        // windPower = 0-2
        public static void SetWindPower(uint area, int windPower)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWindPower");
            Internal.NativeFunctions.nwnxPushInt(windPower);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the weather chance of type in area
        // type = NWNX_AREA_WEATHER_CHANCE_*
        public static int GetWeatherChance(uint area, WeatherEffectType type)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetWeatherChance");
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the weather chance of type in area
        // type = NWNX_AREA_WEATHER_CHANCE_*
        // chance = 0-100
        public static void SetWeatherChance(uint area, WeatherEffectType type, int chance)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeatherChance");
            Internal.NativeFunctions.nwnxPushInt(chance);
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the fog clip distance in area
        public static float GetFogClipDistance(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetFogClipDistance");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopFloat();
        }

        // Set the fog clip distance in area
        public static void SetFogClipDistance(uint area, float distance)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetFogClipDistance");
            Internal.NativeFunctions.nwnxPushFloat(distance);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }


        // Get the shadow opacity of area
        public static int GetShadowOpacity(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetShadowOpacity");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the shadow opacity of area
        // shadowOpacity = 0-100
        public static void SetShadowOpacity(uint area, int shadowOpacity)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetShadowOpacity");
            Internal.NativeFunctions.nwnxPushInt(shadowOpacity);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }


        // Get the day/night cycle of area
        // Returns NWNX_AREA_DAYNIGHTCYCLE_*
        public static DayNightCycle GetDayNightCycle(uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetDayNightCycle");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return (DayNightCycle)Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the day/night cycle of area
        // type = NWNX_AREA_DAYNIGHTCYCLE_*
        public static void SetDayNightCycle(uint area, DayNightCycle type)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetDayNightCycle");
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get the Sun/Moon Ambient/Diffuse colors of area
        // type = NWNX_AREA_COLOR_TYPE_*
        //
        // Returns FOG_COLOR_* or a custom value, -1 on error
        public static int GetSunMoonColors(uint area, ColorType type)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSunMoonColors");
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
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
        public static void SetSunMoonColors(uint area, ColorType type, int color)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSunMoonColors");
            Internal.NativeFunctions.nwnxPushInt(color);
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Create and returns a transition (square shaped of specified size) at a location
        // Valid object types for the target are DOOR or WAYPOINT.
        // If a tag is specified the returning object will have that tag
        public static uint CreateTransition(uint area, uint target, float x, float y, float z, float size = 2.0f,
            string tag = "")
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CreateTransition");
            Internal.NativeFunctions.nwnxPushString(tag);
            Internal.NativeFunctions.nwnxPushFloat(size);
            Internal.NativeFunctions.nwnxPushFloat(z);
            Internal.NativeFunctions.nwnxPushFloat(y);
            Internal.NativeFunctions.nwnxPushFloat(x);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }

        // Get the state of a tile animation loop
        // nAnimLoop = 1-3
        public static int GetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTileAnimationLoop");
            Internal.NativeFunctions.nwnxPushInt(animLoop);
            Internal.NativeFunctions.nwnxPushFloat(tileY);
            Internal.NativeFunctions.nwnxPushFloat(tileX);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set the state of a tile animation loop
        // nAnimLoop = 1-3
        //
        // NOTE: Requires clients to re-enter the area for it to take effect
        public static void SetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop, bool enabled)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetTileAnimationLoop");
            Internal.NativeFunctions.nwnxPushInt(enabled ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(animLoop);
            Internal.NativeFunctions.nwnxPushFloat(tileY);
            Internal.NativeFunctions.nwnxPushFloat(tileX);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
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
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CreateGenericTrigger");
            Internal.NativeFunctions.nwnxPushFloat(size);
            Internal.NativeFunctions.nwnxPushString(tag);
            Internal.NativeFunctions.nwnxPushFloat(z);
            Internal.NativeFunctions.nwnxPushFloat(y);
            Internal.NativeFunctions.nwnxPushFloat(x);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }


        /// @brief Add oObject to the ExportGIT exclusion list, objects on this list won't be exported when NWNX_Area_ExportGIT() is called.
        /// @param oObject The object to add
        public static void AddObjectToExclusionList(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddObjectToExclusionList");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Remove oObject from the ExportGIT exclusion list.
        /// @param oObject The object to add
        public static void RemoveObjectFromExclusionList(uint oObject)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveObjectFromExclusionList");
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxCallFunction();
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
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ExportGIT");
            Internal.NativeFunctions.nwnxPushInt(nObjectFilter);
            Internal.NativeFunctions.nwnxPushInt(bExportUUID ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(bExportVarTable ? 1 : 0);
            Internal.NativeFunctions.nwnxPushString(sFileName);
            Internal.NativeFunctions.nwnxPushObject(oArea);
            Internal.NativeFunctions.nwnxCallFunction();

            return Internal.NativeFunctions.nwnxPopInt();
        }
    }
}
