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

}
