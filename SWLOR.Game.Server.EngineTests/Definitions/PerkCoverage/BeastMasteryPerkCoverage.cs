using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class BeastMasteryPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.DNAManipulation,
                    MaxLevel = 5,
                    Prices = new[] { 2, 2, 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.DNAManipulationTrait },
                },
                new()
                {
                    Perk = PerkType.IncubationProcessing,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.IncubationProcessingTrait },
                },
                new()
                {
                    Perk = PerkType.ErraticGenius,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.ErraticGeniusTrait },
                },
                new()
                {
                    Perk = PerkType.IncubationManagement,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.IncubationManagementTrait },
                },
                new()
                {
                    Perk = PerkType.Tame,
                    MaxLevel = 5,
                    Prices = new[] { 3, 3, 4, 5, 5 },
                    GrantedFeats = new[] { FeatType.Tame, FeatType.CallBeast },
                },
                new()
                {
                    Perk = PerkType.Reward,
                    MaxLevel = 3,
                    Prices = new[] { 1, 2, 2 },
                    GrantedFeats = new[] { FeatType.Reward1, FeatType.Reward2, FeatType.Reward3 },
                },
                new()
                {
                    Perk = PerkType.Stabling,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 1, 1, 1 },
                    GrantedFeats = new[] { FeatType.StablingTrait },
                },
                new()
                {
                    Perk = PerkType.GuardingBond,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.GuardingBond },
                },
                new()
                {
                    Perk = PerkType.PredatoryBond,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.PredatoryBond },
                },
                new()
                {
                    Perk = PerkType.SoothePet,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.SoothePet },
                },
                new()
                {
                    Perk = PerkType.ReviveBeast,
                    MaxLevel = 3,
                    Prices = new[] { 1, 2, 3 },
                    GrantedFeats = new[] { FeatType.ReviveBeast1, FeatType.ReviveBeast2, FeatType.ReviveBeast3 },
                },
            };
        }
    }
}
