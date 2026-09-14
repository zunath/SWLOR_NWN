using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class DevicesGrenadierPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.FragGrenade,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.FragGrenade1, FeatType.FragGrenade2, FeatType.FragGrenade3 },
                },
                new()
                {
                    Perk = PerkType.BlastRadius,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 5 },
                    GrantedFeats = new[] { FeatType.BlastRadiusTrait },
                },
                new()
                {
                    Perk = PerkType.ConcussionGrenade,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.ConcussionGrenade1, FeatType.ConcussionGrenade2 },
                },
                new()
                {
                    Perk = PerkType.FlashGrenade,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.FlashGrenade1 },
                },
                new()
                {
                    Perk = PerkType.IonGrenade,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.IonGrenade1, FeatType.IonGrenade2 },
                },
                new()
                {
                    Perk = PerkType.AdhesiveGrenade,
                    MaxLevel = 2,
                    Prices = new[] { 4, 4 },
                    GrantedFeats = new[] { FeatType.AdhesiveGrenade1, FeatType.AdhesiveGrenade2 },
                },
                new()
                {
                    Perk = PerkType.ClusterGrenade,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ClusterGrenade1 },
                },
                new()
                {
                    Perk = PerkType.DisruptionPulse,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DisruptionPulse1 },
                },
                new()
                {
                    Perk = PerkType.ThermalDetonator,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.ThermalDetonator1 },
                },
            };
        }
    }
}
