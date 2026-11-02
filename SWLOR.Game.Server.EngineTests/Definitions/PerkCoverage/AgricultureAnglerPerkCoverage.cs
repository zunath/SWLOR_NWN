using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class AgricultureAnglerPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.StrongLine,
                    MaxLevel = 3,
                    Prices = new[] { 1, 3, 3 },
                    Notes = "Agriculture 5, 25 and 40.",
                },
                new()
                {
                    Perk = PerkType.HardSet,
                    MaxLevel = 3,
                    Prices = new[] { 1, 3, 3 },
                    Notes = "Agriculture 10, 30 and 45.",
                },
                new()
                {
                    Perk = PerkType.FishSense,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    Notes = "Agriculture 15 and 35.",
                },
                new()
                {
                    Perk = PerkType.PatientAngler,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Agriculture 20. Style; cannot be owned with Aggressive Angler.",
                },
                new()
                {
                    Perk = PerkType.AggressiveAngler,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Agriculture 20. Style; cannot be owned with Patient Angler.",
                },
                new()
                {
                    Perk = PerkType.TrophyCatch,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Agriculture 50. Capstone.",
                },
            };
        }
    }
}
