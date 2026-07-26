using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class SaberstaffPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.FocusedArc,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.FocusedArc1, FeatType.FocusedArc2, FeatType.FocusedArc3, FeatType.FocusedArc4 },
                },
                new()
                {
                    Perk = PerkType.ConduitTraining,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ConduitTrainingTrait },
                },
                new()
                {
                    Perk = PerkType.BalancedCurrent,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.BalancedCurrentTrait },
                },
                new()
                {
                    Perk = PerkType.GuardedChannel,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.GuardedChannel1, FeatType.GuardedChannel2, FeatType.GuardedChannel3 },
                },
                new()
                {
                    Perk = PerkType.ForceLens,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ForceLensTrait },
                },
                new()
                {
                    Perk = PerkType.SeverFocus,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.SeverFocus1, FeatType.SeverFocus2 },
                },
                new()
                {
                    Perk = PerkType.ConduitStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ConduitStance1 },
                },
                new()
                {
                    Perk = PerkType.EnergizedForms,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.EnergizedFormsTrait },
                },
                new()
                {
                    Perk = PerkType.BalancedAttunement,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.BalancedAttunementTrait },
                },
                new()
                {
                    Perk = PerkType.InfiniteConduit,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.InfiniteConduit1 },
                },
                new()
                {
                    Perk = PerkType.DoubleStrike,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.DoubleStrike1, FeatType.DoubleStrike2, FeatType.DoubleStrike3, FeatType.DoubleStrike4 },
                },
                new()
                {
                    Perk = PerkType.ForceMomentum,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ForceMomentumTrait },
                },
                new()
                {
                    Perk = PerkType.SpinningDeflection,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SpinningDeflectionTrait },
                },
                new()
                {
                    Perk = PerkType.CircleSlash,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.CircleSlash1, FeatType.CircleSlash2, FeatType.CircleSlash3 },
                },
                new()
                {
                    Perk = PerkType.TempestFocus,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.TempestFocusTrait },
                },
                new()
                {
                    Perk = PerkType.MaelstromArc,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.MaelstromArc1, FeatType.MaelstromArc2 },
                },
                new()
                {
                    Perk = PerkType.TempestStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.TempestStance1 },
                },
                new()
                {
                    Perk = PerkType.ForceGyre,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ForceGyreTrait },
                },
                new()
                {
                    Perk = PerkType.FlowOfTheMaelstrom,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.FlowOfTheMaelstromTrait },
                },
                new()
                {
                    Perk = PerkType.SaberCyclone,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.SaberCyclone1 },
                },
            };
        }
    }
}
