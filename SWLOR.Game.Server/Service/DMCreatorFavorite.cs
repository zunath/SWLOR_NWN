using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Loads and saves each DM's DM Creator favorites, keyed by the DM's public CD key. Favorites are personal: they
    /// never change the palettes other DMs see.
    /// </summary>
    public static class DMCreatorFavorite
    {
        // The DB entity cache is keyed by id alone, and DMPreferences already uses "DM:" with the CD key.
        private const string IdPrefix = "DMCreatorFavorites:";

        /// <summary>
        /// Returns the favorites of the DM acting through an avatar or a possessed creature, or an empty list when none
        /// are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static List<DMFavoriteBlueprint> Get(uint actor)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return new List<DMFavoriteBlueprint>();

            return DB.Get<DMCreatorFavorites>(id)?.Blueprints ?? new List<DMFavoriteBlueprint>();
        }

        /// <summary>
        /// Adds the blueprint to the DM's favorites, or removes it when it is already one, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="blueprint">The blueprint to add or remove.</param>
        /// <param name="favorites">The DM's saved favorites after the change.</param>
        public static DMFavoriteChange Toggle(uint actor, DMBlueprint blueprint, out List<DMFavoriteBlueprint> favorites)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
            {
                favorites = new List<DMFavoriteBlueprint>();
                return DMFavoriteChange.NotSaved;
            }

            // Read the saved list rather than the window's copy, so a second session on the same key is not overwritten.
            var record = DB.Get<DMCreatorFavorites>(id) ?? new DMCreatorFavorites { Id = id };
            var change = DMFavoriteList.Toggle(record.Blueprints, blueprint);
            if (change != DMFavoriteChange.Full)
                DB.Set(record);

            favorites = record.Blueprints;
            return change;
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
