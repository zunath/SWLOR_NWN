using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class AttackWeaponClassificationTests
{
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
