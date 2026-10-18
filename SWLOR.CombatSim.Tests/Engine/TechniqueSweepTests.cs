using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class TechniqueSweepTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();

    private Simulation Setup(MeleeTechniqueStage stage = MeleeTechniqueStage.Opening, bool enabled = true, float secondaryX = 2, params string[] perks)
    {
        var owner = new CombatantSpec
        {
            Name = "owner", MaxHp = 100000, MaxStm = 100, ReactionSeconds = 100,
            WeaponCombat = new() { Stance = "Flow", Accuracy = 10000, EnableCriticals = false }, Perks = perks.ToList(),
            Abilities = new() { new() { Feat = "FinishingCombination2", Target = AbilityTarget.Enemy } }
        };
        CombatantSpec Enemy(string name, float x) => new() { Name = name, MaxHp = 100000, MaxStm = 100, WeaponCombat = new() { PositionX = x } };
        var scenario = TestScenarios.TwoTeams(false, new[] { owner }, new[] { Enemy("primary", 1), Enemy("secondary", secondaryX), Enemy("third", 4) });
        scenario.Teams[0].TargetPolicy = TargetPolicy.Focus;
        scenario.Teams[0].FocusTarget = "primary";
        var rules = RuleSets.Current(); rules.EnableTechniqueSweeps = enabled;
        var sim = TestScenarios.Spawn(scenario, rules, _catalog);
        var c = sim.Get("owner"); var target = sim.Get("primary");
        c.NextActionAt = 0;
        void Use(FeatType feat, double time)
        {
            var info = _catalog.Get(feat); var use = new AbilityUseSpec { Feat = feat.ToString() };
            var activation = c.Weapon.Begin(info, use, target, info.STM, time);
            c.Weapon.Complete(info, use, activation, target, true, time);
        }
        if (stage != MeleeTechniqueStage.None) Use(FeatType.Jab4, -2);
        if (stage == MeleeTechniqueStage.Finish) Use(FeatType.PalmStrike3, -1);
        c.Stats.StmRefunded = 0;
        return sim;
    }

    [TestCase(MeleeTechniqueStage.None, false)]
    [TestCase(MeleeTechniqueStage.Opening, true)]
    [TestCase(MeleeTechniqueStage.Finish, false)]
    public void OnlyOpeningSplitsTwoImpacts(MeleeTechniqueStage stage, bool sweep)
    {
        var sim = Setup(stage);
        SimulationClock.RunUntil(sim, 1);
        sim.Get("owner").Weapon.Diagnostics["AbilityImpactsAttempted"].Should().Be(2);
        if (sweep) sim.Get("secondary").Stats.DamageTakenToHp.Should().BeGreaterThan(0);
        else sim.Get("secondary").Stats.DamageTakenToHp.Should().Be(0);
        sim.Get("third").Stats.DamageTakenToHp.Should().Be(0);
        sim.Get("owner").Weapon.Diagnostics.GetValueOrDefault("SweepCasts").Should().Be(sweep ? 1 : 0);
    }

    [Test] public void SecondarySelectionUsesNearestLivingInRangeEnemy()
    {
        var sim = Setup(secondaryX: 6);
        SimulationClock.RunUntil(sim, 1);
        sim.Get("secondary").Stats.DamageTakenToHp.Should().Be(0);
        sim.Get("third").Stats.DamageTakenToHp.Should().BeGreaterThan(0);
    }

    [Test] public void NoEligibleSecondaryRetainsTwoPrimaryImpactsAndNoAreaRewards()
    {
        var sim = Setup(perks: new[] { "Momentum", "SpinningRhythm:3", "FlowingFootwork", "SweepingAdvance" });
        sim.Get("secondary").IsDead = sim.Get("third").IsDead = true;
        SimulationClock.RunUntil(sim, 1);
        var state = sim.Get("owner").Weapon;
        state.Diagnostics["AbilityImpactsAttempted"].Should().Be(2);
        state.Diagnostics.GetValueOrDefault("SweepCasts").Should().Be(0);
        state.AreaHaste(1).Should().Be(0);
        state.AreaEvasionUntil.Should().Be(0);
        state.Diagnostics.GetValueOrDefault("AreaStaminaRefunded").Should().Be(0);
    }

    [Test] public void SweepAwardsOnceAndKeepsFlowOnPrimary()
    {
        var sim = Setup(perks: new[] { "Momentum", "SpinningRhythm:3", "FlowingFootwork", "SweepingAdvance", "ControlledBreathing" });
        SimulationClock.RunUntil(sim, 1);
        var owner = sim.Get("owner"); var state = owner.Weapon;
        state.Diagnostics["SweepTargetsHit"].Should().Be(2);
        state.AreaHaste(1).Should().Be(9);
        state.EvasionPercent(1).Should().Be(45);
        state.Reactions.WindowTarget.Should().Be(sim.Get("primary").SimId);
        state.Diagnostics["FlowWindows"].Should().Be(1);
        state.Diagnostics["TechniqueRefundedStamina"].Should().Be(2);
        state.Diagnostics["AreaStaminaRefunded"].Should().Be(4);
        owner.Stats.StmRefunded.Should().Be(6);
    }

    [TestCase(9, 8)]
    [TestCase(5, 4)]
    [TestCase(1, 1)]
    [TestCase(0, 0)]
    public void SharedRefundLedgerNeverExceedsPaidCost(int paid, int expected)
    {
        var state = Setup(perks: new[] { "RestorationStrike", "SweepingAdvance" }).Get("owner").Weapon;
        var activation = new TechniqueActivation { PaidStamina = paid, IsSweep = true, ImpactedTargets = 2 };
        state.CostlyAbilityHit(activation);
        state.CostlyAbilityHit(activation);
        state.RestoreHitStamina(activation, 2, "Technique", retainMinimumCost: false);
        state.CompleteSweep(activation, 1);
        activation.HitStaminaRefunded.Should().Be(expected);
    }

    [Test] public void AreaHasteCapsEachSourceAndExpires()
    {
        var state = Setup(perks: new[] { "Momentum", "SpinningRhythm:3" }).Get("owner").Weapon;
        for (var index = 0; index < 10; index++)
            state.CompleteSweep(new() { IsSweep = true, ImpactedTargets = 2 }, index);
        state.AreaHaste(10).Should().Be(27);
        state.AreaHaste(30).Should().Be(18, "later casts do not refresh existing stack clocks");
        state.AreaHaste(31).Should().Be(9);
        state.AreaHaste(32).Should().Be(0);
        state.AreaHaste(39).Should().Be(0);
        state.CompleteSweep(new() { IsSweep = true, ImpactedTargets = 1 }, 40);
        state.AreaHaste(40).Should().Be(0);
    }

    [Test] public void EvadeAccuracyAndDiscountUseProductionPerksAndExpire()
    {
        var state = Setup(perks: new[] { "ImprovedAttentiveness", "OpportunistsFlow" }).Get("owner").Weapon;
        state.AvoidedAttack(0);
        state.AccuracyPercent(1).Should().Be(10);
        state.AvoidedAttackDiscount(9, false, 1).Should().Be(4);
        state.AvoidedAttackDiscount(9, true, 1).Should().Be(4);
        state.AvoidedAttackDiscount(9, true, 2).Should().Be(0);
        state.AccuracyPercent(30).Should().Be(0);
    }

    [Test] public void CounterfactualDisablesOnlySweep()
    {
        var sim = Setup(enabled: false);
        SimulationClock.RunUntil(sim, 1);
        sim.Get("secondary").Stats.DamageTakenToHp.Should().Be(0);
        sim.Get("owner").Weapon.Diagnostics["AbilityImpactsAttempted"].Should().Be(2);
        sim.Get("owner").Weapon.EvasionPercent(1).Should().Be(35);
    }

    [Test] public void RedirectingSecondImpactConservesBareDamageAndCost()
    {
        var sweep = Setup(); var baseline = Setup(enabled: false);
        SimulationClock.RunUntil(sweep, 1); SimulationClock.RunUntil(baseline, 1);
        sweep.Get("owner").Stats.DamageDealt.Should().Be(baseline.Get("owner").Stats.DamageDealt);
        sweep.Get("owner").Stats.StmSpent.Should().Be(baseline.Get("owner").Stats.StmSpent);
        sweep.Get("primary").Stats.DamageTakenToHp.Should().BeLessThan(baseline.Get("primary").Stats.DamageTakenToHp);
    }
}
