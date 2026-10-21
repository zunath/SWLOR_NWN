using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// Adds and removes Creature Manager favorites and lays out the pages of the favorites list.
    /// </summary>
    public static class DMSavedCreatureFavoriteList
    {
        /// <summary>
        /// The most saved creatures one DM can keep as favorites.
        /// </summary>
        public const int MaxFavorites = 500;

        /// <summary>
        /// Returns true when the saved creature is one of the favorites.
        /// </summary>
        public static bool Contains(IEnumerable<DMSavedCreatureFavorite> favorites, string creatureId)
        {
            return favorites.Any(favorite => favorite.CreatureId == creatureId);
        }

        /// <summary>
        /// Adds or removes the saved creature. Adding one that is already a favorite, or removing one that is not,
        /// changes nothing, so a button acts on the state it shows even if another session changed the list.
        /// </summary>
        /// <param name="favorites">The DM's favorites, changed in place.</param>
        /// <param name="creatureId">The saved creature's record id.</param>
        /// <param name="name">The saved creature's name, kept for when the record is deleted.</param>
        /// <param name="isFavorite">True to add the saved creature, false to remove it.</param>
        public static DMFavoriteChange Set(List<DMSavedCreatureFavorite> favorites, string creatureId, string name, bool isFavorite)
        {
            if (!isFavorite)
            {
                Remove(favorites, creatureId);
                return DMFavoriteChange.Removed;
            }

            if (Contains(favorites, creatureId))
                return DMFavoriteChange.Added;

            if (favorites.Count >= MaxFavorites)
                return DMFavoriteChange.Full;

            favorites.Add(new DMSavedCreatureFavorite(creatureId, name));
            return DMFavoriteChange.Added;
        }

        /// <summary>
        /// Removes the saved creature from the favorites, returning false when it was not one.
        /// </summary>
        public static bool Remove(List<DMSavedCreatureFavorite> favorites, string creatureId)
        {
            return favorites.RemoveAll(favorite => favorite.CreatureId == creatureId) > 0;
        }

        /// <summary>
        /// Splits one page of the favorites list between the saved creatures that still exist, which come first in
        /// name order, and the deleted ones listed after them.
        /// </summary>
        /// <param name="savedTotal">How many favorites still have a saved creature and match the search.</param>
        /// <param name="deletedTotal">How many favorites were deleted and match the search.</param>
        /// <param name="pageIndex">The page shown, from 0.</param>
        /// <param name="pageSize">The rows on a page.</param>
        public static (int SavedOffset, int SavedCount, int DeletedOffset, int DeletedCount) GetPage(
            long savedTotal, int deletedTotal, int pageIndex, int pageSize)
        {
            var start = (long)Math.Max(0, pageIndex) * pageSize;
            var savedCount = (int)Math.Clamp(savedTotal - start, 0, pageSize);
            var deletedOffset = (int)Math.Max(0, start - savedTotal);
            var deletedCount = Math.Clamp(Math.Min(pageSize - savedCount, deletedTotal - deletedOffset), 0, pageSize);
            return ((int)Math.Min(start, savedTotal), savedCount, deletedOffset, deletedCount);
        }
    }
}
