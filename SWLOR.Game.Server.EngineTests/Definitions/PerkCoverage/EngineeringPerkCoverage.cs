using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class EngineeringPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.DroidAssembly,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.DroidAssemblyTrait },
                },
            };
        }
    }
}
