using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class PilotingOrdnancePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.OrdnanceVolley,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.OrdnanceVolley1, FeatType.OrdnanceVolley2, FeatType.OrdnanceVolley3, FeatType.OrdnanceVolley4 },
                },
                new()
                {
                    Perk = PerkType.OrdnanceProximityWarheads,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.OrdnanceProximityWarheadsTrait },
                },
                new()
                {
                    Perk = PerkType.OrdnanceShrapnelWarheads,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.OrdnanceShrapnelWarheadsTrait },
                },
                new()
                {
                    Perk = PerkType.OrdnanceFlareMissile,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.OrdnanceFlareMissile1, FeatType.OrdnanceFlareMissile2, FeatType.OrdnanceFlareMissile3 },
                },
                new()
                {
                    Perk = PerkType.OrdnanceClusterWarheads,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.OrdnanceClusterWarheadsTrait },
                },
                new()
                {
                    Perk = PerkType.OrdnanceConcussionBarrage,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.OrdnanceConcussionBarrage1, FeatType.OrdnanceConcussionBarrage2 },
                },
                new()
                {
                    Perk = PerkType.OrdnanceShipStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.OrdnanceShipStance },
                },
                new()
                {
                    Perk = PerkType.OrdnanceSaturationFire,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.OrdnanceSaturationFireTrait },
                },
                new()
                {
                    Perk = PerkType.OrdnanceBombardiersRhythm,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.OrdnanceBombardiersRhythmTrait },
                },
                new()
                {
                    Perk = PerkType.OrdnanceSaturationStrike,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.OrdnanceSaturationStrike },
                },
            };
        }
    }
}
