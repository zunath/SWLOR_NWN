using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Loads and saves each DM's Creature Manager favorites, keyed by the DM's public CD key. Favorites are personal:
    /// they never change the saved creatures other DMs see.
    /// </summary>
    public static class DMCreatureManagerFavorite
    {
        // The DB entity cache is keyed by id alone, so each per-DM record type needs its own prefix.
        private const string IdPrefix = "DMCreatureManagerFavorites:";

        /// <summary>
        /// Returns the favorites of the DM acting through an avatar or a possessed creature, or an empty list when none
        /// are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static List<DMSavedCreatureFavorite> Get(uint actor)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return new List<DMSavedCreatureFavorite>();

            return DB.Get<DMCreatureManagerFavorites>(id)?.Creatures ?? new List<DMSavedCreatureFavorite>();
        }

        /// <summary>
        /// Adds the saved creature to the DM's favorites, or removes it when it is already one, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="creatureId">The saved creature's record id.</param>
        /// <param name="name">The saved creature's name.</param>
        /// <param name="favorites">The DM's saved favorites after the change.</param>
        public static DMFavoriteChange Toggle(uint actor, string creatureId, string name, out List<DMSavedCreatureFavorite> favorites)
        {
            var record = GetRecord(actor);
            if (record == null)
            {
                favorites = new List<DMSavedCreatureFavorite>();
                return DMFavoriteChange.NotSaved;
            }

            // Read the saved list rather than the window's copy, so a second session on the same key is not overwritten.
            var change = DMSavedCreatureFavoriteList.Toggle(record.Creatures, creatureId, name);
            if (change != DMFavoriteChange.Full)
                DB.Set(record);

            favorites = record.Creatures;
            return change;
        }

        /// <summary>
        /// Removes a saved creature from the DM's favorites, if it is one, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="creatureId">The saved creature's record id.</param>
        /// <returns>The DM's saved favorites after the change.</returns>
        public static List<DMSavedCreatureFavorite> Remove(uint actor, string creatureId)
        {
            var record = GetRecord(actor);
            if (record == null)
                return new List<DMSavedCreatureFavorite>();

            if (DMSavedCreatureFavoriteList.Remove(record.Creatures, creatureId))
                DB.Set(record);

            return record.Creatures;
        }

        private static DMCreatureManagerFavorites GetRecord(uint actor)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return null;

            return DB.Get<DMCreatureManagerFavorites>(id) ?? new DMCreatureManagerFavorites { Id = id };
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
