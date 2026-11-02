using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// The rules of the city Market Stall (Player City Revamp, "Living cities"): a city may place one stall for each city
    /// level, and a city that drops a level closes its newest stalls above the limit. Pure, so each rule can be tested.
    /// </summary>
    public static class MarketStallRules
    {
        /// <summary>How many stalls a city of this level may have: one for each level.</summary>
        public static int GetLimit(int cityLevel)
        {
            return Math.Max(0, cityLevel);
        }

        /// <summary>The refusal for placing another stall, or an empty string when the city has room.</summary>
        /// <param name="cityLevel">The city's level.</param>
        /// <param name="stallCount">How many stalls the city already has, open or closed.</param>
        public static string GetPlacementError(int cityLevel, int stallCount)
        {
            var limit = GetLimit(cityLevel);
            if (stallCount < limit)
                return string.Empty;

            return limit == 1
                ? "A city of this level may have 1 market stall. Raise the city level for more."
                : $"A city of this level may have {limit} market stalls. Raise the city level for more.";
        }

        /// <summary>
        /// The stalls that stay open: the oldest ones, as many as the city's level allows. A stall past the limit is
        /// closed (no vendor) and keeps standing, and reopens when the city has room again.
        /// </summary>
        /// <param name="stalls">Every stall of the city, with the time each was placed.</param>
        /// <param name="cityLevel">The city's level.</param>
        public static HashSet<string> SelectOpenStalls(IEnumerable<(string Id, DateTime PlacedUtc)> stalls, int cityLevel)
        {
            return (stalls ?? Enumerable.Empty<(string Id, DateTime PlacedUtc)>())
                .OrderBy(stall => stall.PlacedUtc)
                .ThenBy(stall => stall.Id, StringComparer.Ordinal)
                .Take(GetLimit(cityLevel))
                .Select(stall => stall.Id)
                .ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>The theme a stall sells under: an unset theme means general goods.</summary>
        public static MarketStallTheme NormalizeTheme(MarketStallTheme theme)
        {
            return Enum.IsDefined(typeof(MarketStallTheme), theme) && theme != MarketStallTheme.Invalid
                ? theme
                : MarketStallTheme.GeneralGoods;
        }

        /// <summary>What the mayor reads for a theme.</summary>
        public static string DescribeTheme(MarketStallTheme theme)
        {
            return NormalizeTheme(theme) switch
            {
                MarketStallTheme.CraftingSupplies => "Crafting supplies",
                MarketStallTheme.Decor => "Decor",
                _ => "General goods"
            };
        }

        /// <summary>The name a stall's placeable and vendor carry.</summary>
        public static string GetStallName(string structureName, bool isOpen)
        {
            var name = string.IsNullOrWhiteSpace(structureName) ? "Market Stall" : structureName.Trim();
            return isOpen ? name : $"{name} (Closed)";
        }
    }
}
