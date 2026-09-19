using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Feature;
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
        var visual = WeaponAttackAnimation.SelectVisualRolls(rolls, 5900, 0);
        visual.Select(roll => roll.Weapon).Should().Equal(1, 2);
        WeaponAttackAnimation.SwingDuration.Should().Be(1750);
        visual.Should().OnlyContain(roll => rolls.Contains(roll));
        rolls.Length.Should().Be(count, "all actual damage rolls remain intact");
    }

    [TestCase(1750)]
    [TestCase(2500)]
    [TestCase(3500)]
    public void FastCadenceAlternatesFullLengthHandsInsteadOfCompressingThem(int delay)
    {
        var rolls = Rolls(6);
        var previous = (byte)2;
        for (var cycle = 0; cycle < 8; cycle++)
        {
            var visual = WeaponAttackAnimation.SelectVisualRolls(rolls, delay, previous);
            visual.Should().ContainSingle();
            visual[0].Weapon.Should().NotBe(previous);
            previous = visual[0].Weapon;
        }
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

    [TestCase("longsword_b", BaseItem.Longsword, true)]
    [TestCase("b_longsword", BaseItem.Longsword, true)]
    [TestCase("tit_longsword", BaseItem.Longsword, false)]
    [TestCase("longsword_b", BaseItem.Dagger, false)]
    public void LegacyRepairIsLimitedToTheTwoBasicVibrobladeTemplates(string resref, BaseItem baseItem, bool expected)
    {
        BasicVibrobladeCompatibility.IsBasicVibroblade(baseItem, resref).Should().Be(expected);
    }

    private static WeaponAttackAnimation.Roll[] Rolls(int count) =>
        Enumerable.Range(0, count).Select(i => new WeaponAttackAnimation.Roll(1, 1000, 123, 1000, 14,
            (byte)i, 0, 0, 0, (byte)(i < (count + 1) / 2 ? 1 : 2), new short[32])).ToArray();
}
