using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class ForceLightGuardianPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.GuardianWard,
                    MaxLevel = 4,
                    Prices = new[] { 3, 4, 5, 5 },
                    GrantedFeats = new[] { FeatType.GuardianWard1, FeatType.GuardianWard2, FeatType.GuardianWard3, FeatType.GuardianWard4 },
                },
                new()
                {
                    Perk = PerkType.LightGuardianDeflectivePresence,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.LightGuardianDeflectivePresenceTrait },
                },
                new()
                {
                    Perk = PerkType.CourageousResolve,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CourageousResolveTrait },
                },
                new()
                {
                    Perk = PerkType.ForceIntercept,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ForceIntercept1 },
                },
                new()
                {
                    Perk = PerkType.ReflectiveBarrier,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ReflectiveBarrierTrait },
                },
                new()
                {
                    Perk = PerkType.PurifyingWave,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.PurifyingWave1 },
                },
                new()
                {
                    Perk = PerkType.LastStandOfTheLight,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.LastStandOfTheLight1 },
                },
            };
        }
    }
}
