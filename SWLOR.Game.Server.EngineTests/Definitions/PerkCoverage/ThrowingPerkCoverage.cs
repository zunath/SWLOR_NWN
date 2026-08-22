using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class ThrowingPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.ExplosiveToss,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.ExplosiveToss1, FeatType.ExplosiveToss2, FeatType.ExplosiveToss3, FeatType.ExplosiveToss4 },
                },
                new()
                {
                    Perk = PerkType.PayloadPouch,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.PayloadPouchTrait },
                },
                new()
                {
                    Perk = PerkType.ShrapnelCasing,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.ShrapnelCasingTrait },
                },
                new()
                {
                    Perk = PerkType.FlashToss,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.FlashToss1, FeatType.FlashToss2, FeatType.FlashToss3 },
                },
                new()
                {
                    Perk = PerkType.ClusterPouch,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ClusterPouchTrait },
                },
                new()
                {
                    Perk = PerkType.ConcussiveToss,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.ConcussiveToss1, FeatType.ConcussiveToss2 },
                },
                new()
                {
                    Perk = PerkType.OrdnanceStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.OrdnanceStance1 },
                },
                new()
                {
                    Perk = PerkType.SaturationToss,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SaturationTossTrait },
                },
                new()
                {
                    Perk = PerkType.BombardiersRhythm,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.BombardiersRhythmTrait },
                },
                new()
                {
                    Perk = PerkType.RainOfSteel,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.RainOfSteel1 },
                },
                new()
                {
                    Perk = PerkType.PiercingToss,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.PiercingToss1, FeatType.PiercingToss2, FeatType.PiercingToss3, FeatType.PiercingToss4 },
                },
                new()
                {
                    Perk = PerkType.ReturningGrip,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ReturningGripTrait },
                },
                new()
                {
                    Perk = PerkType.FlurryBleed,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.FlurryBleedTrait },
                },
                new()
                {
                    Perk = PerkType.PinningToss,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.PinningToss1, FeatType.PinningToss2, FeatType.PinningToss3 },
                },
                new()
                {
                    Perk = PerkType.RicochetToss,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.RicochetTossTrait },
                },
                new()
                {
                    Perk = PerkType.SeveringToss,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.SeveringToss1, FeatType.SeveringToss2 },
                },
                new()
                {
                    Perk = PerkType.FlurryStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.FlurryStance1 },
                },
                new()
                {
                    Perk = PerkType.DeepWound,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DeepWoundTrait },
                },
                new()
                {
                    Perk = PerkType.BleedingTempo,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.BleedingTempoTrait },
                },
                new()
                {
                    Perk = PerkType.PerfectFlurry,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.PerfectFlurry1 },
                },
            };
        }
    }
}
