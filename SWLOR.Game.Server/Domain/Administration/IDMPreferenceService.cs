using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Loads and saves DM chat and display preferences, keyed by the DM's public CD key. The static facade
    /// <c>Service.DMPreference</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface IDMPreferenceService
    {
        /// <summary>
        /// Returns the preferences of the DM acting through an avatar or a possessed creature, or the defaults when
        /// none are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        DMPreferences Get(uint actor);

        /// <summary>
        /// Changes and saves the preferences of the DM acting through an avatar or a possessed creature.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="change">The change to make.</param>
        /// <returns>false when the DM's CD key could not be read, so nothing was saved.</returns>
        bool Update(uint actor, Action<DMPreferences> change);
    }
}
