using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.SpaceCombat.Engine;
using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.Tests.SpaceCombat.Engine;

[TestFixture]
public class SpaceFightTests
{
    [Test]
    public void SpaceFight_ToggleWithoutEnoughCapacitor_SkipsTheCyclesItCannotPay()
    {
        // 10 capacitor and no regeneration pay for two 5-capacitor cycles; the eight cycles after that are skipped.
        var attacker = SpaceTestShips.Ship("Gunship", capacitor: 10, weapons: SpaceTestShips.Laser(cycleSeconds: 1, capacitorPerCycle: 5));
        var scenario = SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 10);
        var fight = new SpaceFight(scenario, seed: 1);

        fight.Run();

        var clock = fight.Ships[0].Weapons[0].Clock;
        clock.Fired.Should().Be(2);
        clock.Skipped.Should().Be(8);
        fight.Ships[0].Capacitor.Should().Be(0);
    }

    [Test]
    public void SpaceFight_CapacitorRegeneration_LetsLaterCyclesFire()
    {
        // 5 capacitor per second pays for one 5-capacitor cycle each second indefinitely.
        var attacker = SpaceTestShips.Ship("Gunship", capacitor: 5, weapons: SpaceTestShips.Laser(cycleSeconds: 1, capacitorPerCycle: 5));
        attacker.CapacitorRegenPerSecond = 5;
        var scenario = SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 10);
        var fight = new SpaceFight(scenario, seed: 1);

        fight.Run();

        var clock = fight.Ships[0].Weapons[0].Clock;
        clock.Fired.Should().Be(10);
        clock.Skipped.Should().Be(0);
    }

    [Test]
    public void SpaceFight_EachToggleRunsOnItsOwnCycle()
    {
        var attacker = SpaceTestShips.Ship("Gunship", weapons: new[]
        {
            SpaceTestShips.Laser("Fast", cycleSeconds: 1),
            SpaceTestShips.Laser("Slow", cycleSeconds: 2.5)
        });
        var scenario = SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 10);
        var fight = new SpaceFight(scenario, seed: 1);

        fight.Run();

        fight.Ships[0].Weapons[0].Clock.Fired.Should().Be(10);
        fight.Ships[0].Weapons[1].Clock.Fired.Should().Be(4);
    }

    [Test]
    public void SpaceFight_APassiveSide_NeverFires()
    {
        var gunship = SpaceTestShips.Ship("Gunship", weapons: SpaceTestShips.Laser(dmg: 40));
        var target = SpaceTestShips.Ship("Target", hull: 1000000, weapons: SpaceTestShips.Laser(dmg: 40));
        var scenario = SpaceTestShips.Scenario(gunship, target, defenderPassive: true, durationSeconds: 20);
        var fight = new SpaceFight(scenario, seed: 1);

        var result = fight.Run();

        var passiveShip = fight.Ships.Single(s => s.SideIndex == SpaceFight.DefenderSide);
        passiveShip.Weapons[0].Clock.Fired.Should().Be(0);
        passiveShip.DamageDealt.Should().Be(0);
        result.DamageDealtByDefenders.Should().Be(0);
        fight.Ships.Single(s => s.SideIndex == SpaceFight.AttackerSide).Hull.Should().Be(1000);
        result.DamageDealtByAttackers.Should().BeGreaterThan(0);
    }

    [Test]
    public void SpaceFight_AnActiveDefender_ShootsBack()
    {
        var gunship = SpaceTestShips.Ship("Gunship", weapons: SpaceTestShips.Laser(dmg: 40));
        var target = SpaceTestShips.Ship("Target", weapons: SpaceTestShips.Laser(dmg: 40));
        var scenario = SpaceTestShips.Scenario(gunship, target, defenderPassive: false, durationSeconds: 20);

        var result = new SpaceFight(scenario, seed: 1).Run();

        result.DamageDealtByDefenders.Should().BeGreaterThan(0);
    }

    [Test]
    public void SpaceFight_EachShipFiresAtTheFirstLivingEnemy()
    {
        var gunship = SpaceTestShips.Ship("Gunship", weapons: SpaceTestShips.Laser(dmg: 100));
        var scenario = SpaceTestShips.Scenario(gunship, SpaceTestShips.Ship("First", hull: 1000000), durationSeconds: 20);
        scenario.Defenders.Ships.Add(SpaceTestShips.Ship("Second", hull: 1000000));
        var fight = new SpaceFight(scenario, seed: 1);

        fight.Run();

        fight.Ships.Single(s => s.Name == "First").Hull.Should().BeLessThan(1000000);
        fight.Ships.Single(s => s.Name == "Second").Hull.Should().Be(1000000);
    }

    [Test]
    public void SpaceFight_ShipsWithACount_AreNumbered()
    {
        var scenario = SpaceTestShips.Scenario(SpaceTestShips.Ship("Wing"), SpaceTestShips.Ship("Target"));
        scenario.Attackers.Ships[0].Count = 3;

        var fight = new SpaceFight(scenario, seed: 1);

        fight.Ships.Select(s => s.Name).Should().Equal("Wing 1", "Wing 2", "Wing 3", "Target");
    }

    [Test]
    public void SpaceFight_Repairer_RestoresHullAndPaysCapacitor()
    {
        var spec = SpaceTestShips.Ship("Repaired", capacitor: 100);
        spec.Repairers.Add(new RepairerModuleSpec { Name = "Repairer", Target = RepairTarget.Hull, AmountPerCycle = 50, CycleSeconds = 1, CapacitorPerCycle = 10 });
        var scenario = SpaceTestShips.Scenario(SpaceTestShips.Ship("Gunship"), spec, durationSeconds: 3);
        var fight = new SpaceFight(scenario, seed: 1);
        var ship = fight.Ships.Single(s => s.Name == "Repaired");
        ship.Hull = 500;

        fight.Run();

        ship.Hull.Should().Be(650);
        ship.Capacitor.Should().Be(70);
    }

    [Test]
    public void SpaceFight_BurstAbility_FiresOnCooldownAndSpendsItsCosts()
    {
        var attacker = SpaceTestShips.Ship("Gunship", capacitor: 100, weapons: SpaceTestShips.Laser(dmg: 10, cycleSeconds: 1000));
        attacker.Pilot.MaxStm = 100;
        attacker.Abilities.Add(new BurstAbilitySpec
        {
            Name = "Overcharge", Weapon = "Laser", BonusDmg = 50, CooldownSeconds = 10, CapacitorCost = 20, StmCost = 30
        });
        var scenario = SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 25);
        var fight = new SpaceFight(scenario, seed: 1);

        fight.Run();

        // Presses at 0, 10 and 20 seconds.
        var ship = fight.Ships[0];
        ship.Abilities[0].Clock.Fired.Should().Be(3);
        ship.Stm.Should().Be(10);
        ship.Capacitor.Should().Be(100 - 60);
    }

    [Test]
    public void SpaceFight_BurstAbilityWithTwoShots_PaysOnceAndFiresTwice()
    {
        var laser = SpaceTestShips.Laser(dmg: 10, cycleSeconds: 1000);
        var attacker = SpaceTestShips.Ship("Gunship", capacitor: 100, weapons: laser);
        attacker.Accuracy = 1000;
        attacker.Pilot.MaxStm = 100;
        attacker.Abilities.Add(new BurstAbilitySpec
        {
            Name = "Double Shot", Weapon = "Laser", BonusDmg = 20, Shots = 2, CooldownSeconds = 100, StmCost = 10
        });
        var single = SpaceTestShips.Ship("Gunship", capacitor: 100, weapons: SpaceTestShips.Laser(dmg: 10, cycleSeconds: 1000));
        single.Accuracy = 1000;
        single.Pilot.MaxStm = 100;
        single.Abilities.Add(new BurstAbilitySpec { Name = "Shot", Weapon = "Laser", BonusDmg = 20, CooldownSeconds = 100, StmCost = 10 });

        var twice = new SpaceFight(SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 5), seed: 3);
        var once = new SpaceFight(SpaceTestShips.Scenario(single, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 5), seed: 3);
        twice.Run();
        once.Run();

        twice.Ships[0].Stm.Should().Be(90);
        twice.Ships[0].Abilities[0].Clock.Fired.Should().Be(1);
        twice.Ships[0].DamageDealt.Should().BeGreaterThan(once.Ships[0].DamageDealt);
    }

    [Test]
    public void SpaceFight_ADotTicksEveryPeriodAndARefreshKeepsItsSchedule()
    {
        for (var seed = 1; seed < 100; seed++)
        {
            var attacker = SpaceTestShips.Ship("Bomber", capacitor: 100, weapons: SpaceTestShips.Laser(dmg: 1, cycleSeconds: 1000));
            attacker.Accuracy = 1000;
            attacker.Abilities.Add(new BurstAbilitySpec
            {
                Name = "Volley", Weapon = "Laser", CooldownSeconds = 16,
                Dots = { new DotSpec { Name = "Burn", Damage = 10, PeriodSeconds = 6, DurationSeconds = 30 } }
            });
            var fight = new SpaceFight(SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 60), seed);
            var target = fight.Ships[1];
            fight.Step(0);
            if (target.Dots.Count == 0)
                continue;

            var hull = target.Hull;
            for (var step = 1; step <= 130; step++)
                fight.Step(step * 0.1);
            (hull - target.Hull).Should().Be(20, "ticks at 6 and 12 s");

            for (var step = 131; step <= 170; step++)
                fight.Step(step * 0.1);
            var dot = target.Dots.Single();
            dot.NextTickAt.Should().BeApproximately(18, 1e-6, "a refresh at 16 s keeps the 6 s schedule");
            dot.ExpiresAt.Should().BeOneOf(30, 46);
            return;
        }

        Assert.Fail("No seed landed the first ability shot.");
    }

    [Test]
    public void SpaceFight_SaturationFireExposesOnTheSecondAreaHitWithinTheWindow()
    {
        var laser = SpaceTestShips.Laser(dmg: 1, cycleSeconds: 1000);
        var attacker = SpaceTestShips.Ship("Bomber", capacitor: 100, weapons: laser);
        attacker.Accuracy = 1000;
        attacker.SaturationExposedDefensePercent = 15;
        attacker.Abilities.Add(new BurstAbilitySpec { Name = "Volley", Weapon = "Laser", CooldownSeconds = 18, AreaLauncher = true });
        var fight = new SpaceFight(SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 20), seed: 1);

        fight.Step(0);
        fight.Ships[1].Defense.Should().Be(100, "one area hit is not enough");
        for (var step = 1; step <= 180; step++)
            fight.Step(step * 0.1);

        fight.Ships[1].Defense.Should().Be(SWLOR.Game.Server.Service.SpaceService.SpaceCombatMath.AdjustRating(100, 0, -15),
            "the second area hit within 30 s leaves the target Exposed");
    }

    [Test]
    public void SpaceFight_SameSeed_GivesTheSameResult()
    {
        var scenario = SpaceTestShips.Scenario(
            SpaceTestShips.Ship("Gunship", weapons: SpaceTestShips.Laser(dmg: 30)),
            SpaceTestShips.Ship("Target", hull: 400, shield: 200, weapons: SpaceTestShips.Laser(dmg: 30)),
            defenderPassive: false, durationSeconds: 300);

        var first = new SpaceFight(scenario, seed: 42).Run();
        var second = new SpaceFight(scenario, seed: 42).Run();

        second.Should().BeEquivalentTo(first);
        first.DefendersDestroyedAt.Should().NotBeNull();
    }

    [Test]
    public void SpaceFight_DifferentSeeds_CanGiveDifferentResults()
    {
        var scenario = SpaceTestShips.Scenario(
            SpaceTestShips.Ship("Gunship", weapons: SpaceTestShips.Laser(dmg: 30)),
            SpaceTestShips.Ship("Target", hull: 400, shield: 200), durationSeconds: 300);

        var times = Enumerable.Range(1, 10).Select(seed => new SpaceFight(scenario, seed).Run().EndedAt).Distinct();

        times.Should().HaveCountGreaterThan(1);
    }

    [Test]
    public void SpaceFight_WhenTheLastDefenderDies_TheAttackersWin()
    {
        var scenario = SpaceTestShips.Scenario(
            SpaceTestShips.Ship("Gunship", weapons: SpaceTestShips.Laser(dmg: 200)), SpaceTestShips.Ship("Target", hull: 50), durationSeconds: 60);

        var result = new SpaceFight(scenario, seed: 1).Run();

        result.AttackersWon.Should().BeTrue();
        result.DefendersDestroyedAt.Should().NotBeNull();
        result.EndedAt.Should().Be(result.DefendersDestroyedAt);
    }

    [Test]
    public void SpaceFight_EndingInAStep_LeavesThePoolsAsTheKillingShotLeftThem()
    {
        // The first cycle at 0 seconds kills the target and spends 5 of 10 capacitor; nothing regenerates after that.
        var attacker = SpaceTestShips.Ship("Gunship", capacitor: 10, weapons: SpaceTestShips.Laser(dmg: 1000, capacitorPerCycle: 5));
        attacker.CapacitorRegenPerSecond = 100;
        var scenario = SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1), durationSeconds: 10);
        var fight = new SpaceFight(scenario, seed: 1);

        var result = fight.Run();

        result.EndedAt.Should().Be(0);
        fight.Ships[0].Capacitor.Should().Be(5);
    }

    [Test]
    public void SpaceFight_LastPartialStep_RegeneratesOnlyUpToTheDuration()
    {
        // 0.25 seconds in 0.1-second steps: the third step regenerates 0.05 seconds, not a full step.
        var attacker = SpaceTestShips.Ship("Gunship", capacitor: 1000);
        attacker.CapacitorRegenPerSecond = 100;
        var scenario = SpaceTestShips.Scenario(attacker, SpaceTestShips.Ship("Target", hull: 1000000), durationSeconds: 0.25);
        var fight = new SpaceFight(scenario, seed: 1);
        fight.Ships[0].Capacitor = 0;

        fight.Run();

        fight.Ships[0].Capacitor.Should().BeApproximately(25, 1e-6);
    }
}
