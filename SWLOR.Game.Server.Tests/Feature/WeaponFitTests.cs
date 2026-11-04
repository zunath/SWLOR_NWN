using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class WeaponFitTests
{
    [TestCase(WeaponFitAxis.TranslateX, 999, 0.05f)]
    [TestCase(WeaponFitAxis.TranslateZ, -999, -0.05f)]
    [TestCase(WeaponFitAxis.RotateY, 999, 10f)]
    [TestCase(WeaponFitAxis.RotateZ, -999, -10f)]
    [TestCase(WeaponFitAxis.Scale, 999, 1.1f)]
    [TestCase(WeaponFitAxis.Scale, -999, 0.9f)]
    public void StoredValuesAreClampedToTheLimits(WeaponFitAxis axis, int steps, float expected)
    {
        WeaponFitLimits.GetValue(axis, steps).Should().BeApproximately(expected, 0.0001f);
    }

    [Test]
    public void ZeroStepsLeaveTheAuthoredModelUnchanged()
    {
        foreach (var axis in Enum.GetValues<WeaponFitAxis>())
            WeaponFitLimits.GetValue(axis, 0).Should().Be(axis == WeaponFitAxis.Scale ? 1f : 0f);
    }

    [TestCase(WeaponFitAxis.TranslateY, 3, "+1.5 cm")]
    [TestCase(WeaponFitAxis.TranslateY, -1, "-0.5 cm")]
    [TestCase(WeaponFitAxis.RotateX, 0, "0 deg")]
    [TestCase(WeaponFitAxis.RotateX, -4, "-4 deg")]
    [TestCase(WeaponFitAxis.Scale, 5, "105%")]
    [TestCase(WeaponFitAxis.Scale, -50, "90%")]
    public void ValuesAreShownInPlayerUnits(WeaponFitAxis axis, int steps, string expected)
    {
        WeaponFitLimits.Format(axis, steps).Should().Be(expected);
    }

    [Test]
    public void EachHandStoresItsOwnFit()
    {
        WeaponFit.GetVariableName(InventorySlot.RightHand, WeaponFitAxis.RotateZ).Should().Be("WEAPON_FIT_RIGHT_RotateZ");
        WeaponFit.GetVariableName(InventorySlot.LeftHand, WeaponFitAxis.RotateZ).Should().Be("WEAPON_FIT_LEFT_RotateZ");
    }
}
