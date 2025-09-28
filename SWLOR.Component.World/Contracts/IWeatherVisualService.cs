namespace SWLOR.Component.World.Contracts
{
    /// <summary>
    /// Service for managing visual weather effects, placeables, skyboxes, and fog.
    /// </summary>
    public interface IWeatherVisualService
    {
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
        /// Handles weather when a player enters an area.
        /// </summary>
        void OnAreaEnter();
    }
}
