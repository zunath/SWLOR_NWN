using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.WeatherService
{
    /// <summary>Shares a storm front across maps with the same authored climate and modifiers.</summary>
    public sealed class WeatherRegionCache
    {
        private readonly Dictionary<(WeatherClimate Climate, int Heat, int Humidity, int Wind), WeatherAreaState> _regions = new();

        public WeatherConditions GetConditions(WeatherPattern pattern, WeatherClimate climate,
            int heatModifier, int humidityModifier, int windModifier, Func<int, int> random)
        {
            var key = (climate, heatModifier, humidityModifier, windModifier);
            if (!_regions.TryGetValue(key, out var region))
            {
                region = new WeatherAreaState();
                _regions.Add(key, region);
            }

            region.TryUpdate(pattern, climate, heatModifier, humidityModifier, windModifier, true, random);
            return region.Conditions;
        }

        public void Clear() => _regions.Clear();
    }
}
