using SWLOR.Component.World.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using WeatherType = SWLOR.NWN.API.NWScript.Enum.WeatherType;

namespace SWLOR.Component.World.Service
{
    public class WeatherService : IWeatherService
    {
        private readonly IWeatherCalculationService _weatherCalculationService;
        private readonly IWeatherEffectsService _weatherEffectsService;
        private readonly IWeatherVisualService _weatherVisualService;
        private readonly IWeatherClimateService _weatherClimateService;

        public WeatherService(
            IWeatherCalculationService weatherCalculationService,
            IWeatherEffectsService weatherEffectsService,
            IWeatherVisualService weatherVisualService,
            IWeatherClimateService weatherClimateService)
        {
            _weatherCalculationService = weatherCalculationService;
            _weatherEffectsService = weatherEffectsService;
            _weatherVisualService = weatherVisualService;
            _weatherClimateService = weatherClimateService;
        }

        /// <summary>
        /// When the module loads, cache planet climates and other pertinent data.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadData()
        {
            _weatherClimateService.LoadData();
        }

        public bool AdjustWeather()
        {
            return _weatherCalculationService.AdjustWeather();
        }

        public void SetWeather()
        {
            _weatherVisualService.SetWeather();
        }

        public void SetWeather(uint oArea)
        {
            _weatherVisualService.SetWeather(oArea);
        }

        public WeatherType GetWeather()
        {
            return _weatherCalculationService.GetWeather();
        }

        public WeatherType GetWeather(uint oArea)
        {
            return _weatherCalculationService.GetWeather(oArea);
        }

        public void OnCombatRoundEnd(uint oCreature)
        {
            _weatherEffectsService.OnCombatRoundEnd(oCreature);
        }

        public void ApplyAcid(uint oTarget, uint oArea)
        {
            _weatherEffectsService.ApplyAcid(oTarget, oArea);
        }

        public void ApplyCold(uint oTarget, uint oArea)
        {
            _weatherEffectsService.ApplyCold(oTarget, oArea);
        }

        public void ApplySandstorm(uint oTarget, uint oArea)
        {
            _weatherEffectsService.ApplySandstorm(oTarget, oArea);
        }

        public void ApplySnowstorm(uint oTarget, uint oArea)
        {
            _weatherEffectsService.ApplySnowstorm(oTarget, oArea);
        }

        public void DoWeatherEffects(uint oCreature)
        {
            _weatherEffectsService.DoWeatherEffects(oCreature);
        }

        public int GetHeatIndex(uint oArea)
        {
            return _weatherCalculationService.GetHeatIndex(oArea);
        }

        public int GetHumidity(uint oArea)
        {
            return _weatherCalculationService.GetHumidity(oArea);
        }

        public int GetWindStrength(uint oArea)
        {
            return _weatherCalculationService.GetWindStrength(oArea);
        }

        public void Thunderstorm(uint oArea)
        {
            _weatherEffectsService.Thunderstorm(oArea);
        }

        [ScriptHandler<OnAreaEnter>]
        public void OnAreaEnter()
        {
            _weatherVisualService.OnAreaEnter();
            DoWeatherEffects(GetEnteringObject());
        }

        [ScriptHandler(ScriptName.OnSwlorHeartbeat)]
        public void OnModuleHeartbeat()
        {
            var oMod = GetModule();
            var nHour = GetTimeHour();
            var nLastHour = GetLocalInt(oMod, "WEATHER_LAST_HOUR");

            if (nHour != nLastHour)
            {
                if (AdjustWeather())
                {
                    for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                    {
                        DoWeatherEffects(player);
                    }
                }

                SetLocalInt(oMod, "WEATHER_LAST_HOUR", nHour);
            }
        }

        public void SetAreaHeatModifier(uint oArea, int nModifier)
        {
            _weatherCalculationService.SetAreaHeatModifier(oArea, nModifier);
        }

        public void SetAreaWindModifier(uint oArea, int nModifier)
        {
            _weatherCalculationService.SetAreaWindModifier(oArea, nModifier);
        }

        public void SetAreaHumidityModifier(uint oArea, int nModifier)
        {
            _weatherCalculationService.SetAreaHumidityModifier(oArea, nModifier);
        }

        public void SetAreaAcidRain(uint oArea, int nModifier)
        {
            _weatherCalculationService.SetAreaAcidRain(oArea, nModifier);
        }
    }
}
