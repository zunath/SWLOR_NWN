using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class AgricultureFarmerPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.CropManagement,
                    MaxLevel = 5,
                    Prices = new[] { 1, 2, 3, 3, 3 },
                    Notes = "Agriculture 5, 15, 25, 35 and 45. Gates the crop and livestock tiers.",
                },
                new()
                {
                    Perk = PerkType.BountifulHarvest,
                    MaxLevel = 3,
                    Prices = new[] { 1, 3, 4 },
                    Notes = "Agriculture 10, 30 and 50. Adds items to crop harvests and butchering.",
                },
                new()
                {
                    Perk = PerkType.SeedSaver,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    Notes = "Agriculture 20 and 40. A chance a harvest returns its seed.",
                },
            };
        }
    }
}
