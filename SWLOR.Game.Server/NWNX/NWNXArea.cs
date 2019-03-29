using NWN;
using SWLOR.Game.Server.GameObject;
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
        public static int GetNumberOfPlayersInArea(NWArea area)
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
        public static NWCreature GetLastEntered(NWArea area)
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
        public static NWObject GetLastLeft(NWArea area)
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
        public static AreaPVPSetting GetPVPSetting(NWArea area)
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
        public static void SetPVPSetting(NWArea area, AreaPVPSetting pvpSetting)
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
        public static int GetAreaSpotModifier(NWArea area)
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
        public static void SetAreaSpotModifier(NWArea area, int spotModifier)
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
        public static int GetAreaListenModifier(NWArea area)
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
        public static void SetAreaListenModifier(NWArea area, int listenModifier)
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
        public static bool GetNoRestingAllowed(NWArea area)
        {
            string sFunc = "GetNoRestingAllowed";

            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);

            return NWNX_GetReturnValueInt(NWNX_Area, sFunc) == _.TRUE;
        }

        /// <summary>
        /// Set whether resting is allowed in area
        /// true: Resting not allowed
        /// false: Resting allowed
        /// </summary>
        /// <param name="area"></param>
        /// <param name="bNoRestingAllowed"></param>
        public static void SetNoRestingAllowed(NWArea area, bool bNoRestingAllowed)
        {
            string sFunc = "SetNoRestingAllowed";

            NWNX_PushArgumentInt(NWNX_Area, sFunc, bNoRestingAllowed ? _.TRUE : _.FALSE);
            NWNX_PushArgumentObject(NWNX_Area, sFunc, area);
            NWNX_CallFunction(NWNX_Area, sFunc);
        }

        /// <summary>
        /// Get the wind power in area
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public static int GetWindPower(NWArea area)
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
        public static void SetWindPower(NWArea area, int windPower)
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
        public static int GetWeatherChance(NWArea area, AreaWeatherChance type)
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
        public static void SetWeatherChance(NWArea area, AreaWeatherChance type, int chance)
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
        public static float GetFogClipDistance(NWArea area)
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
        public static void SetFogClipDistance(NWArea area, float distance)
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
        public static int GetShadowOpacity(NWArea area)
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
        public static void SetShadowOpacity(NWArea area, int shadowOpacity)
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
        public static AreaDayNightCycle GetDayNightCycle(NWArea area)
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
        public static void SetDayNightCycle(NWArea area, AreaDayNightCycle type)
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
        public static void SetSunMoonColors(NWArea area, AreaColorType type, int color)
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
        public static NWObject CreateTransition(NWArea area, NWObject target, float x, float y, float z, float size = 2.0f, string tag = "")
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
    }
}
