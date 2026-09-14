namespace SWLOR.Game.Server.Service.WeatherService
{
    /// <summary>
    /// The shared weather front. Game-clock changes and area traffic must not advance it.
    /// </summary>
    public sealed class WeatherPattern
    {
        public static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(1);

        public int Heat { get; private set; }
        public int Humidity { get; private set; }
        public int Wind { get; private set; }
        public int Revision { get; private set; }
        public DateTime NextChangeUtc { get; private set; }

        public bool TryAdvance(DateTime now, int month, bool isNight, Func<int, int> random)
        {
            if (Revision > 0 && now < NextChangeUtc)
                return false;

            var seasonalHeat = Math.Max(1, random(5) + 6 - Math.Abs(month - 6));
            var targetHeat = seasonalHeat + (isNight ? -2 : 2);
            if (Revision == 0)
            {
                Heat = targetHeat;
                Humidity = random(10) + 1;
            }
            else
            {
                // Include dawn/dusk in the gradual change, rather than applying an
                // instantaneous four-point temperature jump to each area.
                Heat += Math.Sign(targetHeat - Heat);
                var humidityChange = random(2 * Wind + 1) - Wind;
                Humidity = Math.Clamp(Humidity + Math.Sign(humidityChange), 1, 10);
            }

            Wind = random(10) + random(10) - 8;
            if (Wind < 1) Wind = 1 - Wind;

            Revision++;
            // Do not catch up missed updates in a burst after a long pause.
            NextChangeUtc = now + UpdateInterval;
            return true;
        }
    }
}
