using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class FabricationBuilderPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.QuickChange,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Fabrication 5 and 35.",
                },
                new()
                {
                    Perk = PerkType.HardenedHeads,
                    MaxLevel = 1,
                    Prices = new[] { 1 },
                    Notes = "Fabrication 10.",
                },
                new()
                {
                    Perk = PerkType.FieldService,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    Notes = "Fabrication 15 and 40.",
                },
                new()
                {
                    Perk = PerkType.ProductionRun,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Fabrication 20. Style; cannot be owned with Fine Tolerance.",
                },
                new()
                {
                    Perk = PerkType.FineTolerance,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Fabrication 20. Style; cannot be owned with Production Run.",
                },
                new()
                {
                    Perk = PerkType.DualFeed,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Fabrication 25.",
                },
                new()
                {
                    Perk = PerkType.FeedstockReserve,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Fabrication 30.",
                },
                new()
                {
                    Perk = PerkType.FeedstockContract,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Fabrication 45.",
                },
                new()
                {
                    Perk = PerkType.MasterAssembler,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Fabrication 50. Capstone.",
                },
            };
        }
    }
}
