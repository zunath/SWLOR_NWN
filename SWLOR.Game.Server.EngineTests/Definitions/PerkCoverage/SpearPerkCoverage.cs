using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class SpearPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.DisablingStrike,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.DisablingStrike1, FeatType.DisablingStrike2, FeatType.DisablingStrike3, FeatType.DisablingStrike4 },
                },
                new()
                {
                    Perk = PerkType.ErosionStrike,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ErosionStrikeTrait },
                },
                new()
                {
                    Perk = PerkType.ForceWarding,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.ForceWardingTrait },
                },
                new()
                {
                    Perk = PerkType.InterruptionStrike,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.InterruptionStrike1, FeatType.InterruptionStrike2, FeatType.InterruptionStrike3 },
                },
                new()
                {
                    Perk = PerkType.ForcePiercing,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ForcePiercingTrait },
                },
                new()
                {
                    Perk = PerkType.DisruptionField,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.DisruptionField1, FeatType.DisruptionField2 },
                },
                new()
                {
                    Perk = PerkType.PerceptiveStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.PerceptiveStance1 },
                },
                new()
                {
                    Perk = PerkType.DisruptionExpert,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DisruptionExpertTrait },
                },
                new()
                {
                    Perk = PerkType.FractureStrike,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.FractureStrikeTrait },
                },
                new()
                {
                    Perk = PerkType.Forcebane,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.Forcebane1 },
                },
                new()
                {
                    Perk = PerkType.VigorThrust,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.VigorThrust1, FeatType.VigorThrust2, FeatType.VigorThrust3, FeatType.VigorThrust4 },
                },
                new()
                {
                    Perk = PerkType.LateralFootwork,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.LateralFootworkTrait },
                },
                new()
                {
                    Perk = PerkType.HighGuard,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.HighGuardTrait },
                },
                new()
                {
                    Perk = PerkType.SweepingFlank,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SweepingFlank1, FeatType.SweepingFlank2, FeatType.SweepingFlank3 },
                },
                new()
                {
                    Perk = PerkType.ImprovedAttentiveness,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ImprovedAttentivenessTrait },
                },
                new()
                {
                    Perk = PerkType.HamperingBarrage,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.HamperingBarrage1, FeatType.HamperingBarrage2 },
                },
                new()
                {
                    Perk = PerkType.VigorStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.VigorStance1 },
                },
                new()
                {
                    Perk = PerkType.RestorationStrike,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.RestorationStrikeTrait },
                },
                new()
                {
                    Perk = PerkType.OpportunistsFlow,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.OpportunistsFlowTrait },
                },
                new()
                {
                    Perk = PerkType.CripplingDefense,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.CripplingDefense1 },
                },
            };
        }
    }
}
