using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// A city's weekly upkeep (planet cities and player stations alike): seven days of its layout's daily price, less 5%
    /// per level of the mayor's Upkeep perk, plus 10,000 a week for each Medical Center, Starport, Cantina and (on a station)
    /// Jump Beacon level above 1. Edicts add their weekly costs beside it, with no Upkeep perk discount.
    /// </summary>
    public static class CityUpkeepRules
    {
        public const int UpgradeBasePrice = 10000;
        public const float UpkeepPerkReductionPerLevel = 0.05f;

        public static int GetWeeklyUpkeep(int pricePerDay, int upkeepPerkLevel, int medicalCenterLevel, int starportLevel, int cantinaLevel, int jumpBeaconLevel = 1)
        {
            var upkeepReductionPercent = upkeepPerkLevel * UpkeepPerkReductionPerLevel;
            var basePrice = pricePerDay * 7;
            basePrice -= (int)(basePrice * upkeepReductionPercent);

            var upgradePrice =
                (medicalCenterLevel - 1) * UpgradeBasePrice +
                (starportLevel - 1) * UpgradeBasePrice +
                (cantinaLevel - 1) * UpgradeBasePrice +
                (Math.Max(1, jumpBeaconLevel) - 1) * UpgradeBasePrice;

            return basePrice + upgradePrice;
        }

        /// <summary>The credits the Edicts add to the week's bill.</summary>
        public static int GetEdictUpkeep(IEnumerable<EdictBillLine> edictLines)
        {
            return edictLines.Sum(x => x.Credits);
        }

        /// <summary>
        /// The lines of the upkeep notice that itemize the week's bill, so the mayor sees what each Edict adds and what
        /// to repeal. Edicts that were not billed this week are listed with the reason.
        /// </summary>
        /// <param name="baseUpkeep">The base and facility upkeep.</param>
        /// <param name="edictLines">The Edicts billed this week.</param>
        /// <param name="unbilledEdicts">The Edicts not billed, each with its reason.</param>
        /// <param name="totalOwed">Everything the city owes after this week's bill.</param>
        public static string BuildUpkeepBreakdownText(
            int baseUpkeep,
            IEnumerable<EdictBillLine> edictLines,
            IEnumerable<string> unbilledEdicts,
            int totalOwed)
        {
            var lines = new List<string> { $"**Base and facilities**: {baseUpkeep:N0} cr" };
            lines.AddRange(edictLines.Select(x => $"**Edict - {x.Name}**: {x.Credits:N0} cr"));

            var unbilled = unbilledEdicts.ToList();
            if (unbilled.Count > 0)
                lines.Add($"**Edicts not billed**: {string.Join("; ", unbilled)}");

            lines.Add($"**Total owed**: {totalOwed:N0} cr");
            return string.Join("\n", lines);
        }
    }
}
