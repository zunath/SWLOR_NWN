using System;
using SWLOR.Component.World.Contracts;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Module;
using WeatherType = SWLOR.NWN.API.NWScript.Enum.WeatherType;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service for calculating weather conditions and adjustments.
    /// </summary>
    public class WeatherCalculationService : IWeatherCalculationService
    {
        private readonly IWeatherClimateService _weatherClimateService;

        // Module and area variables.
        private const string VAR_WEATHER_CHANGE = "VAR_WEATHER_CHANGE";
        private const string VAR_WEATHER_HEAT = "VAR_WEATHER_HEAT";
        private const string VAR_WEATHER_HUMIDITY = "VAR_WEATHER_HUMIDITY";
        private const string VAR_WEATHER_WIND = "VAR_WEATHER_WIND";
        private const string VAR_WEATHER_ACID_RAIN = "VAR_WEATHER_ACID_RAIN";
        private const string VAR_INITIALIZED = "VAR_WH_INITIALIZED";

        public WeatherCalculationService(IWeatherClimateService weatherClimateService)
        {
            _weatherClimateService = weatherClimateService;
        }

        /// <summary>
        /// Adjusts the weather based on current conditions and time.
        /// </summary>
        /// <returns>True if weather was adjusted, false otherwise.</returns>
        public bool AdjustWeather()
        {
            var oMod = GetModule();

            //--------------------------------------------------------------------------
            // Always change the weather the very first time
            //--------------------------------------------------------------------------
            if (GetLocalInt(oMod, VAR_INITIALIZED) == 0)
            {
                SetLocalInt(oMod, VAR_INITIALIZED, 1);
                _SetHumidity(Random(10) + 1);
            }
            else if (GetTimeHour() != GetLocalInt(oMod, VAR_WEATHER_CHANGE))
            {
                return false;
            }

            //--------------------------------------------------------------------------
            // Adjust the indices.  Only humidity is affected by the current values.
            //--------------------------------------------------------------------------
            var nHumidity = GetHumidity(OBJECT_SELF);
            var nWind = GetWindStrength(OBJECT_SELF);

            //--------------------------------------------------------------------------
            // Heat is affected by time of year.
            //--------------------------------------------------------------------------
            var nHeat = Random(5) + (6 - abs(GetCalendarMonth() - 6));
            if (nHeat < 1) nHeat = 1;

            //--------------------------------------------------------------------------
            // Humidity is random but moves slowly.
            //--------------------------------------------------------------------------
            nHumidity = nHumidity + (Random(2 * nWind + 1) - nWind);
            if (nHumidity > 10) nHumidity = 20 - nHumidity;
            if (nHumidity < 1) nHumidity = 1 - nHumidity;

            //--------------------------------------------------------------------------
            // Wind is more likely to be calm, but can change quickly.
            //--------------------------------------------------------------------------
            nWind = d10(2) - 10;
            if (nWind < 1) nWind = 1 - nWind;

            _SetHeatIndex(nHeat);
            _SetHumidity(nHumidity);
            _SetWindStrength(nWind);

            //--------------------------------------------------------------------------
            // Work out when to next change the weather.
            //--------------------------------------------------------------------------
            var nNextChange = GetTimeHour() + (11 - nWind);
            if (nNextChange > 23) nNextChange -= 24;
            SetLocalInt(oMod, VAR_WEATHER_CHANGE, nNextChange);

            // Update all occupied areas with the new settings.
            var oPC = GetFirstPC();
            while (GetIsObjectValid(oPC))
            {
                SetWeather(GetArea(oPC), AreaWeatherType.Clear);
                oPC = GetNextPC();
            }

            return true;
        }

        /// <summary>
        /// Gets the heat index for an area.
        /// </summary>
        /// <param name="oArea">The area to get heat index for.</param>
        /// <returns>The heat index value.</returns>
        public int GetHeatIndex(uint oArea)
        {
            //--------------------------------------------------------------------------
            // Areas may have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            var oMod = GetModule();
            var nHeat = GetLocalInt(oMod, VAR_WEATHER_HEAT);
            if (GetIsObjectValid(oArea))
            {
                nHeat += GetLocalInt(oArea, VAR_WEATHER_HEAT);
                nHeat += _weatherClimateService.GetAreaClimate(oArea).HeatModifier;
            }

            nHeat = (GetIsNight()) ? nHeat - 2 : nHeat + 2;

            if (nHeat > 10) nHeat = 10;
            if (nHeat < 1) nHeat = 1;

            return nHeat;
        }

        /// <summary>
        /// Gets the humidity for an area.
        /// </summary>
        /// <param name="oArea">The area to get humidity for.</param>
        /// <returns>The humidity value.</returns>
        public int GetHumidity(uint oArea)
        {
            //--------------------------------------------------------------------------
            // Areas may have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            var oMod = GetModule();
            var nHumidity = GetLocalInt(oMod, VAR_WEATHER_HUMIDITY);
            if (GetIsObjectValid(oArea))
            {
                nHumidity += GetLocalInt(oArea, VAR_WEATHER_HUMIDITY);
                nHumidity += _weatherClimateService.GetAreaClimate(oArea).HumidityModifier;
            }

            if (nHumidity > 10) nHumidity = 10;
            if (nHumidity < 1) nHumidity = 1;

            return nHumidity;
        }

        /// <summary>
        /// Gets the wind strength for an area.
        /// </summary>
        /// <param name="oArea">The area to get wind strength for.</param>
        /// <returns>The wind strength value.</returns>
        public int GetWindStrength(uint oArea)
        {
            //--------------------------------------------------------------------------
            // Areas will have one of the CLIMATE_* values stored in each weather var.
            //--------------------------------------------------------------------------
            var oMod = GetModule();
            var nWind = GetLocalInt(oMod, VAR_WEATHER_WIND);
            if (GetIsObjectValid(oArea))
            {
                nWind += GetLocalInt(oArea, VAR_WEATHER_WIND);
                nWind += _weatherClimateService.GetAreaClimate(oArea).WindModifier;

                //----------------------------------------------------------------------
                // Automatic cover bonus for artificial areas such as cities (lots of
                // buildings).
                //----------------------------------------------------------------------
                if (GetIsAreaNatural(oArea) == 0) nWind -= 1;
            }

            if (nWind > 10) nWind = 10;
            if (nWind < 1) nWind = 1;

            return nWind;
        }

        /// <summary>
        /// Gets the current weather for the current area.
        /// </summary>
        /// <returns>The current weather type.</returns>
        public WeatherType GetWeather()
        {
            return GetWeather(OBJECT_SELF);
        }

        /// <summary>
        /// Gets the weather for a specific area.
        /// </summary>
        /// <param name="oArea">The area to get weather for.</param>
        /// <returns>The weather type for the area.</returns>
        public WeatherType GetWeather(uint oArea)
        {
            if (GetIsAreaInterior(oArea) || GetIsAreaAboveGround(oArea) == false)
            {
                return WeatherType.Invalid;
            }

            var nHeat = GetHeatIndex(oArea);
            var nHumidity = GetHumidity(oArea);
            var nWind = GetWindStrength(oArea);

            if (nHumidity > 7 && nHeat > 3 && nHeat < 6 && nWind < 3)
            {
                return WeatherType.Foggy;
            }

            // Rather unfortunately, the default method is also called GetWeather. 
            return SWLOR.NWN.API.Service.NWScript.GetWeather(oArea);
        }

        /// <summary>
        /// Sets the heat modifier for an area.
        /// </summary>
        /// <param name="oArea">The area to set modifier for.</param>
        /// <param name="nModifier">The heat modifier value.</param>
        public void SetAreaHeatModifier(uint oArea, int nModifier)
        {
            SetLocalInt(oArea, VAR_WEATHER_HEAT, nModifier);
        }

        /// <summary>
        /// Sets the wind modifier for an area.
        /// </summary>
        /// <param name="oArea">The area to set modifier for.</param>
        /// <param name="nModifier">The wind modifier value.</param>
        public void SetAreaWindModifier(uint oArea, int nModifier)
        {
            SetLocalInt(oArea, VAR_WEATHER_WIND, nModifier);
        }

        /// <summary>
        /// Sets the humidity modifier for an area.
        /// </summary>
        /// <param name="oArea">The area to set modifier for.</param>
        /// <param name="nModifier">The humidity modifier value.</param>
        public void SetAreaHumidityModifier(uint oArea, int nModifier)
        {
            SetLocalInt(oArea, VAR_WEATHER_HUMIDITY, nModifier);
        }

        /// <summary>
        /// Sets the acid rain modifier for an area.
        /// </summary>
        /// <param name="oArea">The area to set modifier for.</param>
        /// <param name="nModifier">The acid rain modifier value.</param>
        public void SetAreaAcidRain(uint oArea, int nModifier)
        {
            SetLocalInt(oArea, VAR_WEATHER_ACID_RAIN, nModifier);
        }

        private void _SetHeatIndex(int nHeat)
        {
            var oMod = GetModule();
            SetLocalInt(oMod, VAR_WEATHER_HEAT, nHeat);
        }

        private void _SetHumidity(int nHumidity)
        {
            var oMod = GetModule();
            SetLocalInt(oMod, VAR_WEATHER_HUMIDITY, nHumidity);
        }

        private void _SetWindStrength(int nWind)
        {
            var oMod = GetModule();
            SetLocalInt(oMod, VAR_WEATHER_WIND, nWind);
        }
    }
}
