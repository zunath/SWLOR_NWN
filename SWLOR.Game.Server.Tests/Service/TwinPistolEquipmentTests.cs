using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class TwinPistolEquipmentTests
{
    [OneTimeSetUp]
    public void LoadMappings() => Skill.LoadMappings();

    [TestCase(BaseItem.Pistol, true, true)]
    [TestCase(BaseItem.Sling, true, true)]
    [TestCase(BaseItem.Pistol, false, false)]
    [TestCase(BaseItem.LegacyPistol, true, false)]
    [TestCase(BaseItem.CompanionPistol, true, false)]
    [TestCase(BaseItem.Rifle, true, false)]
    public void OwnedPair_RequiresItsExplicitMarkerAndCanonicalPistol(BaseItem type, bool marked, bool expected) =>
        TwinPistolEquipment.IsPairItem(type, marked).Should().Be(expected);

    [TestCase(ItemPropertyType.DMG, true)]
    [TestCase(ItemPropertyType.Delay, true)]
    [TestCase(ItemPropertyType.Accuracy, true)]
    [TestCase(ItemPropertyType.DamageStat, true)]
    [TestCase(ItemPropertyType.AccuracyStat, true)]
    [TestCase(ItemPropertyType.WeaponDamageType, true)]
    [TestCase(ItemPropertyType.HPBonus, false)]
    [TestCase(ItemPropertyType.Stamina, false)]
    [TestCase(ItemPropertyType.Attack, false)]
    [TestCase(ItemPropertyType.WeaponEnhancement, false)]
    [TestCase(ItemPropertyType.EnhancementLevel, false)]
    [TestCase(ItemPropertyType.Blueprint, false)]
    [TestCase(ItemPropertyType.RequiresSkill, false)]
    [TestCase(ItemPropertyType.OnHitCastSpell, false)]
    [TestCase(ItemPropertyType.UnlimitedAmmunition, false)]
    public void GeneratedHand_OnlyMirrorsWeaponProfileAndNeverSlotsOrEquipmentBonuses(ItemPropertyType type, bool expected) =>
        TwinPistolEquipment.IsMirroredProperty(type).Should().Be(expected);

    [TestCase(BaseItem.Pistol, BaseItem.CompanionPistol, true)]
    [TestCase(BaseItem.Sling, BaseItem.CompanionPistol, true)]
    [TestCase(BaseItem.LegacyPistol, BaseItem.CompanionPistol, false)]
    [TestCase(BaseItem.Pistol, BaseItem.OffHandPistol, false)]
    [TestCase(BaseItem.CompanionPistol, BaseItem.CompanionPistol, false)]
    [TestCase(BaseItem.Rifle, BaseItem.CompanionPistol, false)]
    [TestCase(BaseItem.Dagger, BaseItem.CompanionPistol, false)]
    [TestCase(BaseItem.Pistol, BaseItem.SmallShield, false)]
    [TestCase(null, BaseItem.CompanionPistol, false)]
    [TestCase(BaseItem.Pistol, null, false)]
    public void PairClassification_RequiresAnEligibleMainHandAndNewCompanion(BaseItem? main, BaseItem? off, bool expected) =>
        TwinPistolEquipment.IsPair(main, off).Should().Be(expected);

    [TestCase(BaseItem.Pistol, BaseItem.Pistol, BaseItem.CompanionPistol, SkillType.TwinPistol)]
    [TestCase(BaseItem.CompanionPistol, BaseItem.Pistol, BaseItem.CompanionPistol, SkillType.TwinPistol)]
    [TestCase(BaseItem.Pistol, BaseItem.Pistol, BaseItem.SmallShield, SkillType.Pistol)]
    [TestCase(BaseItem.Pistol, BaseItem.Pistol, null, SkillType.Pistol)]
    [TestCase(BaseItem.CompanionPistol, null, BaseItem.CompanionPistol, SkillType.Invalid)]
    [TestCase(BaseItem.CompanionPistol, BaseItem.Rifle, BaseItem.CompanionPistol, SkillType.Invalid)]
    public void EquippedSkill_ChangesWithTheArrangement(BaseItem weapon, BaseItem? main, BaseItem? off, SkillType expected) =>
        TwinPistolEquipment.ResolveSkill(weapon, main, off).Should().Be(expected);

    [Test]
    public void PistolCrossTraining_RetainsItsExplicitAbilitySkill() =>
        TwinPistolEquipment.ResolveSkill(BaseItem.Pistol, BaseItem.Pistol, BaseItem.CompanionPistol, SkillType.Pistol)
            .Should().Be(SkillType.Pistol);

    [TestCase(BaseItem.Pistol, SkillType.Pistol, true, SkillType.TwinPistol)]
    [TestCase(BaseItem.Pistol, SkillType.Pistol, false, SkillType.Pistol)]
    [TestCase(BaseItem.Pistol, SkillType.Armor, true, SkillType.Armor)]
    [TestCase(BaseItem.CompanionPistol, SkillType.TwinPistol, true, SkillType.TwinPistol)]
    [TestCase(BaseItem.Rifle, SkillType.Rifle, true, SkillType.Rifle)]
    [TestCase(BaseItem.LegacyPistol, SkillType.Pistol, true, SkillType.Pistol)]
    public void Requirements_ReinterpretOnlyPairedMainHandPistolRank(BaseItem item, SkillType requirement,
        bool paired, SkillType expected) =>
        TwinPistolEquipment.ResolveRequirementSkill(item, requirement, paired).Should().Be(expected);

    [TestCase(InventorySlot.LeftHand, true)]
    [TestCase(InventorySlot.RightHand, false)]
    [TestCase(InventorySlot.Bullets, false)]
    public void CompanionEquipOrder_AllowsAnInactiveLeftHandFirst(InventorySlot slot, bool allowed) =>
        string.IsNullOrEmpty(EquipmentRestrictions.GetPistolEquipmentError(BaseItem.CompanionPistol, slot, null, null))
            .Should().Be(allowed);

    [Test]
    public void MainHandPistol_CanCompleteThePair() =>
        EquipmentRestrictions.GetPistolEquipmentError(BaseItem.Pistol, InventorySlot.RightHand, null, BaseItem.CompanionPistol)
            .Should().BeEmpty();

    [TestCase(BaseItem.LegacyPistol, InventorySlot.RightHand, null, BaseItem.CompanionPistol)]
    [TestCase(BaseItem.CompanionPistol, InventorySlot.LeftHand, BaseItem.LegacyPistol, null)]
    public void UnsupportedLegacyNativeLauncher_HasAnExplicitErrorInBothEquipOrders(BaseItem item, InventorySlot slot,
        BaseItem? main, BaseItem? off) =>
        EquipmentRestrictions.GetPistolEquipmentError(item, slot, main, off)
            .Should().Be("Legacy pistols cannot be paired with a companion pistol.");

    [TestCase(ItemPropertyType.Accuracy)]
    [TestCase(ItemPropertyType.DMG)]
    [TestCase(ItemPropertyType.Delay)]
    [TestCase(ItemPropertyType.WeaponDamageType)]
    public void CompanionProfileProperties_AreSupported(ItemPropertyType property) =>
        TwinPistolEquipment.IsSupportedCompanionProperty(property).Should().BeTrue();

    [TestCase(ItemPropertyType.Attack)]
    [TestCase(ItemPropertyType.HPBonus)]
    [TestCase(ItemPropertyType.Defense)]
    [TestCase(ItemPropertyType.ShieldDeflection)]
    [TestCase(ItemPropertyType.ACBonus)]
    [TestCase(ItemPropertyType.AbilityBonus)]
    [TestCase(ItemPropertyType.OnHitCastSpell)]
    [TestCase(ItemPropertyType.UnlimitedAmmunition)]
    public void CompanionProperties_CannotLeakNativeOrCreatureWideBonuses(ItemPropertyType property) =>
        TwinPistolEquipment.IsSupportedCompanionProperty(property).Should().BeFalse();

    [Test]
    public void TwinPistol_UsesNormalSkillCapAndWeaponProgression()
    {
        var skill = SkillType.TwinPistol.GetAttribute<SkillType, SkillAttribute>();
        skill.Name.Should().Be("Twin Pistol");
        skill.MaxRank.Should().Be(50);
        skill.ContributesToSkillCap.Should().BeTrue();
        skill.CombatPointCategory.Should().Be(CombatPointCategoryType.Weapon);
        skill.CharacterTypeRestriction.Should().Be(CharacterType.Invalid);
        Skill.GetSkillTypeByBaseItem(BaseItem.Pistol).Should().Be(SkillType.Pistol);
        Skill.GetSkillTypeByBaseItem(BaseItem.CompanionPistol).Should().Be(SkillType.TwinPistol);
        Item.IsAttackWeaponType(BaseItem.CompanionPistol).Should().BeTrue();
        Item.ShieldBaseItemTypes.Should().NotContain(BaseItem.CompanionPistol);
        Item.OneHandedMeleeItemTypes.Should().NotContain(BaseItem.CompanionPistol);
    }
}
