using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    /// <summary>
    /// Structure of the Performance perk lines (Dancing and Instruments). The performer
    /// StatType values each level grants and the routine/set unlocks are verified live by
    /// PerformancePerkEngineTests, which cannot be expressed in a structural case.
    /// </summary>
    public class PerformancePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                // Dancing
                new()
                {
                    Perk = PerkType.Dance,
                    MaxLevel = 4,
                    Prices = new[] { 2, 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.Dance1, FeatType.Dance2, FeatType.Dance3, FeatType.Dance4 },
                },
                new()
                {
                    Perk = PerkType.LightFeet,
                    MaxLevel = 2,
                    Prices = new[] { 2, 4 },
                    GrantedFeats = new[] { FeatType.LightFeetTrait, FeatType.LightFeet2Trait },
                },
                new()
                {
                    Perk = PerkType.CrowdPleaser,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.CrowdPleaserTrait, FeatType.CrowdPleaser2Trait },
                },
                new()
                {
                    Perk = PerkType.PowerStep,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.PowerStepTrait },
                },
                new()
                {
                    Perk = PerkType.Encore,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.EncoreTrait, FeatType.Encore2Trait, FeatType.Encore3Trait },
                },
                new()
                {
                    Perk = PerkType.Quickstep,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.QuickstepTrait },
                },
                new()
                {
                    Perk = PerkType.VigorReel,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.VigorReelTrait },
                },
                new()
                {
                    Perk = PerkType.PartnerDance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.PartnerDanceTrait },
                },
                new()
                {
                    Perk = PerkType.StandingOvation,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.StandingOvationTrait },
                },
                new()
                {
                    Perk = PerkType.PeakForm,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.PeakFormTrait },
                },
                new()
                {
                    Perk = PerkType.Showstopper,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.ShowstopperTrait },
                },

                // Instruments
                new()
                {
                    Perk = PerkType.PlayInstrument,
                    MaxLevel = 4,
                    Prices = new[] { 2, 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.PlayInstrument1, FeatType.PlayInstrument2, FeatType.PlayInstrument3, FeatType.PlayInstrument4 },
                },
                new()
                {
                    Perk = PerkType.SteadyHands,
                    MaxLevel = 2,
                    Prices = new[] { 2, 4 },
                    GrantedFeats = new[] { FeatType.SteadyHandsTrait, FeatType.SteadyHands2Trait },
                },
                new()
                {
                    Perk = PerkType.Harmony,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.HarmonyTrait, FeatType.Harmony2Trait },
                },
                new()
                {
                    Perk = PerkType.KeenEar,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.KeenEarTrait },
                },
                new()
                {
                    Perk = PerkType.Resonance,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.ResonanceTrait, FeatType.Resonance2Trait },
                },
                new()
                {
                    Perk = PerkType.AnglersLullaby,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.AnglersLullabyTrait },
                },
                new()
                {
                    Perk = PerkType.StillWaters,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.StillWatersTrait },
                },
                new()
                {
                    Perk = PerkType.Duet,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DuetTrait },
                },
                new()
                {
                    Perk = PerkType.Requiem,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.RequiemTrait },
                },
                new()
                {
                    Perk = PerkType.SilverTongue,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SilverTongueTrait },
                },
                new()
                {
                    Perk = PerkType.PerfectPitch,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.PerfectPitchTrait },
                },
                new()
                {
                    Perk = PerkType.Virtuoso,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.VirtuosoTrait },
                },
            };
        }
    }
}
