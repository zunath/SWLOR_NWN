using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class SmitheryForgemasterPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.SmitheryPlasmaEfficiency,
                    MaxLevel = 1,
                    Prices = new[] { 1 },
                    Notes = "Forgemaster, Smithery 5. Plasma Forge stat only; no feat.",
                },
                new()
                {
                    Perk = PerkType.SmitheryHeatRetention,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Forgemaster, Smithery 10 and 30.",
                },
                new()
                {
                    Perk = PerkType.SmitheryOverdrivePress,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Forgemaster, Smithery 15. Adds a Plasma Forge action.",
                },
                new()
                {
                    Perk = PerkType.HotForgingStyle,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Forgemaster style, Smithery 20. Picked in the Plasma Forge window; carried by HotForgingStatusEffect. May be owned with Cold Working.",
                },
                new()
                {
                    Perk = PerkType.SmitheryHammerRhythm,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Forgemaster, Smithery 25.",
                },
                new()
                {
                    Perk = PerkType.SmitheryScorchGuard,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Forgemaster, Smithery 35.",
                },
                new()
                {
                    Perk = PerkType.SmitherySurgePlasma,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Forgemaster, Smithery 40. Adds a Plasma Forge action.",
                },
                new()
                {
                    Perk = PerkType.SmitheryFastRebond,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Forgemaster, Smithery 45.",
                },
                new()
                {
                    Perk = PerkType.SmitheryRedLine,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Forgemaster capstone, Smithery 50. Adds the once-per-craft Red Line Press.",
                },
            };
        }
    }
}
