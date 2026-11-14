using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.SpaceCombat.Engine;
using SWLOR.CombatSim.SpaceCombat.Scenario;
using SWLOR.Game.Server.Service;

namespace SWLOR.CombatSim.Tests.SpaceCombat.Engine;

[TestFixture]
public class SpaceShotMathTests
{
    private static SpaceScenarioSpec Penalties(int hit = 10, double damage = 0.2) =>
        new() { SizeHitPenaltyPerStep = hit, SizeDamagePenaltyPerStep = damage };

    private static ShipState Ship(ShipSize size, WeaponModuleSpec weapon = null) =>
        ShipState.Create(SpaceTestShips.Ship(size: size, weapons: weapon ?? SpaceTestShips.Laser()), "Ship", 0);

    [Test]
    public void Accuracy_UsesAgilityForEnergyAndPerceptionForLaunchers()
    {
        var spec = SpaceTestShips.Ship();
        spec.Pilot.Agi = 20;
        spec.Pilot.Per = 12;
        spec.Accuracy = 50;
        var ship = ShipState.Create(spec, "Ship", 0);
        var energy = new WeaponModuleSpec { Family = ModuleFamily.Energy, Accuracy = 7 };
        var launcher = new WeaponModuleSpec { Family = ModuleFamily.Launcher, Accuracy = 7 };

        SpaceShotMath.Accuracy(ship, energy).Should().Be(50 + 7 + 20 * 3);
        SpaceShotMath.Accuracy(ship, launcher).Should().Be(50 + 7 + 12 * 3);
    }

    [Test]
    public void DamageAttribute_IsPerceptionForEnergyAndMightForLaunchers()
    {
        var spec = SpaceTestShips.Ship();
        spec.Pilot.Per = 18;
        spec.Pilot.Mgt = 25;
        var ship = ShipState.Create(spec, "Ship", 0);

        SpaceShotMath.DamageAttribute(ship, new WeaponModuleSpec { Family = ModuleFamily.Energy }).Should().Be(18);
        SpaceShotMath.DamageAttribute(ship, new WeaponModuleSpec { Family = ModuleFamily.Launcher }).Should().Be(25);
    }

    [Test]
    public void HitChance_WithEqualSizes_IsTheSharedFormula()
    {
        var attacker = Ship(ShipSize.Medium);
        var target = Ship(ShipSize.Medium);
        var weapon = attacker.Weapons[0].Spec;

        SpaceShotMath.HitChance(Penalties(), attacker, weapon, target)
            .Should().Be(Combat.CalculateHitRate(SpaceShotMath.Accuracy(attacker, weapon), target.Evasion, 0));
    }

    [TestCase(ShipSize.Medium, 65)]
    [TestCase(ShipSize.Large, 55)]
    [TestCase(ShipSize.Capital, 45)]
    public void HitChance_LosesPointsForEachSizeStepTheWeaponIsAboveItsTarget(ShipSize weaponSize, int expected)
    {
        var weapon = SpaceTestShips.Laser();
        weapon.Size = weaponSize;
        var attacker = Ship(ShipSize.Capital, weapon);
        var target = Ship(ShipSize.Small);

        // Accuracy equals evasion, so the base is 75 before the penalty.
        SpaceShotMath.HitChance(Penalties(hit: 10), attacker, weapon, target).Should().Be(expected);
    }

    [Test]
    public void HitChance_WithIgnoreSizePenalty_LosesNothing()
    {
        var weapon = SpaceTestShips.Laser();
        weapon.Size = ShipSize.Capital;
        weapon.IgnoreSizePenalty = true;
        var attacker = Ship(ShipSize.Capital, weapon);
        var target = Ship(ShipSize.Small);

        SpaceShotMath.HitChance(Penalties(hit: 10), attacker, weapon, target).Should().Be(75);
        SpaceShotMath.SizeDamageMultiplier(Penalties(damage: 0.5), Ship(ShipSize.Small, weapon), weapon, Ship(ShipSize.Capital))
            .Should().Be(1);
    }

    [Test]
    public void HitChance_FromASmallerWeapon_HasNoHitPenalty()
    {
        var attacker = Ship(ShipSize.Small);
        var target = Ship(ShipSize.Large);

        SpaceShotMath.HitChance(Penalties(hit: 10), attacker, attacker.Weapons[0].Spec, target).Should().Be(75);
    }

    [Test]
    public void WeaponSize_DefaultsToTheShipsSize()
    {
        var attacker = Ship(ShipSize.Large);

        SpaceShotMath.WeaponSize(attacker, attacker.Weapons[0].Spec).Should().Be(ShipSize.Large);
    }

    [TestCase(ShipSize.Small, ShipSize.Small, 1.0)]
    [TestCase(ShipSize.Medium, ShipSize.Small, 1.0)]
    [TestCase(ShipSize.Small, ShipSize.Medium, 0.8)]
    [TestCase(ShipSize.Small, ShipSize.Large, 0.64)]
    [TestCase(ShipSize.Small, ShipSize.Capital, 0.512)]
    public void SizeDamageMultiplier_LosesAShareForEachSizeStepTheWeaponIsBelowItsTarget(ShipSize weaponSize, ShipSize targetSize, double expected)
    {
        var attacker = Ship(weaponSize);
        var target = Ship(targetSize);

        SpaceShotMath.SizeDamageMultiplier(Penalties(damage: 0.2), attacker, attacker.Weapons[0].Spec, target)
            .Should().BeApproximately(expected, 1e-9);
    }

    [Test]
    public void ResistanceMultiplier_ScalesDamageByTheTargetsResistanceToThatType()
    {
        var spec = SpaceTestShips.Ship();
        spec.Resistances = new SpaceResistances { EM = 25, Thermal = -50, Explosive = 0 };
        var target = ShipState.Create(spec, "Target", 1);

        SpaceShotMath.ResistanceMultiplier(target, SpaceDamageType.EM).Should().Be(0.75);
        SpaceShotMath.ResistanceMultiplier(target, SpaceDamageType.Thermal).Should().Be(1.5);
        SpaceShotMath.ResistanceMultiplier(target, SpaceDamageType.Explosive).Should().Be(1.0);
    }

    [Test]
    public void CriticalChance_UsesTheGroundFormulaWithModuleRankOverTen()
    {
        var weapon = SpaceTestShips.Laser();
        weapon.Rank = 40;
        var spec = SpaceTestShips.Ship(weapons: weapon);
        spec.Pilot.Per = 30;
        var attacker = ShipState.Create(spec, "Attacker", 0);
        var target = Ship(ShipSize.Small);

        var chance = SpaceShotMath.CriticalChance(new SpaceScenarioSpec(), attacker, weapon, target);

        chance.Should().Be(Combat.CalculateCriticalRate(30, 10, 40, 0));
        chance.Should().Be(Combat.MinimumCriticalRate + 4 + 3);
    }

    [Test]
    public void CriticalChance_WithAScenarioOverride_ClampsToItsOwnLimits()
    {
        var attacker = Ship(ShipSize.Small);
        var target = Ship(ShipSize.Small);
        var weapon = attacker.Weapons[0].Spec;

        SpaceShotMath.CriticalChance(new SpaceScenarioSpec { CriticalRateMinimum = 20 }, attacker, weapon, target).Should().Be(20);
        SpaceShotMath.CriticalChance(new SpaceScenarioSpec { CriticalRateMaximum = 5 }, attacker, weapon, target).Should().Be(5);
    }

    [TestCase(0, 1)]
    [TestCase(9, 1)]
    [TestCase(10, 2)]
    [TestCase(45, 5)]
    [TestCase(50, 6)]
    public void DeltaCap_FollowsTheGroundRule(int rank, int expected)
    {
        SpaceShotMath.DeltaCap(rank).Should().Be(expected);
    }
}
