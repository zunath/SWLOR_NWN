using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using AreaWeatherType = SWLOR.NWN.API.NWScript.Enum.AreaWeatherType;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Area;
using WeatherType = SWLOR.NWN.API.NWScript.Enum.WeatherType;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service for managing visual weather effects, placeables, skyboxes, and fog.
    /// </summary>
    public class WeatherVisualService : IWeatherVisualService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IWeatherCalculationService> _weatherCalculationService;
        private readonly Lazy<IWeatherClimateService> _weatherClimateService;
        private readonly Dictionary<uint, List<uint>> _areaWeatherPlaceables = new();

        // Module and area variables.
        private const string VAR_WEATHER_ACID_RAIN = "VAR_WEATHER_ACID_RAIN";
        private const string VAR_INITIALIZED = "VAR_WH_INITIALIZED";
        private const string VAR_SKYBOX = "VAR_WH_SKYBOX";
        private const string VAR_FOG_SUN = "VAR_WH_FOG_SUN";
        private const string VAR_FOG_MOON = "VAR_WH_FOG_MOON";
        private const string VAR_FOG_C_SUN = "VAR_WH_FOG_C_SUN";
        private const string VAR_FOG_C_MOON = "VAR_WH_FOG_C_MOON";

        public WeatherVisualService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _weatherCalculationService = new Lazy<IWeatherCalculationService>(() => _serviceProvider.GetRequiredService<IWeatherCalculationService>());
            _weatherClimateService = new Lazy<IWeatherClimateService>(() => _serviceProvider.GetRequiredService<IWeatherClimateService>());
        }
        
        // Lazy-loaded services to break circular dependencies
        private IWeatherCalculationService WeatherCalculationService => _weatherCalculationService.Value;
        private IWeatherClimateService WeatherClimateService => _weatherClimateService.Value;

        /// <summary>
        /// Sets the weather for the current area.
        /// </summary>
        public void SetWeather()
        {
            SetWeather(OBJECT_SELF);
        }

        /// <summary>
        /// Sets the weather for a specific area.
        /// </summary>
        /// <param name="oArea">The area to set weather for.</param>
        public void SetWeather(uint oArea)
        {
            if (GetLocalInt(oArea, VAR_INITIALIZED) == 0)
            {
                if (GetIsAreaInterior(oArea) ||
                    GetIsAreaAboveGround(oArea) == false)
                    return;
                SetLocalInt(oArea, VAR_SKYBOX, (int)GetSkyBox(oArea));
                SetLocalInt(oArea, VAR_FOG_SUN, GetFogAmount(FogType.Sun, oArea));
                SetLocalInt(oArea, VAR_FOG_MOON, GetFogAmount(FogType.Moon, oArea));
                SetLocalInt(oArea, VAR_FOG_C_SUN, (int)GetFogColor(FogType.Sun, oArea));
                SetLocalInt(oArea, VAR_FOG_C_MOON, (int)GetFogColor(FogType.Moon, oArea));
                SetLocalInt(oArea, VAR_INITIALIZED, 1);
            }

            var nHeat = WeatherCalculationService.GetHeatIndex(oArea);
            var nHumidity = WeatherCalculationService.GetHumidity(oArea);
            var nWind = WeatherCalculationService.GetWindStrength(oArea);
            var bStormy = GetSkyBox(oArea) == SkyboxType.GrassStorm;
            var bDustStorm = (GetLocalInt(oArea, "DUST_STORM") == 1);
            var bSandStorm = (GetLocalInt(oArea, "SAND_STORM") == 1);
            var bSnowStorm = (GetLocalInt(oArea, "SNOW_STORM") == 1);

            //--------------------------------------------------------------------------
            // Process weather rules for this area.
            //--------------------------------------------------------------------------
            if (nHumidity > 7 && nHeat > 3)
            {
                if (nHeat < 6 && nWind < 3)
                {
                    SWLOR.NWN.API.Service.NWScript.SetWeather(oArea, AreaWeatherType.Clear);
                }
                else SWLOR.NWN.API.Service.NWScript.SetWeather(oArea, AreaWeatherType.Rain);
            }
            else if (nHumidity > 7) SWLOR.NWN.API.Service.NWScript.SetWeather(oArea, AreaWeatherType.Snow);
            else SWLOR.NWN.API.Service.NWScript.SetWeather(oArea, AreaWeatherType.Clear);

            //--------------------------------------------------------------------------
            // Stormy if heat is greater than 4 only; if already stormy then 2 in 3
            // chance of storm clearing, otherwise x in 20 chance of storm starting,
            // where x is the wind level.
            //--------------------------------------------------------------------------
            if (nHeat > 4 && nHumidity > 7 &&
                ((bStormy && d20() - nWind < 1) || (bStormy && d3() == 1)))
            {
                SetSkyBox(SkyboxType.GrassStorm, oArea);
                // Note: Thunderstorm would be called by WeatherEffectsService
                SetLocalInt(oArea, "GS_AM_SKY_OVERRIDE", 1);
            }
            else
            {
                SetSkyBox((SkyboxType)GetLocalInt(oArea, VAR_SKYBOX), oArea);
                DeleteLocalInt(oArea, "GS_AM_SKY_OVERRIDE");
                bStormy = false;
            }

            // Does this area suffer from dust or sand storms?
            if (!bStormy && nWind >= 9 && d3() == 1)
            {
                // Dust storm - low visibility but no damage.
                if (WeatherClimateService.GetAreaClimate(oArea).HasSandStorms)
                {
                    SetFogColor(FogType.Sun, FogColorType.OrangeDark, oArea);
                    SetFogColor(FogType.Moon, FogColorType.OrangeDark, oArea);
                    SetFogAmount(FogType.Sun, 80, oArea);
                    SetFogAmount(FogType.Moon, 80, oArea);

                    SetLocalInt(oArea, "SAND_STORM", 1);
                }
                else if (WeatherClimateService.GetAreaClimate(oArea).HasSnowStorms)
                {
                    SetFogColor(FogType.Sun, FogColorType.White, oArea);
                    SetFogColor(FogType.Moon, FogColorType.White, oArea);
                    SetFogAmount(FogType.Sun, 80, oArea);
                    SetFogAmount(FogType.Moon, 80, oArea);

                    SetLocalInt(oArea, "SNOW_STORM", 1);
                }
            }
            else if (bDustStorm || bSandStorm || bSnowStorm)
            {
                // End the storm.
                DeleteLocalInt(oArea, "DUST_STORM");
                DeleteLocalInt(oArea, "SAND_STORM");
                DeleteLocalInt(oArea, "SNOW_STORM");

                SetFogColor(FogType.Sun, (FogColorType)GetLocalInt(oArea, VAR_FOG_C_SUN), oArea);
                SetFogColor(FogType.Moon, (FogColorType)GetLocalInt(oArea, VAR_FOG_C_MOON), oArea);
                SetFogAmount(FogType.Sun, GetLocalInt(oArea, VAR_FOG_SUN), oArea);
                SetFogAmount(FogType.Moon, GetLocalInt(oArea, VAR_FOG_MOON), oArea);
            }
        }

        /// <summary>
        /// Handles weather when a player enters an area.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void OnAreaEnter()
        {
            SetWeather();

            var oArea = (OBJECT_SELF);
            var nHour = GetTimeHour();
            var nLastHour = GetLocalInt(oArea, "WEATHER_LAST_HOUR");

            if (nHour != nLastHour)
            {
                if (!_areaWeatherPlaceables.ContainsKey(oArea))
                    _areaWeatherPlaceables[oArea] = new List<uint>();

                var weatherObjects = _areaWeatherPlaceables[oArea];

                // Clean up any old weather placeables.
                for (var x = weatherObjects.Count - 1; x >= 0; x--)
                {
                    var placeable = weatherObjects.ElementAt(x);
                    DestroyObject(placeable);
                    weatherObjects.RemoveAt(x);
                }

                // Create new ones depending on the current weather.
                var nWeather = WeatherCalculationService.GetWeather();

                if (nWeather == WeatherType.Foggy)
                {
                    // Get the size in tiles.
                    var nSizeX = GetAreaSize(AreaDimensionType.Width, oArea);
                    var nSizeY = GetAreaSize(AreaDimensionType.Height, oArea);

                    // We want one placeable per 8 tiles.
                    var nMax = (nSizeX * nSizeY) / 8;

                    for (var nCount = d6(); nCount < nMax; nCount++)
                    {
                        var vPosition = GetPosition(GetEnteringObject());

                        // Vectors are in meters - 10 meters to a tile. 
                        vPosition.X = IntToFloat(Random(nSizeX * 10));
                        vPosition.Y = IntToFloat(Random(nSizeY * 10));

                        var fFacing = IntToFloat(Random(360));

                        var sResRef = "x3_plc_mist";

                        var oPlaceable = CreateObject(ObjectType.Placeable, sResRef, Location(oArea, vPosition, fFacing));
                        SetObjectVisualTransform(oPlaceable, ObjectVisualTransformType.Scale, IntToFloat(200 + Random(200)) / 100.0f);

                        weatherObjects.Add(oPlaceable);
                    }
                }

                _areaWeatherPlaceables[oArea] = weatherObjects;
                SetLocalInt(oArea, "WEATHER_LAST_HOUR", nHour);
            }
        }
    }
}
