namespace SWLOR.Component.World.Contracts
{
    public interface IWeatherService
    {
        /// <summary>
        /// Adjusts the weather based on current conditions and time.
        /// </summary>
        /// <returns>True if weather was adjusted, false otherwise.</returns>
        bool AdjustWeather();

        /// <summary>
        /// Sets the weather for the current area.
        /// </summary>
        void SetWeather();

        /// <summary>
        /// Sets the weather for a specific area.
        /// </summary>
        /// <param name="oArea">The area to set weather for.</param>
        void SetWeather(uint oArea);

        /// <summary>
        /// Gets the current weather for the current area.
        /// </summary>
        /// <returns>The current weather type.</returns>
        NWN.API.NWScript.Enum.Weather GetWeather();

        /// <summary>
        /// Gets the weather for a specific area.
        /// </summary>
        /// <param name="oArea">The area to get weather for.</param>
        /// <returns>The weather type for the area.</returns>
        NWN.API.NWScript.Enum.Weather GetWeather(uint oArea);

        /// <summary>
        /// Applies weather effects to a creature.
        /// </summary>
        /// <param name="oCreature">The creature to apply effects to.</param>
        void DoWeatherEffects(uint oCreature);

        /// <summary>
        /// Gets the heat index for an area.
        /// </summary>
        /// <param name="oArea">The area to get heat index for.</param>
        /// <returns>The heat index value.</returns>
        int GetHeatIndex(uint oArea);

        /// <summary>
        /// Gets the humidity for an area.
        /// </summary>
        /// <param name="oArea">The area to get humidity for.</param>
        /// <returns>The humidity value.</returns>
        int GetHumidity(uint oArea);

        /// <summary>
        /// Gets the wind strength for an area.
        /// </summary>
        /// <param name="oArea">The area to get wind strength for.</param>
        /// <returns>The wind strength value.</returns>
        int GetWindStrength(uint oArea);

        /// <summary>
        /// Sets the heat modifier for an area.
        /// </summary>
        /// <param name="oArea">The area to set modifier for.</param>
        /// <param name="nModifier">The heat modifier value.</param>
        void SetAreaHeatModifier(uint oArea, int nModifier);

        /// <summary>
        /// Sets the wind modifier for an area.
        /// </summary>
        /// <param name="oArea">The area to set modifier for.</param>
        /// <param name="nModifier">The wind modifier value.</param>
        void SetAreaWindModifier(uint oArea, int nModifier);

        /// <summary>
        /// Sets the humidity modifier for an area.
        /// </summary>
        /// <param name="oArea">The area to set modifier for.</param>
        /// <param name="nModifier">The humidity modifier value.</param>
        void SetAreaHumidityModifier(uint oArea, int nModifier);

        /// <summary>
        /// Sets the acid rain modifier for an area.
        /// </summary>
        /// <param name="oArea">The area to set modifier for.</param>
        /// <param name="nModifier">The acid rain modifier value.</param>
        void SetAreaAcidRain(uint oArea, int nModifier);

        /// <summary>
        /// Loads weather data when the module caches.
        /// </summary>
        void LoadData();

        /// <summary>
        /// Handles weather when a player enters an area.
        /// </summary>
        void OnAreaEnter();

        /// <summary>
        /// Handles weather updates on module heartbeat.
        /// </summary>
        void OnModuleHeartbeat();
    }
}
