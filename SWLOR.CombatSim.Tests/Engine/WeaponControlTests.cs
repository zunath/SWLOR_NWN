using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class WeaponControlTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();

    private static CombatantSpec Fighter(string name) => new()
    {
        Name = name, MaxHp = 10000, MaxStm = 100, ReactionSeconds = 0.1f,
        WeaponCombat = new() { Accuracy = 200, Evasion = 100, EnableCriticals = false }
    };

    [Test]
    public void Catalog_ReadsKatarControlDurationsAndInterruptProfile()
    {
        foreach (var feat in new[]{FeatType.JointLock1, FeatType.JointLock2, FeatType.JointLock3})
        {
            var info = _catalog.Get(feat);
            info.ImpactStatusDuration.Should().Be(15);
            info.ImpactStatusTypes.Should().Contain(typeof(DazedStatusEffect));
        }
        _catalog.Get(FeatType.JointLock3).STM.Should().Be(10);
        _catalog.Get(FeatType.JointLock3).SourceWeaponPower.Should().Be(36);
        _catalog.Get(FeatType.ScrapheapLockdown1).ImpactStatusDuration.Should().Be(30);
        _catalog.Get(FeatType.ScrapheapLockdown1).ImpactStatusTypes.Should().Contain(typeof(DazedStatusEffect));
        _catalog.Get(FeatType.InterruptingSweep2).InterruptsTarget.Should().BeTrue();
    }

    [TestCase(15, 0, 0, true, 8)]
    [TestCase(15, 0, 20, true, 6)]
    [TestCase(15, 0, 100, true, 0)]
    [TestCase(15, 20, 0, true, 9)]
    [TestCase(30, 20, 0, true, 15)]
    [TestCase(15, 0, 0, false, 15)]
    public void Duration_UsesProductionResistanceAndPvpTickBudget(int seconds, int outgoing, int resistance, bool pvp, double expected)
    {
        WeaponControlState.Duration(new DazedStatusEffect(), seconds, outgoing, resistance, pvp).Should().Be(expected);
    }

    [Test]
    public void Daze_BlocksNewActionsAndSwingsUntilExpiryAndSurvivesDamage()
    {
        var victim = Fighter("Victim");
        victim.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1 } };
        victim.Abilities.Add(new() { Feat = "Jab4" });
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true,new[]{victim},new[]{Fighter("Enemy")}),RuleSets.Current(),_catalog);
        var target = sim.Get("Victim");
        target.Weapon.Control.TryDaze(0,8).Should().BeTrue();
        sim.ApplyDamage(sim.Get("Enemy"),target,1,directHostile:true);
        target.Weapon.Control.IsDazed(1).Should().BeTrue();
        SimulationClock.RunUntil(sim,7.9);
        target.Stats.AbilitiesUsed.Should().Be(0);
        target.Weapon.Diagnostics.GetValueOrDefault("OrdinaryAttempts").Should().Be(0);
        SimulationClock.RunUntil(sim,10);
        target.Stats.AbilitiesUsed.Should().BeGreaterThan(0);
        target.Weapon.Diagnostics.GetValueOrDefault("OrdinaryAttempts").Should().BeGreaterThan(0);
    }

    [Test]
    public void Daze_DoesNotCancelAnAlreadyStartedScriptedActivation()
    {
        var caster = Fighter("Caster");
        caster.Abilities.Add(new() { Feat = "Haymaker1" });
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true,new[]{caster},new[]{Fighter("Enemy")}),RuleSets.Current(),_catalog);
        var source = sim.Get("Caster");
        SimulationClock.RunUntil(sim,0.2);
        source.Casting.Should().NotBeNull();
        source.Weapon.Control.TryDaze(sim.Now,8).Should().BeTrue();
        SimulationClock.RunUntil(sim,3);
        source.Weapon.Control.IsDazed(sim.Now).Should().BeTrue();
        source.Stats.AbilitiesUsed.Should().Be(1);
        source.Stats.InterruptedCasts.Should().Be(0);
        source.Casting.Should().BeNull();
    }

    [Test]
    public void InterruptingSweep_CancelsAnUnpaidCastWithoutChargingItsResources()
    {
        var caster = Fighter("Caster");
        caster.Abilities.Add(new() { Feat = "Haymaker1" });
        var interrupter = Fighter("Interrupter");
        interrupter.Abilities.Add(new() { Feat = "InterruptingSweep2", Target = AbilityTarget.Self });
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true,new[]{caster},new[]{interrupter}),RuleSets.Current(),_catalog);
        var source = sim.Get("Caster");
        sim.Get("Interrupter").NextActionAt = 0.5;
        SimulationClock.RunUntil(sim,0.2);
        source.Casting.Should().NotBeNull();
        SimulationClock.RunUntil(sim,1);
        source.Stats.InterruptedCasts.Should().Be(1);
        source.Stats.AbilitiesUsed.Should().Be(0);
        source.Stm.Should().Be(100);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InterruptingSweep_ResumesAttacksBeforeTheCanceledCastEndsUnlessStillDazed(bool dazed)
    {
        var caster = Fighter("Caster");
        caster.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1 } };
        caster.Abilities.Add(new() { Feat = "Haymaker1" });
        var interrupter = Fighter("Interrupter");
        interrupter.Abilities.Add(new() { Feat = "InterruptingSweep2", Target = AbilityTarget.Self });
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { caster }, new[] { interrupter }), RuleSets.Current(), _catalog);
        var source = sim.Get("Caster");
        source.NextActionAt = 0;
        sim.Get("Interrupter").NextActionAt = 0.5;
        SimulationClock.RunUntil(sim, 0.2);
        source.Casting.Should().NotBeNull();
        var originalImpactAt = source.Casting.ImpactAt;
        source.Weapon.Diagnostics.GetValueOrDefault("OrdinaryAttempts").Should().Be(0);
        if (dazed) source.Weapon.Control.TryDaze(sim.Now, 8).Should().BeTrue();

        SimulationClock.RunUntil(sim, () => source.Stats.InterruptedCasts == 1, 1, "the explicit interruption");
        source.NextActionAt = originalImpactAt + 1;
        SimulationClock.RunUntil(sim, 1);

        sim.Now.Should().BeLessThan(originalImpactAt);
        source.Weapon.Diagnostics.GetValueOrDefault("OrdinaryAttempts").Should().Be(dazed ? 0 : 1);
        source.Stats.AbilitiesUsed.Should().Be(0);
        source.Stm.Should().Be(100);
    }

    [Test]
    public void Daze_GrantsTwentySecondsOfSharedImmunityAfterExpiry()
    {
        var state = new WeaponControlState();
        state.TryDaze(0,8).Should().BeTrue();
        state.TryDaze(4,8).Should().BeFalse();
        state.IsDazed(8).Should().BeFalse();
        state.TryDaze(27.999,8).Should().BeFalse();
        state.TryDaze(28,8).Should().BeTrue();
        state.Clear();
        state.IsDazed(28).Should().BeFalse();
        state.HardControlImmunityUntil.Should().Be(0);
    }

    [Test]
    public void JointLock_LandedFormulaImpactAppliesControlWhileMindImmunityRejectsIt()
    {
        foreach (var resistance in new[]{0,100})
        {
            var attacker = Fighter("Attacker");
            attacker.Abilities.Add(new() { Feat = "JointLock3" });
            var victim = Fighter("Victim");
            victim.WeaponCombat.MindResistance = resistance;
            var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true,new[]{attacker},new[]{victim}),RuleSets.Current(),_catalog);
            SimulationClock.RunUntil(sim,1);
            var source = sim.Get("Attacker"); var target = sim.Get("Victim");
            target.Hp.Should().BeLessThan(victim.MaxHp);
            target.Weapon.Control.IsDazed(sim.Now).Should().Be(resistance==0);
            source.Weapon.Diagnostics.GetValueOrDefault(resistance==0 ? "DazesApplied" : "DazesResisted").Should().Be(1);
        }
    }
}
