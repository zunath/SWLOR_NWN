namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// A city's weekly upkeep (planet cities and player stations alike): seven days of its layout's daily price, less 5%
    /// per level of the mayor's Upkeep perk, plus 10,000 a week for each Medical Center, Starport and Cantina level above 1.
    /// </summary>
    public static class CityUpkeepRules
    {
        public const int UpgradeBasePrice = 10000;
        public const float UpkeepPerkReductionPerLevel = 0.05f;

        public static int GetWeeklyUpkeep(int pricePerDay, int upkeepPerkLevel, int medicalCenterLevel, int starportLevel, int cantinaLevel)
        {
            var upkeepReductionPercent = upkeepPerkLevel * UpkeepPerkReductionPerLevel;
            var basePrice = pricePerDay * 7;
            basePrice -= (int)(basePrice * upkeepReductionPercent);

            var upgradePrice =
                (medicalCenterLevel - 1) * UpgradeBasePrice +
                (starportLevel - 1) * UpgradeBasePrice +
                (cantinaLevel - 1) * UpgradeBasePrice;

            return basePrice + upgradePrice;
        }
    }
}
