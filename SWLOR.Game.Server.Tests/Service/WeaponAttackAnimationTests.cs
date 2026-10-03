using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponAttackAnimationTests
{
    [TestCase(2)]
    [TestCase(5)]
    [TestCase(6)]
    public void OrdinaryCadenceShowsBothHandsAtFullSpeedRegardlessOfHasteRollCount(int count)
    {
        var rolls = Rolls(count);
        var visual = WeaponAttackAnimation.SelectVisualRolls(rolls);
        visual.Select(roll => roll.Weapon).Should().Equal(1, 2);
        WeaponAttackAnimation.CalculateSwingDuration(5900, visual.Length).Should().Be(1000);
        visual.Should().OnlyContain(roll => rolls.Contains(roll));
        rolls.Length.Should().Be(count, "all actual damage rolls remain intact");
    }

    [TestCase(1750)]
    [TestCase(2500)]
    [TestCase(3500)]
    public void FastCadenceStillShowsBothHandsInsideEveryCycle(int delay)
    {
        var rolls = Rolls(6);
        var visual = WeaponAttackAnimation.SelectVisualRolls(rolls);
        visual.Select(roll => roll.Weapon).Should().Equal(1, 2);
        var duration = WeaponAttackAnimation.CalculateSwingDuration(delay, visual.Length);
        duration.Should().BeInRange(775, 1000);
        (visual.Length * (duration + WeaponAttackAnimation.TransitionDuration)).Should().BeLessThanOrEqualTo(delay);
    }

    [TestCase(775)]
    [TestCase(1000)]
    public void EachSwingFinishesThenSendsReadyBeforeTheNextSwing(int duration)
    {
        WeaponAttackAnimation.AdvanceStage(0, duration - 1, duration, 2).Should().Be(0);
        WeaponAttackAnimation.AdvanceStage(0, duration, duration, 2).Should().Be(1);
        WeaponAttackAnimation.AdvanceStage(1, 99, duration, 2).Should().Be(1);
        WeaponAttackAnimation.AdvanceStage(1, 100, duration, 2).Should().Be(2);
        WeaponAttackAnimation.AdvanceStage(2, duration - 1, duration, 2).Should().Be(2);
        WeaponAttackAnimation.AdvanceStage(2, duration, duration, 2).Should().Be(3);
        WeaponAttackAnimation.AdvanceStage(3, 10000, duration, 2).Should().Be(3);
        // Late observers must still receive every intervening stage.
        WeaponAttackAnimation.AdvanceStage(0, 10000, duration, 2).Should().Be(1);
    }

    [Test]
    public void AllThreeMainHandVariantsAreAvailableWithoutImmediateRepeats()
    {
        for (var previous = 0; previous < 3; previous++)
        {
            var choices = Enumerable.Range(0, 2).Select(random => WeaponAttackAnimation.NextVariant(previous, random));
            choices.Should().BeEquivalentTo(Enumerable.Range(0, 3).Where(value => value != previous));
        }
    }

    [TestCase("2wslashl", null, 2, "2wstab")]
    [TestCase("1hstab", null, 0, "1hslashl")]
    [TestCase("1hslashl", "nwslashl", 1, "nwslashr")]
    [TestCase("1hslashl", "ca_attack", 1, "ca_attack")]
    [TestCase("2wslasho", null, 1, null)]
    public void ExplicitVariantsPreserveEquipmentFamiliesAbilitiesAndOffhand(string source, string existing, int variant, string expected)
    {
        WeaponAttackAnimation.VariantReplacement(source, existing, variant).Should().Be(expected);
    }

    [TestCase("longsword_b", BaseItem.Longsword, true, false)]
    [TestCase("b_longsword", BaseItem.Longsword, true, false)]
    [TestCase("dagger_b", BaseItem.Dagger, false, true)]
    [TestCase("b_knife", BaseItem.Dagger, false, true)]
    [TestCase("tit_longsword", BaseItem.Longsword, false, false)]
    [TestCase("longsword_b", BaseItem.Dagger, false, false)]
    public void LegacyRepairIsLimitedToTheBasicWeaponTemplates(
        string resref,
        BaseItem baseItem,
        bool expectedVibroblade,
        bool expectedVibroknife)
    {
        BasicVibrobladeCompatibility.IsBasicVibroblade(baseItem, resref).Should().Be(expectedVibroblade);
        BasicVibrobladeCompatibility.IsBasicVibroknife(baseItem, resref).Should().Be(expectedVibroknife);
    }

    [Test]
    public void QysRetiredBasicDagger_RepairsOnlyTheMissingDamageAndVibroknifeRequirement()
    {
        // Qy's saved dagger_b has Delay 22, CUSTOM_ITEM_PROPERTY_TYPE=4 and
        // DURABILITY_MAX=5. Its damage and skill properties are absent.
        var missing = BasicVibrobladeCompatibility.GetMissingProperties(
            BaseItem.Dagger,
            "dagger_b",
            new[] { ItemPropertyType.Delay });

        missing.Should().Equal(
            (ItemPropertyType.DMG, -1, 5),
            (ItemPropertyType.RequiresSkill, (int)SkillType.Vibroknife, 0));
    }

    [Test]
    public void ExistingBasicWeaponPropertiesArePreservedDuringRepair()
    {
        var missing = BasicVibrobladeCompatibility.GetMissingProperties(
            BaseItem.Dagger,
            "dagger_b",
            new[] { ItemPropertyType.DMG, ItemPropertyType.Delay, ItemPropertyType.RequiresSkill });

        missing.Should().BeEmpty();
    }

    [TestCase("doubleaxe_b", BaseItem.DoubleAxe)]
    [TestCase("twinblade_b", BaseItem.TwoBladedSword)]
    public void RetiredBasicTwinBladeTemplatesRestoreTierOneDamageDelayAndRequirement(
        string resref,
        BaseItem baseItem)
    {
        var missing = BasicVibrobladeCompatibility.GetMissingProperties(baseItem, resref, Enumerable.Empty<ItemPropertyType>());

        missing.Should().Equal(
            (ItemPropertyType.DMG, -1, 5),
            (ItemPropertyType.Delay, -1, 23),
            (ItemPropertyType.RequiresSkill, (int)SkillType.TwinBlade, 0));
    }

    private static WeaponAttackAnimation.Roll[] Rolls(int count) =>
        Enumerable.Range(0, count).Select(i => new WeaponAttackAnimation.Roll(1, 1000, 123, 1000, 14,
            (byte)i, 0, 0, 0, (byte)(i < (count + 1) / 2 ? 1 : 2), new short[32])).ToArray();
}
