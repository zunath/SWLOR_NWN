using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class SmitheryArtisanPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.SmitherySteadyHands,
                    MaxLevel = 1,
                    Prices = new[] { 1 },
                    Notes = "Artisan, Smithery 5. Plasma Forge stat only; no feat.",
                },
                new()
                {
                    Perk = PerkType.SmitheryCleanContainment,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Artisan, Smithery 10 and 25.",
                },
                new()
                {
                    Perk = PerkType.SmitheryMicroHone,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Artisan, Smithery 15. Adds a Plasma Forge action.",
                },
                new()
                {
                    Perk = PerkType.ColdWorkingStyle,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Artisan style, Smithery 20. Picked in the Plasma Forge window; carried by ColdWorkingStatusEffect. May be owned with Hot Forging.",
                },
                new()
                {
                    Perk = PerkType.SmitherySteadyQuench,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Artisan, Smithery 30.",
                },
                new()
                {
                    Perk = PerkType.SmitheryCoolHand,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Artisan, Smithery 35.",
                },
                new()
                {
                    Perk = PerkType.SmitheryTemperHold,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Artisan, Smithery 40. Adds a Plasma Forge action.",
                },
                new()
                {
                    Perk = PerkType.SmitheryContainmentFocus,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Artisan, Smithery 45.",
                },
                new()
                {
                    Perk = PerkType.SmitheryPerfectTemper,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Artisan capstone, Smithery 50. Automatic trait.",
                },
            };
        }
    }
}
