using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class BeastBalancedPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.Claw,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.Claw1, FeatType.Claw2, FeatType.Claw3 },
                },
                new()
                {
                    Perk = PerkType.BolsterAttack,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 4 },
                    GrantedFeats = new[] { FeatType.BolsterAttack1, FeatType.BolsterAttack2, FeatType.BolsterAttack3 },
                },
                new()
                {
                    Perk = PerkType.GuardedBite,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 3 },
                    GrantedFeats = new[] { FeatType.GuardedBite1, FeatType.GuardedBite2, FeatType.GuardedBite3 },
                },
                new()
                {
                    Perk = PerkType.PackRhythm,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.PackRhythmTrait },
                },
                new()
                {
                    Perk = PerkType.Hasten,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.Hasten1, FeatType.Hasten2 },
                },
                new()
                {
                    Perk = PerkType.CoordinatedStrike,
                    MaxLevel = 2,
                    Prices = new[] { 4, 4 },
                    GrantedFeats = new[] { FeatType.CoordinatedStrike1, FeatType.CoordinatedStrike2 },
                },
                new()
                {
                    Perk = PerkType.PackRecovery,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.PackRecoveryTrait },
                },
                new()
                {
                    Perk = PerkType.AlphaRhythm,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.AlphaRhythm1 },
                },
            };
        }
    }
}
