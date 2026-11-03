using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class AgricultureChefPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.PantryStaples,
                    MaxLevel = 1,
                    Prices = new[] { 1 },
                    Notes = "Agriculture 5.",
                },
                new()
                {
                    Perk = PerkType.SharpPalate,
                    MaxLevel = 1,
                    Prices = new[] { 1 },
                    Notes = "Agriculture 10.",
                },
                new()
                {
                    Perk = PerkType.SeasonedPalate,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    Notes = "Agriculture 15 and 30. A switch in the Kitchen's setup view.",
                },
                new()
                {
                    Perk = PerkType.SlowCooking,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Agriculture 20. Style; cannot be owned with Flash Cooking.",
                },
                new()
                {
                    Perk = PerkType.FlashCooking,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Agriculture 20. Style; cannot be owned with Slow Cooking.",
                },
                new()
                {
                    Perk = PerkType.KitchenReduce,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    Notes = "Agriculture 25 and 40.",
                },
                new()
                {
                    Perk = PerkType.AromaticKitchen,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Agriculture 35.",
                },
                new()
                {
                    Perk = PerkType.SteadyWrist,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Agriculture 45.",
                },
                new()
                {
                    Perk = PerkType.SignatureDish,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Agriculture 50. Capstone.",
                },
            };
        }
    }
}
