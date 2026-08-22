using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class BeastForcePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.ForceTouch,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.ForceTouch1, FeatType.ForceTouch2, FeatType.ForceTouch3 },
                },
                new()
                {
                    Perk = PerkType.Innervate,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 4 },
                    GrantedFeats = new[] { FeatType.Innervate1, FeatType.Innervate2, FeatType.Innervate3 },
                },
                new()
                {
                    Perk = PerkType.WardingHowl,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.WardingHowl1, FeatType.WardingHowl2, FeatType.WardingHowl3 },
                },
                new()
                {
                    Perk = PerkType.ForceLink,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.ForceLinkTrait },
                },
                new()
                {
                    Perk = PerkType.PsychicCry,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 3 },
                    GrantedFeats = new[] { FeatType.PsychicCry1, FeatType.PsychicCry2, FeatType.PsychicCry3 },
                },
                new()
                {
                    Perk = PerkType.MindfulHide,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.MindfulHideTrait },
                },
                new()
                {
                    Perk = PerkType.ForceBondedBeast,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.ForceBondedBeast1 },
                },
            };
        }
    }
}
