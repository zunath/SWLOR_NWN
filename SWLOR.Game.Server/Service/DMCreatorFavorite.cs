using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of each DM's DM Creator favorites, keyed by the DM's public CD key. Favorites are personal: they
    /// never change the palettes other DMs see. The logic lives in <see cref="IDMCreatorFavoriteService"/>; every
    /// member here forwards to the container's instance.
    /// </summary>
    public static class DMCreatorFavorite
    {
        private static IDMCreatorFavoriteService Instance => FacadeAccessor.Resolve<IDMCreatorFavoriteService>();

        /// <summary>
        /// Returns the favorites of the DM acting through an avatar or a possessed creature, or an empty list when none
        /// are saved.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static List<DMFavoriteBlueprint> Get(uint actor)
        {
            return Instance.Get(actor);
        }

        /// <summary>
        /// Adds the blueprint to the DM's favorites, or removes it when it is already one, and saves the change.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="blueprint">The blueprint to add or remove.</param>
        /// <param name="favorites">The DM's saved favorites after the change.</param>
        public static DMFavoriteChange Toggle(uint actor, DMBlueprint blueprint, out List<DMFavoriteBlueprint> favorites)
        {
            return Instance.Toggle(actor, blueprint, out favorites);
        }
    }
}
