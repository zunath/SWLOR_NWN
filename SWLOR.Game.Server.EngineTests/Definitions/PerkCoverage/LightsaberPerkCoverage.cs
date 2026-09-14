using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class LightsaberPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.ForceSheath,
                    MaxLevel = 4,
                    Prices = new[] { 2, 3, 3, 5 },
                    GrantedFeats = new[] { FeatType.ForceSheath1, FeatType.ForceSheath2, FeatType.ForceSheath3, FeatType.ForceSheath4 },
                },
                new()
                {
                    Perk = PerkType.Overpower,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.OverpowerTrait },
                },
                new()
                {
                    Perk = PerkType.FastStrikes,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.FastStrikesTrait },
                },
                new()
                {
                    Perk = PerkType.ShatteringStrike,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.ShatteringStrike1, FeatType.ShatteringStrike2 },
                },
                new()
                {
                    Perk = PerkType.SunderingSweep,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SunderingSweep1, FeatType.SunderingSweep2, FeatType.SunderingSweep3 },
                },
                new()
                {
                    Perk = PerkType.WeakPoints,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.WeakPointsTrait },
                },
                new()
                {
                    Perk = PerkType.ImbuementStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ImbuementStance1 },
                },
                new()
                {
                    Perk = PerkType.HighGround,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.HighGroundTrait },
                },
                new()
                {
                    Perk = PerkType.FocusShift,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.FocusShiftTrait },
                },
                new()
                {
                    Perk = PerkType.Epicenter,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.Epicenter1 },
                },
                new()
                {
                    Perk = PerkType.SaberWard,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.SaberWard1, FeatType.SaberWard2, FeatType.SaberWard3, FeatType.SaberWard4 },
                },
                new()
                {
                    Perk = PerkType.MentalFortress,
                    MaxLevel = 2,
                    Prices = new[] { 2, 4 },
                    GrantedFeats = new[] { FeatType.MentalFortressTrait },
                },
                new()
                {
                    Perk = PerkType.DeflectingReturn,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.DeflectingReturnTrait },
                },
                new()
                {
                    Perk = PerkType.GuardiansChallenge,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.GuardiansChallenge1, FeatType.GuardiansChallenge2 },
                },
                new()
                {
                    Perk = PerkType.SurroundedNotOutmatched,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SurroundedNotOutmatchedTrait },
                },
                new()
                {
                    Perk = PerkType.SaberForceLink,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.ForceLink1 },
                },
                new()
                {
                    Perk = PerkType.ImmovableStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ImmovableStance1 },
                },
                new()
                {
                    Perk = PerkType.Reprisal,
                    MaxLevel = 2,
                    Prices = new[] { 4, 4 },
                    GrantedFeats = new[] { FeatType.Reprisal1, FeatType.Reprisal2 },
                },
                new()
                {
                    Perk = PerkType.CenterOfTheStorm,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.CenterOfTheStormTrait },
                },
                new()
                {
                    Perk = PerkType.AegisEternal,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.AegisEternal1 },
                },
            };
        }
    }
}
