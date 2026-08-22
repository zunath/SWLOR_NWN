using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class StaffPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.Slam,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.Slam1, FeatType.Slam2, FeatType.Slam3, FeatType.Slam4 },
                },
                new()
                {
                    Perk = PerkType.CrushingMastery,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.CrushingMasteryTrait },
                },
                new()
                {
                    Perk = PerkType.ChargedBlows,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.ChargedBlowsTrait },
                },
                new()
                {
                    Perk = PerkType.RibBreaker,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.RibBreaker1, FeatType.RibBreaker2, FeatType.RibBreaker3 },
                },
                new()
                {
                    Perk = PerkType.HeavyHands,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.HeavyHandsTrait },
                },
                new()
                {
                    Perk = PerkType.GroundQuake,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.GroundQuake1, FeatType.GroundQuake2 },
                },
                new()
                {
                    Perk = PerkType.CrusherStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CrusherStance1 },
                },
                new()
                {
                    Perk = PerkType.BreakPosture,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.BreakPostureTrait },
                },
                new()
                {
                    Perk = PerkType.SkullRattle,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.SkullRattleTrait },
                },
                new()
                {
                    Perk = PerkType.Worldbreaker,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.Worldbreaker1 },
                },
                new()
                {
                    Perk = PerkType.LineBreaker,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.LineBreaker1, FeatType.LineBreaker2, FeatType.LineBreaker3, FeatType.LineBreaker4 },
                },
                new()
                {
                    Perk = PerkType.SentinelGuard,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.SentinelGuardTrait },
                },
                new()
                {
                    Perk = PerkType.StaffParry,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.StaffParryTrait },
                },
                new()
                {
                    Perk = PerkType.LegSweep,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.LegSweep1, FeatType.LegSweep2, FeatType.LegSweep3 },
                },
                new()
                {
                    Perk = PerkType.GuardingStep,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.GuardingStepTrait },
                },
                new()
                {
                    Perk = PerkType.SweepingGuard,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.SweepingGuard1 },
                },
                new()
                {
                    Perk = PerkType.SentinelStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SentinelStance1 },
                },
                new()
                {
                    Perk = PerkType.FlowingDefense,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.FlowingDefenseTrait },
                },
                new()
                {
                    Perk = PerkType.ShelterCircle,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.ShelterCircle1 },
                },
                new()
                {
                    Perk = PerkType.PatientSentinel,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.PatientSentinelTrait },
                },
                new()
                {
                    Perk = PerkType.UnmovingCenter,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.UnmovingCenter1 },
                },
            };
        }
    }
}
