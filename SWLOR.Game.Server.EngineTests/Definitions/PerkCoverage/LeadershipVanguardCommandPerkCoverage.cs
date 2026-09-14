using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class LeadershipVanguardCommandPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.RallyingStandard,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.RallyingStandard1, FeatType.RallyingStandard2 },
                },
                new()
                {
                    Perk = PerkType.PressTheAttack,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.PressTheAttack1, FeatType.PressTheAttack2, FeatType.PressTheAttack3 },
                },
                new()
                {
                    Perk = PerkType.CoordinatedFocus,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.CoordinatedFocus1, FeatType.CoordinatedFocus2, FeatType.CoordinatedFocus3 },
                },
                new()
                {
                    Perk = PerkType.MarkTarget,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.MarkTargetTrait },
                },
                new()
                {
                    Perk = PerkType.ChargeOrder,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.ChargeOrder1, FeatType.ChargeOrder2 },
                },
                new()
                {
                    Perk = PerkType.BreakMorale,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.BreakMorale1, FeatType.BreakMorale2 },
                },
                new()
                {
                    Perk = PerkType.CommandRadius,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.CommandRadiusTrait },
                },
                new()
                {
                    Perk = PerkType.DecisiveCommand,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.DecisiveCommand1 },
                },
            };
        }
    }
}
