using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using WeatherType = SWLOR.NWN.API.NWScript.Enum.WeatherType;

namespace SWLOR.Component.World.Service
{
    public class WeatherService : IWeatherService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IWeatherCalculationService> _weatherCalculationService;
        private readonly Lazy<IWeatherEffectsService> _weatherEffectsService;
        private readonly Lazy<IWeatherVisualService> _weatherVisualService;
        private readonly Lazy<IWeatherClimateService> _weatherClimateService;

        public WeatherService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _weatherCalculationService = new Lazy<IWeatherCalculationService>(() => _serviceProvider.GetRequiredService<IWeatherCalculationService>());
            _weatherEffectsService = new Lazy<IWeatherEffectsService>(() => _serviceProvider.GetRequiredService<IWeatherEffectsService>());
            _weatherVisualService = new Lazy<IWeatherVisualService>(() => _serviceProvider.GetRequiredService<IWeatherVisualService>());
            _weatherClimateService = new Lazy<IWeatherClimateService>(() => _serviceProvider.GetRequiredService<IWeatherClimateService>());
        }
        
        // Lazy-loaded services to break circular dependencies
        private IWeatherCalculationService WeatherCalculationService => _weatherCalculationService.Value;
        private IWeatherEffectsService WeatherEffectsService => _weatherEffectsService.Value;
        private IWeatherVisualService WeatherVisualService => _weatherVisualService.Value;
        private IWeatherClimateService WeatherClimateService => _weatherClimateService.Value;

        /// <summary>
        /// When the module loads, cache planet climates and other pertinent data.
        /// </summary>
        public void LoadData()
        {
            WeatherClimateService.LoadData();
        }

        public bool AdjustWeather()
        {
            return WeatherCalculationService.AdjustWeather();
        }

        public void SetWeather()
        {
            WeatherVisualService.SetWeather();
        }

        public void SetWeather(uint oArea)
        {
            WeatherVisualService.SetWeather(oArea);
        }

        public WeatherType GetWeather()
        {
            return WeatherCalculationService.GetWeather();
        }

        public WeatherType GetWeather(uint oArea)
        {
            return WeatherCalculationService.GetWeather(oArea);
        }

        public void OnCombatRoundEnd(uint oCreature)
        {
            WeatherEffectsService.OnCombatRoundEnd(oCreature);
        }

        public void ApplyAcid(uint oTarget, uint oArea)
        {
            WeatherEffectsService.ApplyAcid(oTarget, oArea);
        }

        public void ApplyCold(uint oTarget, uint oArea)
        {
            WeatherEffectsService.ApplyCold(oTarget, oArea);
        }

        public void ApplySandstorm(uint oTarget, uint oArea)
        {
            WeatherEffectsService.ApplySandstorm(oTarget, oArea);
        }

        public void ApplySnowstorm(uint oTarget, uint oArea)
        {
            WeatherEffectsService.ApplySnowstorm(oTarget, oArea);
        }

        public void DoWeatherEffects(uint oCreature)
        {
            WeatherEffectsService.DoWeatherEffects(oCreature);
        }

        public int GetHeatIndex(uint oArea)
        {
            return WeatherCalculationService.GetHeatIndex(oArea);
        }

        public int GetHumidity(uint oArea)
        {
            return WeatherCalculationService.GetHumidity(oArea);
        }

        public int GetWindStrength(uint oArea)
        {
            return WeatherCalculationService.GetWindStrength(oArea);
        }

        public void Thunderstorm(uint oArea)
        {
            WeatherEffectsService.Thunderstorm(oArea);
        }

        public void OnAreaEnter()
        {
            WeatherVisualService.OnAreaEnter();
            DoWeatherEffects(GetEnteringObject());
        }

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
            WeatherCalculationService.SetAreaHeatModifier(oArea, nModifier);
        }

        public void SetAreaWindModifier(uint oArea, int nModifier)
        {
            WeatherCalculationService.SetAreaWindModifier(oArea, nModifier);
        }

        public void SetAreaHumidityModifier(uint oArea, int nModifier)
        {
            WeatherCalculationService.SetAreaHumidityModifier(oArea, nModifier);
        }

        public void SetAreaAcidRain(uint oArea, int nModifier)
        {
            WeatherCalculationService.SetAreaAcidRain(oArea, nModifier);
        }
    }
}
