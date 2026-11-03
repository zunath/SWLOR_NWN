using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class AttackWeaponClassificationTests
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

    [TestCase(BaseItem.Kama)]
    [TestCase(BaseItem.Club)]
    [TestCase(BaseItem.LightFlail)]
    [TestCase(BaseItem.LightHammer)]
    [TestCase(BaseItem.MorningStar)]
    [TestCase(BaseItem.WarHammer)]
    [TestCase(BaseItem.CreatureBludgeonWeapon)]
    [TestCase(BaseItem.CreaturePierceWeapon)]
    [TestCase(BaseItem.CreatureSlashWeapon)]
    [TestCase(BaseItem.CreatureSlashPierceWeapon)]
    [TestCase(BaseItem.Gloves)]
    [TestCase(BaseItem.Bracer)]
    [TestCase(BaseItem.Bolt)]
    [TestCase(BaseItem.Bullet)]
    public void AttackWeaponProfiles_IncludeMappedAndNaturalWeapons(BaseItem baseItem)
    {
        Item.IsAttackWeaponType(baseItem).Should().BeTrue();
    }

    [TestCase(BaseItem.Armor)]
    [TestCase(BaseItem.LargeShield)]
    [TestCase(BaseItem.CreatureItem)]
    [TestCase(BaseItem.Amulet)]
    public void NonWeaponEquipment_DoesNotBecomeAnAttackWeapon(BaseItem baseItem)
    {
        Item.IsAttackWeaponType(baseItem).Should().BeFalse();
    }
}
