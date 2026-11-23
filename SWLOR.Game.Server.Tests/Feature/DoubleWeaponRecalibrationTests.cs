using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Feature;

public class DoubleWeaponRecalibrationTests
{
    // The Item facade resolves the item service from a container.
    private FacadeContainer _itemFacades = null!;
    private IDisposable _itemFacadeScope = null!;

    [OneTimeSetUp]
    public void BuildItemFacades() => _itemFacades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeItemFacades() => _itemFacades.Dispose();

    [SetUp]
    public void InstallItemFacades() => _itemFacadeScope = _itemFacades.Install();

    [TearDown]
    public void RemoveItemFacades() => _itemFacadeScope.Dispose();

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
