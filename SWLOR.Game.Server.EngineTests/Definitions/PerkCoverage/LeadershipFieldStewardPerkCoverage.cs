using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class LeadershipFieldStewardPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.WatchfulPresence,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.WatchfulPresence1, FeatType.WatchfulPresence2, FeatType.WatchfulPresence3 },
                },
                new()
                {
                    Perk = PerkType.RousingShout,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.RousingShout1, FeatType.RousingShout2, FeatType.RousingShout3 },
                },
                new()
                {
                    Perk = PerkType.SteadyFormation,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.SteadyFormation1, FeatType.SteadyFormation2 },
                },
                new()
                {
                    Perk = PerkType.BolsterResolve,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.BolsterResolveTrait },
                },
                new()
                {
                    Perk = PerkType.FieldRecovery,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.FieldRecovery1, FeatType.FieldRecovery2 },
                },
                new()
                {
                    Perk = PerkType.CleanseOrder,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.CleanseOrder1, FeatType.CleanseOrder2 },
                },
                new()
                {
                    Perk = PerkType.TriageProtocol,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.TriageProtocolTrait },
                },
                new()
                {
                    Perk = PerkType.HoldTheLine,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.HoldTheLine1 },
                },
            };
        }
    }
}
