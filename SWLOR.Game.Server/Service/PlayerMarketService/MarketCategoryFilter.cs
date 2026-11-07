using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PlayerMarketService
{
    /// <summary>
    /// Converts the market's category toggles to and from the saved exclusions. No toggle on and every toggle on both show every
    /// listing, so neither one saves anything.
    /// </summary>
    public static class MarketCategoryFilter
    {
        public static HashSet<MarketCategoryType> GetExcluded(
            IEnumerable<MarketCategoryType> allCategories,
            IReadOnlyCollection<MarketCategoryType> activeCategories)
        {
            return activeCategories.Count == 0
                ? new HashSet<MarketCategoryType>()
                : allCategories.Except(activeCategories).ToHashSet();
        }

        public static List<MarketCategoryType> GetActive(
            IEnumerable<MarketCategoryType> allCategories,
            IReadOnlyCollection<MarketCategoryType> excludedCategories)
        {
            return excludedCategories == null || excludedCategories.Count == 0
                ? new List<MarketCategoryType>()
                : allCategories.Except(excludedCategories).ToList();
        }
    }
}
