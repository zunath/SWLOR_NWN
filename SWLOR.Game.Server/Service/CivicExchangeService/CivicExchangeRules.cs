using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CivicExchangeService
{
    /// <summary>
    /// The Civic Exchange rules (Player City Revamp, "Civic Exchange"): who may use it, what the Cantina level stocks, the 3 crates
    /// a week per CD key, and how a crate's contents are rolled. Pure, so each rule can be tested; the facts (the player, the
    /// city, the saved allowance) are looked up by <see cref="SWLOR.Game.Server.Service.CivicExchange"/>.
    /// </summary>
    public static class CivicExchangeRules
    {
        /// <summary>The bartender's response that opens the Exchange, shown to the citizens of the Cantina's city.</summary>
        public const string OpenResponse = "Open the Civic Exchange";

        public const string NotACitizenMessage = "The Civic Exchange is for citizens of this city.";

        public const string TooNewMessage = "You must be a citizen of this city for 7 days before you can use the Civic Exchange.";

        /// <summary>
        /// Whether a character may use the Civic Exchange of a city: a citizen of that city in good standing, with no taxes owed and at
        /// least 7 days of citizenship (the Plan's citizen rule, the one the Edicts use). Allied citizens and visitors are refused.
        /// </summary>
        public static CivicExchangeAccess GetAccess(
            string citizenPropertyId,
            string cityId,
            bool isDeleted,
            int owedTaxes,
            DateTime? citizenSinceUtc,
            DateTime nowUtc)
        {
            if (EdictRules.IsEligibleCitizen(citizenPropertyId, cityId, isDeleted, owedTaxes, citizenSinceUtc, nowUtc))
                return CivicExchangeAccess.Allowed;

            if (!ElectionRules.IsCitizenOf(citizenPropertyId, cityId, isDeleted))
                return CivicExchangeAccess.NotACitizen;

            return owedTaxes > 0 ? CivicExchangeAccess.OwesTaxes : CivicExchangeAccess.TooNew;
        }

        /// <summary>The player-facing reason for a refused character, or an empty string for <see cref="CivicExchangeAccess.Allowed"/>.</summary>
        public static string DescribeAccess(CivicExchangeAccess access, int owedTaxes)
        {
            return access switch
            {
                CivicExchangeAccess.NotACitizen => NotACitizenMessage,
                CivicExchangeAccess.OwesTaxes => $"You owe {owedTaxes} credits in taxes to this city. You cannot use the Civic Exchange until these are paid. Use the Citizenship Terminal in City Hall to pay these.",
                CivicExchangeAccess.TooNew => TooNewMessage,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Why an item cannot be bought now, in the order a citizen should fix them: the Cantina must stock it, a crate must be
        /// left in the week's allowance, and the citizen must have the Available XP.
        /// </summary>
        /// <param name="item">The item, or null for one that is not on the list.</param>
        /// <param name="cantinaLevel">The city's effective Cantina level.</param>
        /// <param name="availableXp">The citizen's Available XP.</param>
        /// <param name="cratesRemaining">How many crates the citizen's CD key may still buy this week.</param>
        public static CivicExchangeRefusal GetRefusal(CivicExchangeItem item, int cantinaLevel, int availableXp, int cratesRemaining)
        {
            if (item == null)
                return CivicExchangeRefusal.UnknownItem;

            if (cantinaLevel < item.MinCantinaLevel)
                return CivicExchangeRefusal.CantinaTooLow;

            if (item.Kind == CivicExchangeItemKind.MaterialsCrate && cratesRemaining <= 0)
                return CivicExchangeRefusal.WeeklyLimit;

            return availableXp < item.PriceXp ? CivicExchangeRefusal.NotEnoughXp : CivicExchangeRefusal.None;
        }

        /// <summary>The player-facing reason for a refusal, or an empty string for <see cref="CivicExchangeRefusal.None"/>.</summary>
        public static string DescribeRefusal(CivicExchangeRefusal refusal, CivicExchangeItem item)
        {
            return refusal switch
            {
                CivicExchangeRefusal.UnknownItem => "The Civic Exchange does not sell that.",
                CivicExchangeRefusal.CantinaTooLow => $"This city's Cantina must reach level {item?.MinCantinaLevel} to stock the {item?.Name}.",
                CivicExchangeRefusal.WeeklyLimit => $"You have bought all {CivicExchangeCatalog.WeeklyCrateLimit} crates this week. The Civic Exchange restocks every Monday.",
                CivicExchangeRefusal.NotEnoughXp => $"You need {item?.PriceXp:N0} Available XP for the {item?.Name}.",
                _ => string.Empty
            };
        }

        // The weekly crate limit

        /// <summary>
        /// The id of a CD key's allowance record. A character with no CD key falls back to its own id, so an unusual login still has a
        /// limit rather than none.
        /// </summary>
        public static string GetAllowanceKey(string cdKey, string playerId)
        {
            return string.IsNullOrWhiteSpace(cdKey) ? $"player:{playerId}" : cdKey.Trim();
        }

        /// <summary>How many crates a CD key may still buy this week. A missing record, or one from an earlier week, is the full limit.</summary>
        public static int GetCratesRemaining(IStockAllowanceRecord record, DateTime nowUtc)
        {
            return StockAllowanceRules.GetRemaining(CivicExchangeCatalog.WeeklyCrateLimit, record, CityWeek.GetWeekStart(nowUtc));
        }

        /// <summary>Counts a bought crate against the record, starting a new count when the record is from an earlier week.</summary>
        public static void RecordCrate(IStockAllowanceRecord record, DateTime nowUtc)
        {
            StockAllowanceRules.RecordSale(record, 1, CityWeek.GetWeekStart(nowUtc));
        }

        // Crate contents

        /// <summary>The minutes of active harvesting a crate stands for, at <see cref="CivicExchangeCatalog.HarvestSecondsPerUnit"/> a unit.</summary>
        public static double GetCrateHarvestMinutes()
        {
            return CivicExchangeCatalog.CrateUnits * (double)CivicExchangeCatalog.HarvestSecondsPerUnit / 60.0;
        }

        /// <summary>
        /// Rolls a crate of a tier: <see cref="CivicExchangeCatalog.CrateUnits"/> units, each picking one of the tier's harvest tables
        /// at random and then rolling that table. Returns how many of each item the crate holds, in the order they were first rolled.
        /// </summary>
        /// <param name="tier">The crate's tier.</param>
        /// <param name="nextIndex">Picks a number from 0 up to, not including, the given count.</param>
        /// <param name="rollTable">Rolls one item resref from a harvest table by name.</param>
        public static IReadOnlyDictionary<string, int> PlanCrate(int tier, Func<int, int> nextIndex, Func<string, string> rollTable)
        {
            var contents = new Dictionary<string, int>();
            var tables = CivicExchangeCatalog.GetCrateTables(tier);
            if (tables.Count == 0)
                return contents;

            for (var unit = 0; unit < CivicExchangeCatalog.CrateUnits; unit++)
            {
                var resref = rollTable(tables[nextIndex(tables.Count)]);
                contents[resref] = contents.GetValueOrDefault(resref) + 1;
            }

            return contents;
        }
    }
}
