using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class LeadershipPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.CityManagement,
                    MaxLevel = 4,
                    Prices = new[] { 2, 3, 4, 5 },
                    GrantedFeats = new[] { FeatType.CityManagementTrait },
                },
                new()
                {
                    Perk = PerkType.Upkeep,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.UpkeepTrait },
                },
                new()
                {
                    Perk = PerkType.GuildRelations,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.GuildRelationsTrait },
                },
            };
        }
    }
}
