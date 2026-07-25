using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class BeastEvasionPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.EvasiveManeuver,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.EvasiveManeuver1, FeatType.EvasiveManeuver2, FeatType.EvasiveManeuver3 },
                },
                new()
                {
                    Perk = PerkType.Assault,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 4 },
                    GrantedFeats = new[] { FeatType.Assault1, FeatType.Assault2, FeatType.Assault3 },
                },
                new()
                {
                    Perk = PerkType.DistractingFeint,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.DistractingFeint1, FeatType.DistractingFeint2, FeatType.DistractingFeint3 },
                },
                new()
                {
                    Perk = PerkType.EvasiveChallenge,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.EvasiveChallenge1, FeatType.EvasiveChallenge2 },
                },
                new()
                {
                    Perk = PerkType.Sniff,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 3 },
                    GrantedFeats = new[] { FeatType.SniffTrait },
                },
                new()
                {
                    Perk = PerkType.QuickRecovery,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.QuickRecoveryTrait },
                },
                new()
                {
                    Perk = PerkType.UntouchableInstinct,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.UntouchableInstinct1 },
                },
            };
        }
    }
}
