using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Tests.Feature.MigrationDefinition;

public class SaberRecalibrationTests
{
    [TestCase(0, 5, 5)]
    [TestCase(1, 4, 3)]
    [TestCase(2, 6, 7)]
    [TestCase(0, 0, 0)]
    [TestCase(-1, 4, 0)]
    [TestCase(int.MaxValue, int.MaxValue, int.MaxValue)]
    public void CalculateLegacyDamageBonus_UsesTheNativeFlatValueOrMeanRoll(int dice, int die, int expected)
    {
        SaberRecalibration.CalculateLegacyDamageBonus(dice, die).Should().Be(expected);
    }

    [TestCase(false, 0, 0, false, 5, 21, 0, 40)]
    [TestCase(true, 0, 0, false, 5, 21, 0, 40)]
    [TestCase(false, 0, 0, true, 6, 24, 0, 50)]
    [TestCase(true, 0, 0, true, 6, 24, 0, 50)]
    public void CalculateProfile_UsesTierDamageAndSkillBaselines(
        bool saberstaff,
        int currentDamage,
        int accuracy,
        bool upgraded,
        int tier,
        int damage,
        int boundedAccuracy,
        int requiredSkill)
    {
        SaberRecalibration.CalculateProfile(saberstaff, currentDamage, accuracy, upgraded)
            .Should().Be((tier, damage, boundedAccuracy, requiredSkill));
    }

    [Test]
    public void CalculateProfile_KnownValorProfileKeepsChiroAndTwoAccuracyModsWithinDamageCap()
    {
        SaberRecalibration.CalculateProfile(false, currentDamage: 32, accuracy: 10, upgraded: true)
            .Should().Be((6, 29, 10, 50));
    }

    [TestCase(0, 37)]
    [TestCase(5, 33)]
    [TestCase(10, 29)]
    public void CalculateProfile_TradesDamageAllowanceForAccuracySlots(int accuracy, int maximumDamage)
    {
        SaberRecalibration.CalculateProfile(false, int.MaxValue, accuracy, upgraded: true)
            .Damage.Should().Be(maximumDamage);
    }

    [Test]
    public void CalculateProfile_ClampsNegativeAndExtremeInputWithoutOverflow()
    {
        SaberRecalibration.CalculateProfile(false, int.MinValue, int.MinValue, upgraded: false)
            .Should().Be((5, 21, 0, 40));

        SaberRecalibration.CalculateProfile(true, int.MaxValue, int.MaxValue, upgraded: true)
            .Should().Be((6, 29, 10, 50));
    }
}
