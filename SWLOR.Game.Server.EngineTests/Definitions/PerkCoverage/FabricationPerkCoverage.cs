using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class FabricationPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.Research,
                    MaxLevel = 5,
                    Prices = new[] { 2, 2, 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.ResearchTrait },
                },
                new()
                {
                    Perk = PerkType.ScientificNetworking,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.ScientificNetworkingTrait },
                },
                new()
                {
                    Perk = PerkType.ResearchProjects,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.ResearchProjectsTrait },
                },
            };
        }
    }
}
