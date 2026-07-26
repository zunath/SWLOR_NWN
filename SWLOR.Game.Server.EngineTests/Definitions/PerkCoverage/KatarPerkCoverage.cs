using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class KatarPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.GuardCounter,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 4 },
                    GrantedFeats = new[] { FeatType.GuardCounter1, FeatType.GuardCounter2, FeatType.GuardCounter3 },
                },
                new()
                {
                    Perk = PerkType.IronGuardTraining,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.IronGuardTrainingTrait },
                },
                new()
                {
                    Perk = PerkType.RedirectingCounter,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.RedirectingCounterTrait },
                },
                new()
                {
                    // Method name is SteelShoulder, but the perk is registered as PerkType.TwinGuardStance.
                    Perk = PerkType.TwinGuardStance,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.TwinGuardStance1 },
                },
                new()
                {
                    Perk = PerkType.IronElbows,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.IronElbowsTrait },
                },
                new()
                {
                    Perk = PerkType.WhirlingGuard,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.WhirlingGuard1 },
                },
                new()
                {
                    Perk = PerkType.CoveringClaws,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CoveringClawsTrait },
                },
                new()
                {
                    Perk = PerkType.GuardReversal,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.GuardReversalTrait },
                },
                new()
                {
                    // Method name is TagIn, but the perk is registered as PerkType.TwinIntercept.
                    Perk = PerkType.TwinIntercept,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.TwinIntercept1 },
                },
                new()
                {
                    Perk = PerkType.RetaliatoryFlow,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.RetaliatoryFlowTrait },
                },
                new()
                {
                    Perk = PerkType.ImpenetrableGrip,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ImpenetrableGripTrait },
                },
                new()
                {
                    Perk = PerkType.IronWallStance,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.IronWallStance1 },
                },
                new()
                {
                    Perk = PerkType.GuardianReflexes,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.GuardianReflexesTrait },
                },
                new()
                {
                    Perk = PerkType.AdamantineGuard,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.AdamantineGuard1 },
                },
                new()
                {
                    Perk = PerkType.HookingStrike,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.HookingStrike1, FeatType.HookingStrike2, FeatType.HookingStrike3, FeatType.HookingStrike4 },
                },
                new()
                {
                    Perk = PerkType.GuardTraining,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.GuardTrainingTrait },
                },
                new()
                {
                    Perk = PerkType.ScrapperControl,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.ScrapperControlTrait },
                },
                new()
                {
                    Perk = PerkType.JointLock,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.JointLock1, FeatType.JointLock2, FeatType.JointLock3 },
                },
                new()
                {
                    Perk = PerkType.RedirectingGuard,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.RedirectingGuardTrait },
                },
                new()
                {
                    Perk = PerkType.InterruptingSweep,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.InterruptingSweep1, FeatType.InterruptingSweep2 },
                },
                new()
                {
                    Perk = PerkType.ScrapperStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ScrapperStance1 },
                },
                new()
                {
                    Perk = PerkType.BreakerReversal,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.BreakerReversalTrait },
                },
                new()
                {
                    Perk = PerkType.IronGrip,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.IronGripTrait },
                },
                new()
                {
                    Perk = PerkType.ScrapheapLockdown,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.ScrapheapLockdown1 },
                },
            };
        }
    }
}
