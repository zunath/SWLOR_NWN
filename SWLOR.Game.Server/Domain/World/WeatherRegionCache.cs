using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Domain.World
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

        /// <summary>Every region with its state, for a hot reload to carry (<c>WeatherService.CarryWeather</c>).</summary>
        public IEnumerable<(WeatherClimate Climate, int HeatModifier, int HumidityModifier, int WindModifier, WeatherAreaState State)> Entries()
        {
            return _regions.Select(pair => (pair.Key.Climate, pair.Key.Heat, pair.Key.Humidity, pair.Key.Wind, pair.Value));
        }

        /// <summary>Puts back a region a hot reload carried (<c>WeatherService.AdoptWeather</c>). Nothing else calls it.</summary>
        public void Restore(WeatherClimate climate, int heatModifier, int humidityModifier, int windModifier, WeatherAreaState state)
        {
            _regions[(climate, heatModifier, humidityModifier, windModifier)] = state;
        }
    }
}
