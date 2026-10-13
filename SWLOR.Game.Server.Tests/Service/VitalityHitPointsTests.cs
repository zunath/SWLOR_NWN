using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Service;

public class VitalityHitPointsTests
{
    [Test]
    public void EveryVitalityPointAddsTwentyHPAtFortyLevels()
    {
        for (var vitality = 10; vitality <= 40; vitality++)
        {
            var nativeHP = (vitality - 10) / 2 * 40;
            Assert.That(nativeHP + VitalityHitPoints.CalculateOddScoreBonus(vitality, 40),
                Is.EqualTo((vitality - 10) * 20), $"Vitality {vitality}");
        }
    }

    [TestCase(27, 40, 20)]
    [TestCase(26, 40, 0)]
    [TestCase(28, 40, 0)]
    [TestCase(11, 1, 0)]
    [TestCase(11, 3, 1)]
    [TestCase(9, 20, 10)]
    [TestCase(27, 0, 0)]
    public void FractionalContributionUsesOnlyEligibleHitDice(int vitality, int levels, int expected)
        => Assert.That(VitalityHitPoints.CalculateOddScoreBonus(vitality, levels), Is.EqualTo(expected));
}