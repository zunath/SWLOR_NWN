using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Loads and saves DM chat and display preferences, keyed by the DM's public CD key.
    /// </summary>
    public static class DMPreference
    {
        // The DB entity cache is keyed by id alone, and Account already uses the bare CD key.
        private const string IdPrefix = "DM:";

        /// <summary>
        /// Returns the preferences of the DM acting through an avatar or a possessed creature, or the defaults when
        /// none are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static DMPreferences Get(uint actor)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return new DMPreferences();

            return DB.Get<DMPreferences>(id) ?? new DMPreferences { Id = id };
        }

        /// <summary>
        /// Changes and saves the preferences of the DM acting through an avatar or a possessed creature.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="change">The change to make.</param>
        /// <returns>false when the DM's CD key could not be read, so nothing was saved.</returns>
        public static bool Update(uint actor, Action<DMPreferences> change)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return false;

            var preferences = DB.Get<DMPreferences>(id) ?? new DMPreferences { Id = id };
            change(preferences);
            DB.Set(preferences);
            return true;
        }

        private static string GetId(uint actor)
        {
            // The CD key resolves from the object the player controls, which is the possessed creature while possessing.
            var cdKey = GetPCPublicCDKey(DMPresence.GetControlledCreature(actor));
            return string.IsNullOrWhiteSpace(cdKey)
                ? string.Empty
                : IdPrefix + cdKey;
        }
    }
}
