namespace SWLOR.Game.Server.Service.WeatherService
{
    /// <summary>Retains the same area's conditions throughout a weather front.</summary>
    public class WeatherAreaState
    {
        public int Revision { get; private set; }
        public WeatherConditions Conditions { get; private set; }

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
    }
}
