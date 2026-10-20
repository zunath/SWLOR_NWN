using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public sealed class UnarmedPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases() => new()
        {
            new()
            {
                Perk = PerkType.Jab,
                MaxLevel = 4,
                Prices = new[] { 2, 2, 3, 5 },
                GrantedFeats = new[] { FeatType.Jab1, FeatType.Jab2, FeatType.Jab3, FeatType.Jab4 },
            },
            new()
            {
                Perk = PerkType.FlowingForm,
                MaxLevel = 1,
                Prices = new[] { 2 },
                GrantedFeats = new[] { FeatType.FlowingFormTrait },
            },
            new()
            {
                Perk = PerkType.PalmStrike,
                MaxLevel = 3,
                Prices = new[] { 2, 4, 4 },
                GrantedFeats = new[] { FeatType.PalmStrike1, FeatType.PalmStrike2, FeatType.PalmStrike3 },
            },
            new()
            {
                Perk = PerkType.PracticedForm,
                MaxLevel = 3,
                Prices = new[] { 2, 4, 4 },
                GrantedFeats = new[] { FeatType.PracticedFormTrait, FeatType.PracticedForm2Trait, FeatType.PracticedForm3Trait },
            },
            new()
            {
                Perk = PerkType.MeasuredTechnique,
                MaxLevel = 1,
                Prices = new[] { 4 },
                GrantedFeats = new[] { FeatType.MeasuredTechniqueTrait },
            },
            new()
            {
                Perk = PerkType.FinishingCombination,
                MaxLevel = 2,
                Prices = new[] { 3, 3 },
                GrantedFeats = new[] { FeatType.FinishingCombination1, FeatType.FinishingCombination2 },
            },
            new()
            {
                Perk = PerkType.FlowStance,
                MaxLevel = 1,
                Prices = new[] { 4 },
                GrantedFeats = new[] { FeatType.FlowStance1 },
            },
            new()
            {
                Perk = PerkType.ControlledBreathing,
                MaxLevel = 1,
                Prices = new[] { 4 },
                GrantedFeats = new[] { FeatType.ControlledBreathingTrait },
            },
            new()
            {
                Perk = PerkType.RootedTechnique,
                MaxLevel = 1,
                Prices = new[] { 2 },
                GrantedFeats = new[] { FeatType.RootedTechniqueTrait },
            },
            new()
            {
                Perk = PerkType.PerfectForm,
                MaxLevel = 1,
                Prices = new[] { 6 },
                GrantedFeats = new[] { FeatType.PerfectForm1 },
            },
            new()
            {
                Perk = PerkType.DirtyJab,
                MaxLevel = 4,
                Prices = new[] { 2, 2, 3, 5 },
                GrantedFeats = new[] { FeatType.DirtyJab1, FeatType.DirtyJab2, FeatType.DirtyJab3, FeatType.DirtyJab4 },
            },
            new()
            {
                Perk = PerkType.DirtyFighting,
                MaxLevel = 1,
                Prices = new[] { 2 },
                GrantedFeats = new[] { FeatType.DirtyFightingTrait },
            },
            new()
            {
                Perk = PerkType.Uppercut,
                MaxLevel = 3,
                Prices = new[] { 2, 4, 4 },
                GrantedFeats = new[] { FeatType.Uppercut1, FeatType.Uppercut2, FeatType.Uppercut3 },
            },
            new()
            {
                Perk = PerkType.HeavyBlows,
                MaxLevel = 3,
                Prices = new[] { 2, 4, 4 },
                GrantedFeats = new[] { FeatType.HeavyBlowsTrait, FeatType.HeavyBlows2Trait, FeatType.HeavyBlows3Trait },
            },
            new()
            {
                Perk = PerkType.HardKnocks,
                MaxLevel = 1,
                Prices = new[] { 4 },
                GrantedFeats = new[] { FeatType.HardKnocksTrait },
            },
            new()
            {
                Perk = PerkType.LowKick,
                MaxLevel = 2,
                Prices = new[] { 3, 3 },
                GrantedFeats = new[] { FeatType.LowKick1, FeatType.LowKick2 },
            },
            new()
            {
                Perk = PerkType.BruiserStance,
                MaxLevel = 1,
                Prices = new[] { 4 },
                GrantedFeats = new[] { FeatType.BruiserStance1 },
            },
            new()
            {
                Perk = PerkType.CatchYourBreath,
                MaxLevel = 1,
                Prices = new[] { 4 },
                GrantedFeats = new[] { FeatType.CatchYourBreathTrait },
            },
            new()
            {
                Perk = PerkType.Payback,
                MaxLevel = 1,
                Prices = new[] { 2 },
                GrantedFeats = new[] { FeatType.PaybackTrait },
            },
            new()
            {
                Perk = PerkType.Haymaker,
                MaxLevel = 1,
                Prices = new[] { 6 },
                GrantedFeats = new[] { FeatType.Haymaker1 },
            },
        };
    }
}
