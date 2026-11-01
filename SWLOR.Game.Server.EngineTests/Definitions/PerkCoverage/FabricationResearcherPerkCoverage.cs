using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class FabricationResearcherPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.FieldNotes,
                    MaxLevel = 2,
                    Prices = new[] { 1, 2 },
                    Notes = "Fabrication 5 and 15.",
                },
                new()
                {
                    Perk = PerkType.SteadyHands,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Fabrication 35.",
                },
                new()
                {
                    Perk = PerkType.BroaderHypotheses,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Fabrication 25.",
                },
                new()
                {
                    Perk = PerkType.Methodical,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Fabrication 20. Style; cannot be owned with Intuitive.",
                },
                new()
                {
                    Perk = PerkType.Intuitive,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Fabrication 20. Style; cannot be owned with Methodical.",
                },
                new()
                {
                    Perk = PerkType.PeerReview,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Fabrication 40.",
                },
                new()
                {
                    Perk = PerkType.EfficientAnalysis,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Fabrication 10 and 30.",
                },
                new()
                {
                    Perk = PerkType.SecondOpinion,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Fabrication 45.",
                },
                new()
                {
                    Perk = PerkType.Breakthrough,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Fabrication 50. Capstone.",
                },
            };
        }
    }
}
