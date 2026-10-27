using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class GatheringPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.TreasureHunter,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.TreasureHunterTrait },
                },
                new()
                {
                    Perk = PerkType.CreditFinder,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.CreditFinderTrait },
                },
                new()
                {
                    Perk = PerkType.Harvesting,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.Harvesting1, FeatType.Harvesting2, FeatType.Harvesting3, FeatType.Harvesting4, FeatType.Harvesting5 },
                },
                new()
                {
                    Perk = PerkType.Refining,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.Refining1, FeatType.Refining2, FeatType.Refining3, FeatType.Refining4, FeatType.Refining5 },
                },
                new()
                {
                    Perk = PerkType.RefineryManagement,
                    MaxLevel = 6,
                    Prices = new[] { 1, 1, 2, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.RefineryManagement1, FeatType.RefineryManagement2, FeatType.RefineryManagement3, FeatType.RefineryManagement4, FeatType.RefineryManagement5, FeatType.RefineryManagement6 },
                },
                new()
                {
                    Perk = PerkType.Scavenging,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.Scavenging1, FeatType.Scavenging2, FeatType.Scavenging3, FeatType.Scavenging4, FeatType.Scavenging5 },
                },
                new()
                {
                    Perk = PerkType.HardLook,
                    MaxLevel = 5,
                    Prices = new[] { 1, 1, 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.HardLook1, FeatType.HardLook2, FeatType.HardLook3, FeatType.HardLook4, FeatType.HardLook5 },
                },
                new()
                {
                    Perk = PerkType.Prospector,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.ProspectorTrait },
                },
                new()
                {
                    Perk = PerkType.DeepCore,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.DeepCoreTrait },
                },
                new()
                {
                    Perk = PerkType.RapidExtraction,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.RapidExtractionTrait },
                },
                new()
                {
                    Perk = PerkType.GasSiphoning,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.GasSiphoningTrait },
                },
                new()
                {
                    Perk = PerkType.SalvageExpert,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.SalvageExpertTrait },
                },
                new()
                {
                    Perk = PerkType.TraitHunter,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.TraitHunterTrait },
                },
            };
        }
    }
}
