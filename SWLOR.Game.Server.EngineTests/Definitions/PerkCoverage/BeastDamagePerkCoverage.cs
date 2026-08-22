using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class BeastDamagePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.Bite,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.Bite1, FeatType.Bite2, FeatType.Bite3 },
                },
                new()
                {
                    Perk = PerkType.HuntersFocus,
                    MaxLevel = 2,
                    Prices = new[] { 2, 2 },
                    GrantedFeats = new[] { FeatType.HuntersFocusTrait },
                },
                new()
                {
                    Perk = PerkType.RendingClaw,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.RendingClaw1, FeatType.RendingClaw2, FeatType.RendingClaw3 },
                },
                new()
                {
                    Perk = PerkType.Pounce,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.Pounce1, FeatType.Pounce2 },
                },
                new()
                {
                    Perk = PerkType.PredatorsMark,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.PredatorsMarkTrait },
                },
                new()
                {
                    Perk = PerkType.ExposePrey,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ExposePrey1 },
                },
                new()
                {
                    Perk = PerkType.BeastBloodFrenzy,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.BeastBloodFrenzyTrait },
                },
                new()
                {
                    Perk = PerkType.ExecutePrey,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.ExecutePrey1 },
                },
                new()
                {
                    Perk = PerkType.ApexBite,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.ApexBite1 },
                },
            };
        }
    }
}
