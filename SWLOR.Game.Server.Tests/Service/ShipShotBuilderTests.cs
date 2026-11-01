using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// How a revamp weapon step becomes a shot: size scaling and the family riders.
/// </summary>
public class ShipShotBuilderTests
{
    private static ShipModuleStep Step(ShipModuleFamily family, int dmg = 100, ShipSize? fixedSize = null) => new()
    {
        Family = family,
        Rank = 25,
        Dmg = dmg,
        Attack = 50,
        Accuracy = 50,
        WeaponFamily = SpaceWeaponFamily.Energy,
        DamageType = CombatDamageType.Thermal,
        ShieldDamageMultiplier = 1.5,
        FixedSize = fixedSize,
    };

    [TestCase(ShipSize.Small, 100)]
    [TestCase(ShipSize.Medium, 110)]
    [TestCase(ShipSize.Large, 135)]
    [TestCase(ShipSize.Capital, 135)]
    public void Weapon_FiresAsTheHullSizeWithTheSizeMultiplier(ShipSize hull, int expected)
    {
        var shot = ShipShotBuilder.Build(Step(ShipModuleFamily.LaserCannon), hull, ShipSize.Small, 10);

        shot.Dmg.Should().Be(expected);
        shot.Size.Should().BeNull("an unfixed weapon fires as the firing hull's size");
        shot.Rank.Should().Be(25);
        shot.Attack.Should().Be(50);
        shot.Accuracy.Should().Be(50);
        shot.Family.Should().Be(SpaceWeaponFamily.Energy);
        shot.DamageType.Should().Be(CombatDamageType.Thermal);
        shot.ShieldDamageMultiplier.Should().Be(1.5);
    }

    [Test]
    public void FixedSizeWeapon_KeepsItsSizeAndDmgOnAnyHull()
    {
        var capital = ShipShotBuilder.Build(Step(ShipModuleFamily.Turbolaser, fixedSize: ShipSize.Capital), ShipSize.Capital, ShipSize.Large, 10);
        var pointDefense = ShipShotBuilder.Build(Step(ShipModuleFamily.PointDefenseTurret, fixedSize: ShipSize.Small), ShipSize.Large, ShipSize.Large, 10);

        capital.Dmg.Should().Be(100);
        capital.Size.Should().Be(ShipSize.Capital);
        pointDefense.Dmg.Should().Be(100);
        pointDefense.Size.Should().Be(ShipSize.Small);
    }

    [Test]
    public void MissileLauncher_HitsTwentyPercentHarderOnAnUnshieldedTarget()
    {
        var step = Step(ShipModuleFamily.MissileLauncher);

        ShipShotBuilder.Build(step, ShipSize.Small, ShipSize.Medium, 0).Dmg.Should().Be(120);
        ShipShotBuilder.Build(step, ShipSize.Small, ShipSize.Medium, 1).Dmg.Should().Be(100);
        ShipShotBuilder.Build(Step(ShipModuleFamily.LaserCannon), ShipSize.Small, ShipSize.Medium, 0).Dmg.Should().Be(100);
    }

    [Test]
    public void PointDefense_HitsTwentyPercentHarderAgainstSmallTargets()
    {
        var step = Step(ShipModuleFamily.PointDefenseTurret, fixedSize: ShipSize.Small);

        ShipShotBuilder.Build(step, ShipSize.Large, ShipSize.Small, 50).Dmg.Should().Be(120);
        ShipShotBuilder.Build(step, ShipSize.Large, ShipSize.Medium, 50).Dmg.Should().Be(100);
    }

    [Test]
    public void TorpedoFlags_AndAMissingShieldMultiplier_AreKept()
    {
        var step = Step(ShipModuleFamily.TorpedoLauncher);
        step.ShieldBypass = true;
        step.IgnoreSizePenalty = true;
        step.ShieldDamageMultiplier = 0;

        var shot = ShipShotBuilder.Build(step, ShipSize.Small, ShipSize.Capital, 100);

        shot.ShieldBypass.Should().BeTrue();
        shot.IgnoreSizePenalty.Should().BeTrue();
        shot.ShieldDamageMultiplier.Should().Be(1.0);
    }

    [TestCase(ShipSize.Small, 10, 10)]
    [TestCase(ShipSize.Medium, 10, 11)]
    [TestCase(ShipSize.Large, 10, 11)]
    [TestCase(ShipSize.Capital, 10, 11)]
    [TestCase(ShipSize.Small, 0, 1)]
    public void SupportAmounts_ScaleWithTheFiringShipSize(ShipSize size, int amount, int expected)
    {
        ShipShotBuilder.ScaleSupport(amount, size).Should().Be(expected);
    }
}
