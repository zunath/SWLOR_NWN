using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class RiflePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.SuppressingShot,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.SuppressingShot1, FeatType.SuppressingShot2, FeatType.SuppressingShot3, FeatType.SuppressingShot4 },
                },
                new()
                {
                    Perk = PerkType.PinningFire,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.PinningFireTrait },
                },
                new()
                {
                    Perk = PerkType.SustainedFire,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SustainedFireTrait },
                },
                new()
                {
                    Perk = PerkType.CripplingShot,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.CripplingShot1, FeatType.CripplingShot2, FeatType.CripplingShot3 },
                },
                new()
                {
                    Perk = PerkType.SpottersRhythm,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SpottersRhythmTrait },
                },
                new()
                {
                    Perk = PerkType.SuppressiveLine,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.SuppressiveLine1, FeatType.SuppressiveLine2 },
                },
                new()
                {
                    Perk = PerkType.SuppressionStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SuppressionStance1 },
                },
                new()
                {
                    Perk = PerkType.Overwatch,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.OverwatchTrait },
                },
                new()
                {
                    Perk = PerkType.ContainmentNet,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ContainmentNetTrait },
                },
                new()
                {
                    Perk = PerkType.KillBox,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.KillBox1 },
                },
                new()
                {
                    Perk = PerkType.AimedShot,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.AimedShot1, FeatType.AimedShot2, FeatType.AimedShot3, FeatType.AimedShot4 },
                },
                new()
                {
                    Perk = PerkType.SteadyAim,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.SteadyAimTrait },
                },
                new()
                {
                    Perk = PerkType.Patience,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.PatienceTrait },
                },
                new()
                {
                    Perk = PerkType.PiercingRound,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.PiercingRound1, FeatType.PiercingRound2, FeatType.PiercingRound3 },
                },
                new()
                {
                    Perk = PerkType.ScopeCalibration,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ScopeCalibrationTrait },
                },
                new()
                {
                    Perk = PerkType.Headshot,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.Headshot1, FeatType.Headshot2 },
                },
                new()
                {
                    Perk = PerkType.SniperStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SniperStance1 },
                },
                new()
                {
                    Perk = PerkType.DeadCenter,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DeadCenterTrait },
                },
                new()
                {
                    Perk = PerkType.BreachRound,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.BreachRoundTrait },
                },
                new()
                {
                    Perk = PerkType.OneShot,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.OneShot1 },
                },
            };
        }
    }
}
