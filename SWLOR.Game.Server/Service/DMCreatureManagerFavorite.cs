using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of each DM's Creature Manager favorites, keyed by the DM's public CD key. Favorites are personal:
    /// they never change the saved creatures other DMs see. The logic lives in
    /// <see cref="IDMCreatureManagerFavoriteService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class DMCreatureManagerFavorite
    {
        private static IDMCreatureManagerFavoriteService Instance => FacadeAccessor.Resolve<IDMCreatureManagerFavoriteService>();

        /// <summary>
        /// Returns the favorites of the DM acting through an avatar or a possessed creature, or an empty list when none
        /// are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static List<DMSavedCreatureFavorite> Get(uint actor)
        {
            return Instance.Get(actor);
        }

        /// <summary>
        /// Adds the saved creature to the DM's favorites or removes it, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="creatureId">The saved creature's record id.</param>
        /// <param name="name">The saved creature's name.</param>
        /// <param name="isFavorite">True to add the saved creature, false to remove it.</param>
        /// <param name="favorites">The DM's saved favorites after the change.</param>
        public static DMFavoriteChange Set(uint actor, string creatureId, string name, bool isFavorite, out List<DMSavedCreatureFavorite> favorites)
        {
            return Instance.Set(actor, creatureId, name, isFavorite, out favorites);
        }

        /// <summary>
        /// Removes a saved creature from the DM's favorites, if it is one, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="creatureId">The saved creature's record id.</param>
        /// <returns>The DM's saved favorites after the change.</returns>
        public static List<DMSavedCreatureFavorite> Remove(uint actor, string creatureId)
        {
            return Instance.Remove(actor, creatureId);
        }
    }
}
