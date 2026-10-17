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

    [TestCase("None", true)]
    [TestCase("MindResistance", false)]
    [TestCase("AlreadyDazed", false)]
    [TestCase("SharedImmunity", false)]
    public void JointLock_GrantsGuardDefenseOnlyWhenDazedApplies(string rejection, bool grantsDefense)
    {
        var attacker = Fighter("Attacker");
        attacker.Perks.AddRange(new[] { "GuardTraining", "ScrapperControl:3" });
        attacker.Abilities.Add(new() { Feat = "JointLock3" });
        var victim = Fighter("Victim");
        if (rejection == "MindResistance") victim.WeaponCombat.MindResistance = 100;
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { attacker }, new[] { victim }), RuleSets.Current(), _catalog);
        var source = sim.Get("Attacker");
        var target = sim.Get("Victim");
        source.NextActionAt = 0.1;
        if (rejection == "AlreadyDazed") target.Weapon.Control.TryDaze(0, 8).Should().BeTrue();
        if (rejection == "SharedImmunity") target.Weapon.Control.TryDaze(0, 0.01).Should().BeTrue();

        SimulationClock.RunUntil(sim, 1);

        target.Hp.Should().BeLessThan(victim.MaxHp);
        source.Stats.AbilitiesUsed.Should().Be(1);
        source.Weapon.DefensePercent(sim.Now).Should().Be(grantsDefense ? 10 : 0);
        attacker.Abilities[0].IsControl.Should().BeTrue();
    }

    [TestCase(0, true)]
    [TestCase(50, true)]
    [TestCase(100, false)]
    public void HookingStrike_GrantsGuardDefenseUnlessMobilityResistancePreventsHamstring(int resistance, bool grantsDefense)
    {
        var attacker = Fighter("Attacker");
        attacker.Perks.Add("GuardTraining");
        attacker.Abilities.Add(new() { Feat = "HookingStrike4" });
        var victim = Fighter("Victim");
        victim.WeaponCombat.MobilityResistance = resistance;
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { attacker }, new[] { victim }), RuleSets.Current(), _catalog);

        SimulationClock.RunUntil(sim, 1);

        sim.Get("Victim").Hp.Should().BeLessThan(victim.MaxHp);
        sim.Get("Attacker").Weapon.DefensePercent(sim.Now).Should().Be(grantsDefense ? 10 : 0);
    }

    [TestCase(0, 10)]
    [TestCase(100, 0)]
    public void ScrapheapLockdown_RejectedDazeStillGrantsGuardDefenseIfHamstringApplies(int mobility, int defense)
    {
        var attacker = Fighter("Attacker");
        attacker.Perks.Add("GuardTraining");
        attacker.Abilities.Add(new() { Feat = "ScrapheapLockdown1", Target = AbilityTarget.Self });
        var victim = Fighter("Victim");
        victim.WeaponCombat.MindResistance = 100;
        victim.WeaponCombat.MobilityResistance = mobility;
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { attacker }, new[] { victim }), RuleSets.Current(), _catalog);

        SimulationClock.RunUntil(sim, 3);

        sim.Get("Victim").Hp.Should().BeLessThan(victim.MaxHp);
        sim.Get("Victim").Weapon.Control.IsDazed(sim.Now).Should().BeFalse();
        sim.Get("Attacker").Weapon.DefensePercent(sim.Now).Should().Be(defense);
    }

    [TestCase("HookingStrike4", 0, 10)]
    [TestCase("HookingStrike4", 100, 0)]
    [TestCase("JointLock3", 0, 10)]
    [TestCase("JointLock3", 100, 0)]
    public void LethalWeaponControl_ResolvesStatusRidersBeforeDeathWhileAnotherEnemyRemains(string feat, int resistance, int defense)
    {
        var attacker = Fighter("Attacker");
        attacker.Perks.Add("GuardTraining");
        attacker.Abilities.Add(new() { Feat = feat });
        var victim = Fighter("Victim");
        victim.MaxHp = 1;
        victim.WeaponCombat.MindResistance = resistance;
        victim.WeaponCombat.MobilityResistance = resistance;
        var scenario = TestScenarios.TwoTeams(true, new[] { attacker }, new[] { victim, Fighter("Survivor") });
        scenario.Teams[0].TargetPolicy = TargetPolicy.Focus;
        scenario.Teams[0].FocusTarget = "Victim";
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog);

        SimulationClock.RunUntil(sim, () => sim.Get("Victim").IsDead, 60, "the lethal control hit");

        sim.Get("Survivor").IsDead.Should().BeFalse();
        sim.Get("Attacker").Weapon.DefensePercent(sim.Now).Should().Be(defense);
        sim.Get("Victim").Weapon.Control.IsDazed(sim.Now).Should().BeFalse("death clears the status after its self rider resolves");
    }

    [TestCase("None", 10)]
    [TestCase("MindResistance", 0)]
    [TestCase("AlreadyDazed", 0)]
    [TestCase("SharedImmunity", 0)]
    [TestCase("Miss", 0)]
    public void ProfileJointLock_UsesActualStatusApplicationForGuardDefense(string rejection, int defense)
    {
        var attacker = Fighter("Attacker");
        attacker.Perks.Add("GuardTraining");
        attacker.Abilities.Add(new()
        {
            Feat = "JointLock3",
            Damage = new() { Min = 5, Max = 5, Hits = 2, HitChance = rejection == "Miss" ? 0 : 1 }
        });
        var victim = Fighter("Victim");
        if (rejection == "MindResistance") victim.WeaponCombat.MindResistance = 100;
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { attacker }, new[] { victim }), RuleSets.Current(), _catalog);
        var source = sim.Get("Attacker");
        var target = sim.Get("Victim");
        source.NextActionAt = 0.1;
        if (rejection == "AlreadyDazed") target.Weapon.Control.TryDaze(0, 8).Should().BeTrue();
        if (rejection == "SharedImmunity") target.Weapon.Control.TryDaze(0, 0.01).Should().BeTrue();

        SimulationClock.RunUntil(sim, 1);

        attacker.Abilities[0].WeaponPower.Should().BeNull("the calibrated profile remains the damage source");
        target.Hp.Should().Be(victim.MaxHp - (rejection == "Miss" ? 0 : 10));
        source.Weapon.DefensePercent(sim.Now).Should().Be(defense);
        source.Weapon.Diagnostics.GetValueOrDefault("DazesApplied").Should().Be(rejection == "None" ? 1 : 0);
        source.Weapon.Diagnostics.GetValueOrDefault("DazesResisted").Should().Be(rejection is "None" or "Miss" ? 0 : 1);
    }

    [Test]
    public void ProfileAreaControl_LethalSuccessGrantsDefenseWhenOtherVictimsAreImmuneOrMissed()
    {
        var attacker = Fighter("Attacker");
        attacker.Perks.Add("GuardTraining");
        attacker.Abilities.Add(new()
        {
            Feat = "ScrapheapLockdown1", Target = AbilityTarget.Self,
            Damage = new() { Min = 5, Max = 5, Hits = 2, Area = true, HitChanceVs = new() { ["Missed"] = 0 } }
        });
        var lethal = Fighter("Lethal");
        lethal.MaxHp = 1;
        lethal.WeaponCombat.MobilityResistance = 100;
        var immune = Fighter("Immune");
        immune.WeaponCombat.MindResistance = 100;
        immune.WeaponCombat.MobilityResistance = 100;
        var missed = Fighter("Missed");
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { attacker }, new[] { lethal, immune, missed }), RuleSets.Current(), _catalog);

        SimulationClock.RunUntil(sim, 3);

        var source = sim.Get("Attacker");
        sim.Get("Lethal").IsDead.Should().BeTrue();
        sim.Get("Lethal").Weapon.Control.IsDazed(sim.Now).Should().BeFalse();
        sim.Get("Immune").Hp.Should().Be(immune.MaxHp - 10);
        sim.Get("Immune").Weapon.Control.IsDazed(sim.Now).Should().BeFalse();
        sim.Get("Missed").Hp.Should().Be(missed.MaxHp);
        sim.Get("Missed").Weapon.Control.IsDazed(sim.Now).Should().BeFalse();
        source.Weapon.DefensePercent(sim.Now).Should().Be(10);
        source.Weapon.Diagnostics.GetValueOrDefault("DazesApplied").Should().Be(1);
        source.Weapon.Diagnostics.GetValueOrDefault("DazesResisted").Should().Be(1);
    }
}
