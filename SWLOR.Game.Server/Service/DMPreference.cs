using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the DM chat and display preferences, keyed by the DM's public CD key. The logic lives in
    /// <see cref="IDMPreferenceService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class DMPreference
    {
        private static IDMPreferenceService Instance => FacadeAccessor.Resolve<IDMPreferenceService>();

        /// <summary>
        /// Returns the preferences of the DM acting through an avatar or a possessed creature, or the defaults when
        /// none are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static DMPreferences Get(uint actor)
        {
            return Instance.Get(actor);
        }

        /// <summary>
        /// Changes and saves the preferences of the DM acting through an avatar or a possessed creature.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="change">The change to make.</param>
        /// <returns>false when the DM's CD key could not be read, so nothing was saved.</returns>
        public static bool Update(uint actor, Action<DMPreferences> change)
        {
            return Instance.Update(actor, change);
        }
    }
}
