using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.SpaceCombat.Engine;
using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.Tests.SpaceCombat.Engine;

[TestFixture]
public class SpaceDefenseLayerTests
{
    private static ShipState Target(double hull = 300, double shield = 100) =>
        ShipState.Create(SpaceTestShips.Ship(hull: hull, shield: shield), "Target", SpaceFight.DefenderSide);

    [Test]
    public void SpaceDamage_GoesToTheShieldBeforeTheHull()
    {
        var target = Target();

        var removed = target.TakeDamage(60, shieldBypass: false, shieldDamageMultiplier: 1);

        target.Shield.Should().Be(40);
        target.Hull.Should().Be(300);
        removed.Should().Be(60);
    }

    [Test]
    public void SpaceDamage_BeyondTheShieldOverflowsToTheHull()
    {
        var target = Target();

        var removed = target.TakeDamage(150, shieldBypass: false, shieldDamageMultiplier: 1);

        target.Shield.Should().Be(0);
        target.Hull.Should().Be(250);
        removed.Should().Be(150);
    }

    [Test]
    public void SpaceDamage_WithAShieldMultiplier_CostsTheShieldMoreAndOverflowsTheRest()
    {
        var target = Target();

        target.TakeDamage(30, shieldBypass: false, shieldDamageMultiplier: 2);
        target.Shield.Should().Be(40);
        target.Hull.Should().Be(300);

        // 40 shield absorbs 20 damage at double cost; the other 10 reaches the hull at face value.
        target.TakeDamage(30, shieldBypass: false, shieldDamageMultiplier: 2);
        target.Shield.Should().Be(0);
        target.Hull.Should().Be(290);
    }

    [Test]
    public void SpaceDamage_WithShieldBypass_GoesStraightToTheHull()
    {
        var target = Target();

        var removed = target.TakeDamage(60, shieldBypass: true, shieldDamageMultiplier: 5);

        target.Shield.Should().Be(100);
        target.Hull.Should().Be(240);
        removed.Should().Be(60);
    }

    [Test]
    public void SpaceDamage_BeyondTheLastHullPoint_IsNotCounted()
    {
        var target = Target(hull: 20, shield: 0);

        var removed = target.TakeDamage(500, shieldBypass: false, shieldDamageMultiplier: 1);

        target.Hull.Should().Be(0);
        target.IsAlive.Should().BeFalse();
        removed.Should().Be(20);
    }

    [Test]
    public void SpaceFight_TorpedoWithShieldBypass_LeavesTheTargetsShieldUntouched()
    {
        var torpedo = SpaceTestShips.Laser(dmg: 30);
        torpedo.ShieldBypass = true;
        var scenario = SpaceTestShips.Scenario(
            SpaceTestShips.Ship("Bomber", weapons: torpedo), SpaceTestShips.Ship("Target", hull: 5000, shield: 500));
        var fight = new SpaceFight(scenario, seed: 1);
        var target = fight.Ships.Single(s => s.SideIndex == SpaceFight.DefenderSide);

        for (var step = 0; step < 100; step++)
            fight.Step(step * 0.1);

        target.Hull.Should().BeLessThan(5000);
        target.Shield.Should().Be(500);
    }

    [Test]
    public void SpaceFight_WithoutShieldBypass_StripsTheShieldFirst()
    {
        var scenario = SpaceTestShips.Scenario(
            SpaceTestShips.Ship("Gunship", weapons: SpaceTestShips.Laser(dmg: 30)), SpaceTestShips.Ship("Target", hull: 5000, shield: 500));
        var fight = new SpaceFight(scenario, seed: 1);
        var target = fight.Ships.Single(s => s.SideIndex == SpaceFight.DefenderSide);

        for (var step = 0; step < 100 && target.Shield == 500; step++)
            fight.Step(step * 0.1);

        target.Shield.Should().BeLessThan(500);
        target.Hull.Should().Be(5000);
    }

    [Test]
    public void Passives_AddToTheShipsPoolsRatingsAndResistances()
    {
        var spec = SpaceTestShips.Ship();
        spec.Resistances.Thermal = 30;
        spec.Passives.Add(new PassiveModuleSpec
        {
            Name = "Plating", Hull = 100, Shield = 50, Defense = 20, Evasion = 5,
            Resistances = new SpaceResistances { Thermal = 90, EM = 10 }
        });

        var ship = ShipState.Create(spec, "Ship", 0);

        ship.MaxHull.Should().Be(1100);
        ship.MaxShield.Should().Be(50);
        ship.Defense.Should().Be(120);
        ship.Evasion.Should().Be(100 + 5 + 10 * 3);
        ship.Resistance(SpaceDamageType.Thermal).Should().Be(100);
        ship.Resistance(SpaceDamageType.EM).Should().Be(10);
    }
}
