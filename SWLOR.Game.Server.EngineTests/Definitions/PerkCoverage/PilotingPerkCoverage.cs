using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class PilotingPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.DefensiveModules,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.DefensiveModulesTrait },
                },
                new()
                {
                    Perk = PerkType.EnergyManagement,
                    MaxLevel = 2,
                    Prices = new[] { 5, 5 },
                    GrantedFeats = new[] { FeatType.EnergyManagementTrait },
                },
                new()
                {
                    Perk = PerkType.IntuitivePiloting,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.IntuitivePilotingTrait },
                },
                new()
                {
                    Perk = PerkType.MiningModules,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.MiningModulesTrait },
                },
                new()
                {
                    Perk = PerkType.OffensiveModules,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.OffensiveModulesTrait },
                },
                new()
                {
                    Perk = PerkType.StarshipMining,
                    MaxLevel = 2,
                    Prices = new[] { 5, 5 },
                    GrantedFeats = new[] { FeatType.StarshipMiningTrait },
                },
                new()
                {
                    Perk = PerkType.Starships,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.StarshipsTrait },
                },
            };
        }
    }
}
