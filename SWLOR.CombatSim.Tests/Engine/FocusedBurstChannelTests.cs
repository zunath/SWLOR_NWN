using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class FocusedBurstChannelTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();

    [TestCase(FeatType.FocusedBurst1)]
    [TestCase(FeatType.FocusedBurst2)]
    [TestCase(FeatType.FocusedBurst3)]
    public void Catalog_ReadsProductionChannelCadence(FeatType feat)
    {
        var ability = _catalog.Get(feat);
        ability.IsChanneled.Should().BeTrue();
        ability.CastSeconds.Should().Be(1.5f);
        ability.TwinPistol.VolleyCount.Should().Be(3);
        ability.TwinPistol.VolleyIntervalSeconds.Should().Be(.6f);
        _catalog.Get(FeatType.PairedVolley4).IsChanneled.Should().BeFalse();
        _catalog.Get(FeatType.PairedVolley4).TwinPistol.VolleyCount.Should().Be(1);
    }

    [Test]
    public void Channel_PaysOnce_FiresThreePairedWaves_AndPausesAutosThroughTail()
    {
        var (sim, actor) = Spawn();
        actor.NextAttackAt = 0;
        sim.Step(.05);
        actor.Stats.AbilitiesUsed.Should().Be(1);
        actor.Stats.StmSpent.Should().Be(10);
        actor.Stats.AbilityImpacts.Should().Be(2);
        actor.Stats.OrdinaryAttempts.Should().Be(0);
        SimulationClock.RunUntil(sim, .7, .05);
        actor.Stats.AbilityImpacts.Should().Be(4);
        SimulationClock.RunUntil(sim, 1.3, .05);
        actor.Stats.AbilityImpacts.Should().Be(6);
        actor.Casting.Should().NotBeNull("the settling tail still occupies the actor");
        actor.Stats.OrdinaryAttempts.Should().Be(0);
        SimulationClock.RunUntil(sim, 1.55, .05);
        actor.Casting.Should().BeNull();
        actor.Stats.OrdinaryAttempts.Should().Be(2, "native channel completion resumes attacks immediately after the settling tail");
        SimulationClock.RunUntil(sim, 1.7, .05);
        actor.Stats.OrdinaryAttempts.Should().Be(2);
        actor.Stats.StmSpent.Should().Be(10);
        actor.Stats.AbilityUses["FocusedBurst3"].Should().Be(1);
    }

    [TestCase(.3f, 2)]
    [TestCase(.9f, 4)]
    [TestCase(1.35f, 6)]
    public void ExplicitInterruption_LosesOnlyRemainingWaves_WithoutRefundingPaidCost(float afterSeconds, int impacts)
    {
        var (sim, actor) = Spawn(afterSeconds);
        SimulationClock.RunUntil(sim, 2, .05);
        actor.Stats.AbilityImpacts.Should().Be(impacts);
        actor.Stats.InterruptedCasts.Should().Be(1);
        actor.Stats.AbilitiesUsed.Should().Be(1);
        actor.Stats.StmSpent.Should().Be(10);
        actor.Stats.StmRefunded.Should().Be(0);
        actor.RecastReadyAt[_catalog.Get(FeatType.FocusedBurst3).RecastGroup].Should().Be(24);
    }

    [Test]
    public void DeadPrimary_CancelsRemainingWaves_WithoutRetargeting()
    {
        var (sim, actor) = Spawn();
        sim.Step(.05);
        sim.ApplyDamage(actor, sim.Combatants[1], sim.Combatants[1].Hp);
        SimulationClock.RunUntil(sim, 2, .05);
        actor.Stats.AbilityImpacts.Should().Be(2);
        actor.Casting.Should().BeNull();
        actor.Stats.StmSpent.Should().Be(10);
    }

    [Test]
    public void ChannelInterruption_PreservesExistingOrdinaryAttackCooldown()
    {
        var (sim, actor) = Spawn(.3f);
        actor.NextAttackAt = 2;
        SimulationClock.RunUntil(sim, 1, .05);
        actor.Stats.InterruptedCasts.Should().Be(1);
        actor.NextAttackAt.Should().Be(2,
            "native OnAIActionAttackObject retains its last ordinary swing timestamp across channel cancellation");
        actor.Stats.OrdinaryAttempts.Should().Be(0);
    }

    [TestCase(false, 6)]
    [TestCase(true, 3)]
    public void DistinctEquippedItems_DetermineHands_NotExtraChannelImpacts(bool sharedItem, int impacts)
    {
        var scenario = Create();
        scenario.Teams[0].Members[0].WeaponLoadout.OffhandSharesMainItem = sharedItem;
        var actor = new Simulation(scenario, RuleSets.Current(), _catalog, 47).Run().Combatants[0];
        actor.Stats.AbilityImpacts.Should().Be(impacts);
        actor.Stats.AbilitiesUsed.Should().Be(1);
    }

    private (Simulation, Combatant) Spawn(float? interrupt = null)
    {
        var sim = TestScenarios.Spawn(Create(interrupt), RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextActionAt = 0;
        actor.NextAttackAt = double.PositiveInfinity;
        return (sim, actor);
    }

    private static ScenarioSpec Create(float? interrupt = null)
    {
        var scenario = TwinPistolScenarios.Create(false, true, duration: 2);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new() { "FocusedBurst:3" };
        spec.StmRegenPer30Seconds = 0;
        spec.Abilities = new()
        {
            new() { Feat = "FocusedBurst3", MinIntervalSeconds = 600, ChannelInterruptAfterSeconds = interrupt }
        };
        return scenario;
    }
}
