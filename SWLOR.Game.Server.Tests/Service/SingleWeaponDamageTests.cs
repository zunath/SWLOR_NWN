using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class SingleWeaponDamageTests
{
    [Test]
    public void NativeFallback_IsNotAnItemDamageRating()
    {
        var extractor = typeof(SWLOR.Game.Server.Native.GetDamageRoll).GetMethod("ExtractWeaponDamageProfile",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var profile = extractor.Invoke(null, new object[] { null })!;
        var type = profile.GetType();
        type.GetProperty("Damage")!.GetValue(profile).Should().Be(1);
        type.GetProperty("HasItemDamage")!.GetValue(profile).Should().Be(false,
            "the native one-point fallback must not become a weapon DMG property");
    }

    [TestCase(24, 20, 29)]
    [TestCase(24, 30, 32)]
    [TestCase(24, 45, 35)]
    [TestCase(24, 60, 39)]
    [TestCase(4, 20, 5)]
    [TestCase(5, 30, 7)]
    [TestCase(0, 60, 0)]
    [TestCase(24, 0, 24)]
    [TestCase(100, 28, 128)]
    [TestCase(int.MaxValue, 60, int.MaxValue)]
    public void EffectiveRating_RoundsTheCombinedBonusOnce(int damage, int percent, int expected)
        => WeaponDamage.CalculateEffectiveDMG(damage, percent).Should().Be(expected);

    [TestCase(BaseItem.Longsword, true)]
    [TestCase(BaseItem.Dagger, true)]
    [TestCase(BaseItem.Lightsaber, true)]
    [TestCase(BaseItem.ShortSpear, true)]
    [TestCase(BaseItem.Katar, true)]
    [TestCase(BaseItem.Shuriken, true)]
    [TestCase(BaseItem.GreatSword, false)]
    [TestCase(BaseItem.Saberstaff, false)]
    [TestCase(BaseItem.TwinElectroBlade, false)]
    [TestCase(BaseItem.Pistol, false)]
    [TestCase(BaseItem.Rifle, false)]
    [TestCase(BaseItem.CreatureSlashWeapon, false)]
    public void Eligibility_UsesPhysicalEquipmentCategories(BaseItem type, bool expected)
        => WeaponDamage.IsSingleWeaponType(type).Should().Be(expected);

    [Test]
    public void Description_DistinguishesBaseRatingFromViewerBonus()
    {
        var untrained = WeaponDamage.BuildSingleWeaponDescription(24, 20, 0);
        var trained = WeaponDamage.BuildSingleWeaponDescription(24, 20, 40);
        untrained.Should().Contain("Weapon DMG: 24").And.Contain("DMG when wielded alone: 29");
        untrained.Should().NotContain("Doublehand:");
        trained.Should().Contain("Weapon DMG: 24").And.Contain("Additional +40%").And.Contain("DMG when wielded alone: 39 (rounded up once)");
        WeaponDamage.BuildSingleWeaponDescription(24, 20, 0).Should().Be(untrained,
            "a different viewer must not modify an item's persistent rating or another preview");
    }

    [Test]
    public void RatingFeedsTheDamageFormula_WithoutMultiplyingOtherDamageInputs()
    {
        var rating = WeaponDamage.CalculateEffectiveDMG(24, 60);
        Combat.CalculateDamageRange(100, rating + 10, 26, 100, 26, 0)
            .Should().Be((34, 49), "Follow-Through's +10 formula DMG is added after the weapon rating bonus");
    }
}
