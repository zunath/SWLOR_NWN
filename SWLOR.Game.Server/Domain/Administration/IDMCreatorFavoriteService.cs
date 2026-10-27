using System.Collections.Generic;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Loads and saves each DM's DM Creator favorites, keyed by the DM's public CD key. Favorites are personal: they
    /// never change the palettes other DMs see. The static facade <c>Service.DMCreatorFavorite</c> forwards to the
    /// container's instance of this interface.
    /// </summary>
    public interface IDMCreatorFavoriteService
    {
        /// <summary>
        /// Returns the favorites of the DM acting through an avatar or a possessed creature, or an empty list when none
        /// are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        List<DMFavoriteBlueprint> Get(uint actor);

        /// <summary>
        /// Adds the blueprint to the DM's favorites, or removes it when it is already one, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="blueprint">The blueprint to add or remove.</param>
        /// <param name="favorites">The DM's saved favorites after the change.</param>
        DMFavoriteChange Toggle(uint actor, DMBlueprint blueprint, out List<DMFavoriteBlueprint> favorites);
    }
}
