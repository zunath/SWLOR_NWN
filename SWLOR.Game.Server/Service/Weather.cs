using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the area weather. The logic and state live in <see cref="IWeatherService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class Weather
    {
        private static IWeatherService Instance => FacadeAccessor.Resolve<IWeatherService>();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadData()
        {
            Instance.LoadData();
        }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void InitializeWeather()
        {
            Instance.InitializeWeather();
        }

        public static bool AdjustWeather()
        {
            return Instance.AdjustWeather();
        }

        public static void SetWeather()
        {
            Instance.SetWeather();
        }

        public static void SetWeather(uint area)
        {
            Instance.SetWeather(area);
        }

        public static Precipitation GetWeather()
        {
            return Instance.GetWeather();
        }

        public static Precipitation GetWeather(uint area)
        {
            return Instance.GetWeather(area);
        }

        public static int GetHeatIndex(uint area)
        {
            return Instance.GetHeatIndex(area);
        }

        public static int GetHumidity(uint area)
        {
            return Instance.GetHumidity(area);
        }

        public static int GetWindStrength(uint area)
        {
            return Instance.GetWindStrength(area);
        }

        public static void DoWeatherEffects(uint creature)
        {
            Instance.DoWeatherEffects(creature);
        }

        public static void Thunderstorm(uint area)
        {
            Instance.Thunderstorm(area);
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter()
        {
            Instance.OnAreaEnter();
        }

        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void OnModuleHeartbeat()
        {
            Instance.OnModuleHeartbeat();
        }

        public static void SetAreaHeatModifier(uint area, int modifier)
        {
            Instance.SetAreaHeatModifier(area, modifier);
        }

        public static void SetAreaWindModifier(uint area, int modifier)
        {
            Instance.SetAreaWindModifier(area, modifier);
        }

        public static void SetAreaHumidityModifier(uint area, int modifier)
        {
            Instance.SetAreaHumidityModifier(area, modifier);
        }

        public static void SetAreaAcidRain(uint area, int modifier)
        {
            Instance.SetAreaAcidRain(area, modifier);
        }
    }
}
