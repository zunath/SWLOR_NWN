using Precipitation = SWLOR.NWN.API.NWScript.Enum.Weather;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The weather of the outdoor areas: the shared weather front, each area's conditions, native precipitation, fog,
    /// mist and storms, and the damage the hazardous weather deals. The static facade <c>Service.Weather</c> forwards
    /// to the container's instance of this interface and keeps the event handlers.
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// When the module's caches are built, restores the climate tables and forgets every area, region and exposure.
        /// </summary>
        void LoadData();

        /// <summary>
        /// When the module loads, rolls the first weather front.
        /// </summary>
        void InitializeWeather();

        /// <summary>
        /// Advances the weather front when its hour is up and refreshes every area.
        /// </summary>
        /// <returns>True when the front changed.</returns>
        bool AdjustWeather();

        /// <summary>
        /// Applies the current weather to the calling area (<c>OBJECT_SELF</c>).
        /// </summary>
        void SetWeather();

        /// <summary>
        /// Applies the current weather to an area when it has not seen the current front yet.
        /// </summary>
        /// <param name="area">The area</param>
        void SetWeather(uint area);

        /// <summary>
        /// Retrieves the precipitation of the calling area (<c>OBJECT_SELF</c>).
        /// </summary>
        Precipitation GetWeather();

        /// <summary>
        /// Retrieves the precipitation of an area, or <see cref="Precipitation.Invalid"/> when it has no weather.
        /// </summary>
        /// <param name="area">The area</param>
        Precipitation GetWeather(uint area);

        /// <summary>
        /// Retrieves the heat index of an area, from 1 to 10 outside the area's own conditions.
        /// </summary>
        /// <param name="area">The area</param>
        int GetHeatIndex(uint area);

        /// <summary>
        /// Retrieves the humidity of an area, from 1 to 10 outside the area's own conditions.
        /// </summary>
        /// <param name="area">The area</param>
        int GetHumidity(uint area);

        /// <summary>
        /// Retrieves the wind strength of an area, from 1 to 10 outside the area's own conditions.
        /// </summary>
        /// <param name="area">The area</param>
        int GetWindStrength(uint area);

        /// <summary>
        /// Tells a player what the weather is like where they stand and applies the weather's damage.
        /// </summary>
        /// <param name="creature">The player's creature</param>
        void DoWeatherEffects(uint creature);

        /// <summary>
        /// Rolls for a lightning strike in an area that has a thunderstorm.
        /// </summary>
        /// <param name="area">The area</param>
        void Thunderstorm(uint area);

        /// <summary>
        /// When a creature enters an area, applies the area's weather, raises its mist for a player and describes it.
        /// </summary>
        void OnAreaEnter();

        /// <summary>
        /// Every heartbeat of the module: advances the front, applies the weather's damage to every player, forgets the
        /// exposure of players who left and rolls lightning in the occupied areas.
        /// </summary>
        void OnModuleHeartbeat();

        /// <summary>
        /// Sets an area's heat modifier and refreshes its weather.
        /// </summary>
        /// <param name="area">The area</param>
        /// <param name="modifier">The modifier</param>
        void SetAreaHeatModifier(uint area, int modifier);

        /// <summary>
        /// Sets an area's wind modifier and refreshes its weather.
        /// </summary>
        /// <param name="area">The area</param>
        /// <param name="modifier">The modifier</param>
        void SetAreaWindModifier(uint area, int modifier);

        /// <summary>
        /// Sets an area's humidity modifier and refreshes its weather.
        /// </summary>
        /// <param name="area">The area</param>
        /// <param name="modifier">The modifier</param>
        void SetAreaHumidityModifier(uint area, int modifier);

        /// <summary>
        /// Turns an area's acid rain on (1) or off (0).
        /// </summary>
        /// <param name="area">The area</param>
        /// <param name="modifier">1 for acid rain, 0 for none</param>
        void SetAreaAcidRain(uint area, int modifier);
    }
}
