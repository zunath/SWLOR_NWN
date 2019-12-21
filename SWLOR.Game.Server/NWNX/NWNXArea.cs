using NWN;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXArea
    {

        const string NWNX_Area = "NWNX_Area";

        /// <summary>
        /// Gets the number of players in area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static int GetNumberOfPlayersInArea(NWGameObject area)
        {
            string sFunc = "GetNumberOfPlayersInArea";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Gets the creature that last entered area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static NWGameObject GetLastEntered(NWGameObject area)
        {
            string sFunc = "GetLastEntered";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueObject(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Gets the creature that last left area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static NWGameObject GetLastLeft(NWGameObject area)
        {
            string sFunc = "GetLastLeft";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueObject(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the PVP setting of area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static AreaPVPSetting GetPVPSetting(NWGameObject area)
        {
            string sFunc = "GetPVPSetting";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return (AreaPVPSetting)NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the PVP setting of area
        /// </summary>
        /// <param name="area"></param>
        /// <param name="pvpSetting"></param>
        public static void SetPVPSetting(NWGameObject area, AreaPVPSetting pvpSetting)
        {
            string sFunc = "SetPVPSetting";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, (int)pvpSetting);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the spot modifier of area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static int GetAreaSpotModifier(NWGameObject area)
        {
            string sFunc = "GetAreaSpotModifier";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the spot modifier of area
        /// </summary>
        /// <param name="area"></param>
        /// <param name="spotModifier"></param>
        public static void SetAreaSpotModifier(NWGameObject area, int spotModifier)
        {
            string sFunc = "SetAreaSpotModifier";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, spotModifier);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the listen modifer of area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static int GetAreaListenModifier(NWGameObject area)
        {
            string sFunc = "GetAreaListenModifier";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the listen modifer of area
        /// </summary>
        /// <param name="area"></param>
        /// <param name="listenModifier"></param>
        public static void SetAreaListenModifier(NWGameObject area, int listenModifier)
        {
            string sFunc = "SetAreaListenModifier";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, listenModifier);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Returns true if resting is not allowed in area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static bool GetNoRestingAllowed(NWGameObject area)
        {
            string sFunc = "GetNoRestingAllowed";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc) == 1;
        }

        /// <summary>
        /// Set whether resting is allowed in area
        /// true: Resting not allowed
        /// false: Resting allowed
        /// </summary>
        /// <param name="area"></param>
        /// <param name="bNoRestingAllowed"></param>
        public static void SetNoRestingAllowed(NWGameObject area, bool bNoRestingAllowed)
        {
            string sFunc = "SetNoRestingAllowed";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, bNoRestingAllowed ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the wind power in area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static int GetWindPower(NWGameObject area)
        {
            string sFunc = "GetWindPower";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the wind power in area
        /// windPower = 0-2
        /// </summary>
        /// <param name="area"></param>
        /// <param name="windPower"></param>
        public static void SetWindPower(NWGameObject area, int windPower)
        {
            string sFunc = "SetWindPower";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, windPower);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the weather chance of type in area
        /// </summary>
        /// <param name="area"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetWeatherChance(NWGameObject area, AreaWeatherChance type)
        {
            string sFunc = "GetWeatherChance";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, (int)type);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the weather chance of type in area
        /// </summary>
        /// <param name="area"></param>
        /// <param name="type"></param>
        /// <param name="chance"></param>
        public static void SetWeatherChance(NWGameObject area, AreaWeatherChance type, int chance)
        {
            string sFunc = "SetWeatherChance";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, chance);
            NWNX_PushArgumentInt(NWNX_Area, sFunc, (int)type);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the fog clip distance in area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static float GetFogClipDistance(NWGameObject area)
        {
            string sFunc = "GetFogClipDistance";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueFloat(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the fog clip distance in area
        /// </summary>
        /// <param name="area"></param>
        /// <param name="distance"></param>
        public static void SetFogClipDistance(NWGameObject area, float distance)
        {
            string sFunc = "SetFogClipDistance";

            NWNX_PushArgumentFloat(NWNX_Area, sFunc, distance);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the shadow opacity of area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static int GetShadowOpacity(NWGameObject area)
        {
            string sFunc = "GetShadowOpacity";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the shadow opacity of area
        /// shadowOpacity = 0-100
        /// </summary>
        /// <param name="area"></param>
        /// <param name="shadowOpacity"></param>
        public static void SetShadowOpacity(NWGameObject area, int shadowOpacity)
        {
            string sFunc = "SetShadowOpacity";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, shadowOpacity);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the day/night cycle of area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static AreaDayNightCycle GetDayNightCycle(NWGameObject area)
        {
            string sFunc = "GetDayNightCycle";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return (AreaDayNightCycle)NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the day/night cycle of area
        /// </summary>
        /// <param name="area"></param>
        /// <param name="type"></param>
        public static void SetDayNightCycle(NWGameObject area, AreaDayNightCycle type)
        {
            string sFunc = "SetDayNightCycle";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, (int)type);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Set the Sun/Moon Ambient/Diffuse colors of area
        /// type = NWNX_AREA_COLOR_TYPE_*
        /// color = FOG_COLOR_*
        ///
        /// The color can also be represented as a hex RGB number if specific color shades are desired.
        /// The format of a hex specified color would be 0xFFEEDD where
        /// FF would represent the amount of red in the color
        /// EE would represent the amount of green in the color
        /// DD would represent the amount of blue in the color.
        /// </summary>
        /// <param name="area"></param>
        /// <param name="type"></param>
        /// <param name="color"></param>
        public static void SetSunMoonColors(NWGameObject area, AreaColorType type, int color)
        {
            string sFunc = "SetSunMoonColors";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, color);
            NWNX_PushArgumentInt(NWNX_Area, sFunc, (int)type);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Create and returns a transition (square shaped of specified size) at a location
        /// Valid object types for the target are DOOR or WAYPOINT.
        /// If a tag is specified the returning object will have that tag
        /// </summary>
        /// <param name="area"></param>
        /// <param name="target"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="size"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static NWGameObject CreateTransition(NWGameObject area, NWGameObject target, float x, float y, float z, float size = 2.0f, string tag = "")
        {
            string sFunc = "CreateTransition";

            NWNX_PushArgumentString(NWNX_Area, sFunc, tag);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, size);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, z);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, y);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, x);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, target);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueObject(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the state of a tile animation loop
        /// </summary>
        /// <param name="oArea">The area object</param>
        /// <param name="fTileX">The X coordinate of the tile</param>
        /// <param name="fTileY">The Y coordinate of the tile</param>
        /// <param name="nAnimLoop">The loop to check. (1-3)</param>
        /// <returns>true if the loop is enabled, false otherwise</returns>
        public static bool GetTileAnimationLoop(NWGameObject oArea, float fTileX, float fTileY, int nAnimLoop)
        {
            string sFunc = "GetTileAnimationLoop";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, nAnimLoop);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fTileY);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fTileX);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, oArea);

            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc) == 1;
        }

        /// <summary>
        /// Set the state of a tile animation loop
        /// </summary>
        /// <param name="oArea">The area object.</param>
        /// <param name="fTileX">The X coordinate of the tile</param>
        /// <param name="fTileY">The Y coordinate of the tile</param>
        /// <param name="nAnimLoop">The loop to set (1-3).</param>
        /// <param name="bEnabled">true or false</param>
        public static void SetTileAnimationLoop(NWGameObject oArea, float fTileX, float fTileY, int nAnimLoop, bool bEnabled)
        {
            string sFunc = "SetTileAnimationLoop";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, bEnabled ? 1 : 0);
            NWNX_PushArgumentInt(NWNX_Area, sFunc, nAnimLoop);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fTileY);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fTileX);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, oArea);

            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Test to see if there's a direct, walkable line between two points in the area.
        /// </summary>
        /// <param name="oArea">The area object.</param>
        /// <param name="fStartX">The starting point</param>
        /// <param name="fStartY">The starting point</param>
        /// <param name="fEndX">The ending point</param>
        /// <param name="fEndY">The ending point</param>
        /// <param name="fPerSpace">The personal space of a creature. Found in appearance.2da.</param>
        /// <param name="fHeight">The height of a creature. Found in appearance.2da.</param>
        /// <param name="bIgnoreDoors">Whether to ignore doors in the check.</param>
        /// <returns>The DirectLineResult of the test</returns>
        public static DirectLineResult TestDirectLine(NWGameObject oArea, float fStartX, float fStartY, float fEndX, float fEndY, float fPerSpace, float fHeight, bool bIgnoreDoors = false)
        {
            string sFunc = "TestDirectLine";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, bIgnoreDoors ? 1 : 0);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fHeight);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fPerSpace);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fEndY);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fEndX);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fStartY);
            NWNX_PushArgumentFloat(NWNX_Area, sFunc, fStartX);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, oArea);

            NWNX_CallFunction(NWNX_Area, sFunc);

            return (DirectLineResult)NWNX_GetReturnValueInt(NWNX_Area, sFunc);
        }
    }
}
