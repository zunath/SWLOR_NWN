using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class DoubleWeaponRecalibrationTests
{
    [TestCase(BaseItem.TwoBladedSword, true)]
    [TestCase(BaseItem.DoubleAxe, true)]
    [TestCase(BaseItem.Saberstaff, true)]
    [TestCase(BaseItem.TwinElectroBlade, true)]
    [TestCase(BaseItem.Lightsaber, false)]
    [TestCase(BaseItem.QuarterStaff, false)]
    [TestCase(BaseItem.Pistol, false)]
    public void DoubleWeaponClassificationMatchesNativeTwoEndFamilies(BaseItem baseItem, bool expected)
    {
        Item.IsDoubleWeaponType(baseItem).Should().Be(expected);
    }
}
