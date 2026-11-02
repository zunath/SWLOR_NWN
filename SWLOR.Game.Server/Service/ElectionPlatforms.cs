using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Reads the records the election window's platform step is written from: the city's upkeep and treasury, the
    /// citizen count, and what the candidate's City Management rank would do to the city level. The rules and the words
    /// are in <see cref="ElectionPlatformRules"/> and <see cref="CityLevelRules"/>.
    /// </summary>
    public static class ElectionPlatforms
    {
        /// <summary>What the platform step knows about the city and the candidate.</summary>
        /// <param name="city">The city the election is for.</param>
        /// <param name="candidate">The character who would run.</param>
        public static ElectionPlatformContext GetContext(WorldProperty city, Entity.Player candidate)
        {
            var level = EdictChanges.GetCityLevel(city);
            var citizenCount = (int)DB.SearchCount(new DBQuery<Entity.Player>()
                .AddFieldSearch(nameof(Entity.Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Entity.Player.IsDeleted), false));

            // The upkeep the city pays now follows the sitting mayor's Upkeep perk, as the weekly check does.
            var mayor = DB.Get<Entity.Player>(city.OwnerPlayerId);
            var upkeepPerkRank = mayor != null && mayor.Perks.TryGetValue(PerkType.Upkeep, out var rank) ? rank : 0;
            var layout = Property.GetLayoutByType(city.Layout);
            var baseUpkeep = CityUpkeepRules.GetWeeklyUpkeep(
                layout.PricePerDay,
                upkeepPerkRank,
                city.Upgrades.GetValueOrDefault(PropertyUpgradeType.MedicalCenterLevel, 1),
                city.Upgrades.GetValueOrDefault(PropertyUpgradeType.StarportLevel, 1),
                city.Upgrades.GetValueOrDefault(PropertyUpgradeType.CantinaLevel, 1),
                city.Upgrades.GetValueOrDefault(PropertyUpgradeType.JumpBeaconLevel, 1));

            var citizensRequired = Enumerable.Range(1, CityLevelRules.MaxLevel)
                .ToDictionary(x => x, Property.GetCitizensRequiredForNextCityLevel);
            var candidateMayorLevel = CityLevelRules.GetMayorLevel(
                candidate != null && candidate.Perks.TryGetValue(PerkType.CityManagement, out var managementRank) ? managementRank : null);
            var candidateSupported = CityLevelRules.GetLevelAfterDrop(
                level,
                candidateMayorLevel,
                CityLevelRules.GetMaxSupportedLevel(candidateMayorLevel, citizenCount, citizensRequired));
            var baselineSupported = CityLevelRules.GetLevelAfterDrop(
                level,
                CityLevelRules.MaxLevel,
                CityLevelRules.GetMaxSupportedLevel(CityLevelRules.MaxLevel, citizenCount, citizensRequired));

            return new ElectionPlatformContext(
                level,
                Edict.IsWildSpaceStation(city),
                citizenCount,
                baseUpkeep,
                CityUpkeepRules.GetEdictUpkeep(Edict.GetWeeklyBill(city)),
                city.Treasury,
                city.RestrictedTreasury,
                (int)city.Taxes.GetValueOrDefault(PropertyTaxType.Citizenship),
                candidateMayorLevel,
                candidateSupported,
                baselineSupported);
        }

        /// <summary>The cities an Alliance or Embargo can name from the city, by name.</summary>
        public static List<WorldProperty> GetNameableCities(string cityId)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var count = (int)DB.SearchCount(query);
            return count <= 0
                ? new List<WorldProperty>()
                : DB.Search(query.AddPaging(count, 0))
                    .Where(x => x.Id != cityId)
                    .OrderBy(x => x.CustomName)
                    .ToList();
        }
    }
}
