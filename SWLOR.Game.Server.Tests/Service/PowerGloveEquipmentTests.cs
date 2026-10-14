using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Service;

public class PowerGloveEquipmentTests
{
    [OneTimeSetUp]
    public void LoadPureWeaponMappings()
    {
        Skill.LoadMappings();
        foreach (var method in new[] { "LoadItemToDamageStatMapping", "LoadItemToAccuracyStatMapping" })
            typeof(Item).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
    }

    [Test]
    public void PowerGloveUsesAnIndependentEquippedMeleeProfile()
    {
        Item.PowerGloveBaseItemTypes.Should().Equal(BaseItem.PowerGlove);
        Item.WeaponBaseItemTypes.Should().Contain(BaseItem.PowerGlove);
        Item.OneHandedMeleeItemTypes.Should().Contain(BaseItem.PowerGlove);
        Item.IsAttackWeaponType(BaseItem.PowerGlove).Should().BeTrue();
        Item.IsDoubleWeaponType(BaseItem.PowerGlove).Should().BeFalse();
        Item.ArmorBaseItemTypes.Should().NotContain(BaseItem.PowerGlove);
        Item.KatarBaseItemTypes.Should().NotContain(BaseItem.PowerGlove);
        Item.ForceSensitiveWeaponBaseItemTypes.Should().NotContain(BaseItem.PowerGlove);
        Skill.GetSkillTypeByBaseItem(BaseItem.PowerGlove).Should().Be(SkillType.Unarmed);
        Item.GetWeaponDamageAbilityType(BaseItem.PowerGlove).Should().Be(AbilityType.Might);
        Item.GetWeaponAccuracyAbilityType(BaseItem.PowerGlove).Should().Be(AbilityType.Perception);
        WeaponDelay.GetWeaponDelay(BaseItem.PowerGlove).Should().Be(ItemPropertyAttackDelay.Delay220);
    }

    [Test]
    public void ArmorGlovesAndLegacyKatarsKeepTheirOriginalSkill()
    {
        foreach (var type in new[] { BaseItem.Gloves, BaseItem.Bracer, BaseItem.Katar })
            Skill.GetSkillTypeByBaseItem(type).Should().Be(SkillType.Katar);
        Item.ArmorBaseItemTypes.Should().Contain(BaseItem.Gloves).And.Contain(BaseItem.Bracer);
    }

    [Test]
    public void UnarmedIsAWeaponSkillForStandardAndForceSensitiveCharacters()
    {
        var skill = SkillType.Unarmed.GetAttribute<SkillType, SkillAttribute>();
        skill.Category.Should().Be(SkillCategoryType.Combat);
        skill.CombatPointCategory.Should().Be(CombatPointCategoryType.Weapon);
        skill.MaxRank.Should().Be(50);
        skill.ContributesToSkillCap.Should().BeTrue();
        skill.IsAvailableToCharacterType(CharacterType.Standard).Should().BeTrue();
        skill.IsAvailableToCharacterType(CharacterType.ForceSensitive).Should().BeTrue();
    }

    [TestCase(BaseItem.PowerGlove, null, true)]
    [TestCase(BaseItem.PowerGlove, BaseItem.PowerGlove, true)]
    [TestCase(BaseItem.PowerGlove, BaseItem.Katar, false)]
    [TestCase(BaseItem.PowerGlove, BaseItem.Dagger, false)]
    [TestCase(BaseItem.PowerGlove, BaseItem.SmallShield, false)]
    [TestCase(BaseItem.PowerGlove, BaseItem.Invalid, false)]
    [TestCase(BaseItem.Katar, BaseItem.PowerGlove, false)]
    [TestCase(BaseItem.Gloves, null, false)]
    [TestCase(BaseItem.Bracer, null, false)]
    [TestCase(BaseItem.Invalid, null, false)]
    [TestCase(null, BaseItem.PowerGlove, false)]
    [TestCase(null, null, false)]
    public void FullTechniqueGripRequiresOnlyRealPowerGloves(BaseItem? mainHand, BaseItem? offHand, bool expected) =>
        EquipmentPredicates.HasUnarmedGrip(mainHand, offHand).Should().Be(expected);
}
