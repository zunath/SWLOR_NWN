using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// Which blueprint and limited items a city store has at a level, which store a stall theme opens, and who may shop.
    /// Pure, so each rule can be tested.
    /// </summary>
    public static class CityStoreRules
    {
        /// <summary>The store a stall sells from, by the theme its mayor picked. A stall with no theme sells general goods.</summary>
        public static CityStoreKind GetKindForTheme(MarketStallTheme theme)
        {
            return theme switch
            {
                MarketStallTheme.CraftingSupplies => CityStoreKind.StallCraftingSupplies,
                MarketStallTheme.Decor => CityStoreKind.StallDecor,
                _ => CityStoreKind.StallGeneralGoods
            };
        }

        /// <summary>Whether the store belongs to a market stall rather than a building.</summary>
        public static bool IsStallKind(CityStoreKind kind)
        {
            return kind is CityStoreKind.StallGeneralGoods or CityStoreKind.StallCraftingSupplies or CityStoreKind.StallDecor;
        }

        /// <summary>
        /// The blueprint a store opens with at this level: the highest one the level has reached. A level below the
        /// first (a building the city does not have) gets the first.
        /// </summary>
        public static string GetBlueprint(CityStoreDetail detail, int level)
        {
            if (detail == null || detail.Blueprints.Count == 0)
                return string.Empty;

            var chosen = detail.Blueprints[0].Blueprint;
            foreach (var (minLevel, blueprint) in detail.Blueprints)
            {
                if (minLevel <= level)
                    chosen = blueprint;
            }

            return chosen;
        }

        /// <summary>The limited item with this resref that the store stocks at this level, or null.</summary>
        public static CityStoreLimitedItem FindLimitedItem(CityStoreDetail detail, int level, string resref)
        {
            return detail?.LimitedItems.FirstOrDefault(item =>
                item.MinLevel <= level &&
                string.Equals(item.Resref, resref, StringComparison.Ordinal));
        }

        /// <summary>
        /// Whether a shopper may use a city's stores. Anyone may, citizen or not, except a citizen of a city the host
        /// city has an Embargo against.
        /// </summary>
        /// <param name="embargoedCityId">The city the host city's Embargo names, or empty when there is none in effect.</param>
        /// <param name="shopperCitizenCityId">The city the shopper is a citizen of, or empty.</param>
        public static bool CanShop(string embargoedCityId, string shopperCitizenCityId)
        {
            if (string.IsNullOrWhiteSpace(embargoedCityId) || string.IsNullOrWhiteSpace(shopperCitizenCityId))
                return true;

            return !string.Equals(embargoedCityId, shopperCitizenCityId, StringComparison.Ordinal);
        }

        /// <summary>The limited items among those the store stocks, as the stock the engine would hold when the store is built.</summary>
        public static IReadOnlyList<string> GetLimitedResrefs(CityStoreDetail detail)
        {
            return detail == null
                ? Array.Empty<string>()
                : detail.LimitedItems.Select(item => item.Resref).Distinct(StringComparer.Ordinal).ToList();
        }
    }
}
