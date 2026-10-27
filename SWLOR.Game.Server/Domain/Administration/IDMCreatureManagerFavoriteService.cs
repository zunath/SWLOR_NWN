using System.Collections.Generic;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Loads and saves each DM's Creature Manager favorites, keyed by the DM's public CD key. Favorites are personal:
    /// they never change the saved creatures other DMs see. The static facade <c>Service.DMCreatureManagerFavorite</c>
    /// forwards to the container's instance of this interface.
    /// </summary>
    public interface IDMCreatureManagerFavoriteService
    {
        /// <summary>
        /// Returns the favorites of the DM acting through an avatar or a possessed creature, or an empty list when none
        /// are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        List<DMSavedCreatureFavorite> Get(uint actor);

        /// <summary>
        /// Adds the saved creature to the DM's favorites or removes it, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="creatureId">The saved creature's record id.</param>
        /// <param name="name">The saved creature's name.</param>
        /// <param name="isFavorite">True to add the saved creature, false to remove it.</param>
        /// <param name="favorites">The DM's saved favorites after the change.</param>
        DMFavoriteChange Set(uint actor, string creatureId, string name, bool isFavorite, out List<DMSavedCreatureFavorite> favorites);

        /// <summary>
        /// Removes a saved creature from the DM's favorites, if it is one, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="creatureId">The saved creature's record id.</param>
        /// <returns>The DM's saved favorites after the change.</returns>
        List<DMSavedCreatureFavorite> Remove(uint actor, string creatureId);
    }
}
