using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class TwinBladePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.CrossCut,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.CrossCut1, FeatType.CrossCut2, FeatType.CrossCut3, FeatType.CrossCut4 },
                },
                new()
                {
                    Perk = PerkType.Momentum,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.MomentumTrait },
                },
                new()
                {
                    Perk = PerkType.SpinningRhythm,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SpinningRhythmTrait },
                },
                new()
                {
                    Perk = PerkType.SpinningWhirl,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SpinningWhirl1, FeatType.SpinningWhirl2, FeatType.SpinningWhirl3 },
                },
                new()
                {
                    Perk = PerkType.FlowingFootwork,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.FlowingFootworkTrait },
                },
                new()
                {
                    Perk = PerkType.BladeVortex,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.BladeVortex1, FeatType.BladeVortex2 },
                },
                new()
                {
                    Perk = PerkType.CycloneStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CycloneStance1 },
                },
                new()
                {
                    Perk = PerkType.SweepingAdvance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SweepingAdvanceTrait },
                },
                new()
                {
                    Perk = PerkType.EdgeRhythm,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.EdgeRhythmTrait },
                },
                new()
                {
                    Perk = PerkType.TempestBloom,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.TempestBloom1 },
                },
                new()
                {
                    Perk = PerkType.LaceratingTwinCut,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.LaceratingTwinCut1, FeatType.LaceratingTwinCut2, FeatType.LaceratingTwinCut3, FeatType.LaceratingTwinCut4 },
                },
                new()
                {
                    Perk = PerkType.BloodWake,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.BloodWakeTrait },
                },
                new()
                {
                    Perk = PerkType.BleedSpread,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.BleedSpreadTrait },
                },
                new()
                {
                    Perk = PerkType.SerratedArc,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SerratedArc1, FeatType.SerratedArc2, FeatType.SerratedArc3 },
                },
                new()
                {
                    Perk = PerkType.PredatoryPressure,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.PredatoryPressureTrait },
                },
                new()
                {
                    Perk = PerkType.TwinRupture,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.TwinRupture1, FeatType.TwinRupture2 },
                },
                new()
                {
                    Perk = PerkType.LaceratorStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.LaceratorStance1 },
                },
                new()
                {
                    Perk = PerkType.SanguineTempo,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SanguineTempoTrait },
                },
                new()
                {
                    Perk = PerkType.ArterialReading,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ArterialReadingTrait },
                },
                new()
                {
                    Perk = PerkType.RedBloom,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.RedBloom1 },
                },
            };
        }
    }
}
