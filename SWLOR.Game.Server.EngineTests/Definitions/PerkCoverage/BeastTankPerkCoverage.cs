using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class BeastTankPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.IronHide,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.IronHide1, FeatType.IronHide2, FeatType.IronHide3 },
                },
                new()
                {
                    Perk = PerkType.Anger,
                    MaxLevel = 2,
                    Prices = new[] { 2, 4 },
                    GrantedFeats = new[] { FeatType.Anger1, FeatType.Anger2 },
                },
                new()
                {
                    Perk = PerkType.FocusAttention,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.FocusAttentionTrait },
                },
                new()
                {
                    Perk = PerkType.GuardingRoar,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.GuardingRoar1, FeatType.GuardingRoar2, FeatType.GuardingRoar3 },
                },
                new()
                {
                    Perk = PerkType.Intercept,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.Intercept1, FeatType.Intercept2 },
                },
                new()
                {
                    Perk = PerkType.BodyguardsResolve,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.BodyguardsResolveTrait },
                },
                new()
                {
                    Perk = PerkType.RampartHide,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.RampartHide1 },
                },
                new()
                {
                    Perk = PerkType.LastGuardian,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.LastGuardianTrait },
                },
                new()
                {
                    Perk = PerkType.UnbreakableBeast,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.UnbreakableBeast1 },
                },
            };
        }
    }
}
