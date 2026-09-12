namespace SWLOR.Game.Server.Service.WeatherService
{
    /// <summary>
    /// One damage clock per player, shared by area entry and the module heartbeat.
    /// Taking shelter stops damage without allowing rapid re-entry to stack pulses.
    /// </summary>
    public sealed class WeatherExposure
    {
        private DateTime _nextDamageUtc;
        private WeatherHazard _previousHazard;

        public int GetDamageDice(DateTime now, WeatherHazard hazard)
        {
            if (hazard == WeatherHazard.None)
            {
                _previousHazard = hazard;
                return 0;
            }

            if (now < _nextDamageUtc)
                return 0;

            var dice = hazard == WeatherHazard.Acid && _previousHazard == hazard ? 1 : 2;
            _previousHazard = hazard;
            _nextDamageUtc = now.AddSeconds(6);
            return dice;
        }
    }
}
