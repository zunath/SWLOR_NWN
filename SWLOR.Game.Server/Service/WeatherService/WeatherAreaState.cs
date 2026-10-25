using SWLOR.NWN.API.NWScript.Enum.Area;
using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;

namespace SWLOR.Game.Server.Service.WeatherService
{
    /// <summary>Retains the same area's conditions throughout a weather front.</summary>
    public class WeatherAreaState
    {
        public int Revision { get; private set; }
        public WeatherConditions Conditions { get; private set; }

        /// <summary>The native weather last sent to the area, or null before the first send.</summary>
        public WeatherType? AppliedWeather { get; private set; }

        /// <summary>
        /// Returns true when the area's native weather must be sent. Clients restart
        /// precipitation as a light shower whenever SetWeather repeats the current
        /// weather, so a front that keeps the same rain or snow must not resend it.
        /// </summary>
        public bool TryApplyWeather(WeatherType target, Precipitation current)
        {
            if (AppliedWeather == target && current == (Precipitation)target) return false;
            AppliedWeather = target;
            return true;
        }

        public bool TryUpdate(WeatherPattern pattern, WeatherClimate climate,
            int heatModifier, int humidityModifier, int windModifier, bool isNatural, Func<int, int> random)
        {
            if (Revision == pattern.Revision) return false;

            Conditions = WeatherConditions.Create(pattern.Heat, pattern.Humidity, pattern.Wind,
                climate, heatModifier, humidityModifier, windModifier, isNatural,
                Conditions?.Storm ?? WeatherStorm.None, random);
            Revision = pattern.Revision;
            return true;
        }

        public void Invalidate() => Revision = 0;

        public bool TryUpdate(WeatherPattern pattern, WeatherClimate climate,
            int heatModifier, int humidityModifier, int windModifier, bool isNatural, WeatherStorm regionalStorm)
        {
            if (Revision == pattern.Revision) return false;

            Conditions = WeatherConditions.CreateForArea(pattern.Heat, pattern.Humidity, pattern.Wind,
                climate, heatModifier, humidityModifier, windModifier, isNatural, regionalStorm);
            Revision = pattern.Revision;
            return true;
        }
    }
}
