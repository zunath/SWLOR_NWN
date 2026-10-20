using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public sealed class TwinPistolPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.PairedVolley,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.PairedVolley1, FeatType.PairedVolley2, FeatType.PairedVolley3, FeatType.PairedVolley4 },
                },
                new()
                {
                    Perk = PerkType.AlternatingFire,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.AlternatingFireTrait },
                },
                new()
                {
                    Perk = PerkType.MatchedRhythm,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.MatchedRhythmTrait },
                },
                new()
                {
                    Perk = PerkType.FocusedBurst,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.FocusedBurst1, FeatType.FocusedBurst2, FeatType.FocusedBurst3 },
                },
                new()
                {
                    Perk = PerkType.BalancedGrip,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.BalancedGripTrait },
                },
                new()
                {
                    Perk = PerkType.ControlledBurst,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.ControlledBurst1, FeatType.ControlledBurst2 },
                },
                new()
                {
                    Perk = PerkType.AkimboStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.AkimboStance },
                },
                new()
                {
                    Perk = PerkType.ConvergingFire,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ConvergingFireTrait },
                },
                new()
                {
                    Perk = PerkType.CoolUnderFire,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.CoolUnderFireTrait },
                },
                new()
                {
                    Perk = PerkType.TwinSuns,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.TwinSuns },
                },
                new()
                {
                    Perk = PerkType.BlasterSlide,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.BlasterSlide1, FeatType.BlasterSlide2, FeatType.BlasterSlide3, FeatType.BlasterSlide4 },
                },
                new()
                {
                    Perk = PerkType.TwinPistolMomentum,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.TwinPistolMomentumTrait },
                },
                new()
                {
                    Perk = PerkType.PairedHandling,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.PairedHandlingTrait },
                },
                new()
                {
                    Perk = PerkType.GrappleShot,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.GrappleShot1, FeatType.GrappleShot2, FeatType.GrappleShot3 },
                },
                new()
                {
                    Perk = PerkType.ChangingAngles,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ChangingAnglesTrait },
                },
                new()
                {
                    Perk = PerkType.PistolRush,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.PistolRush1, FeatType.PistolRush2 },
                },
                new()
                {
                    Perk = PerkType.CrossfireStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CrossfireStance },
                },
                new()
                {
                    Perk = PerkType.TwinPistolFollowThrough,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.TwinPistolFollowThroughTrait },
                },
                new()
                {
                    Perk = PerkType.TwinPistolSecondWind,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.TwinPistolSecondWindTrait },
                },
                new()
                {
                    Perk = PerkType.FinalSalvo,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.FinalSalvo },
                },
            };
        }
    }
}
