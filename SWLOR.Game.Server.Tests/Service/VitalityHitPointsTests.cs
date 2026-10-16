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

    [TestCase(0, 0, 0)]
    [TestCase(-1, 0, -1)]
    [TestCase(880, 20, 900)]
    [TestCase(short.MaxValue, 20, short.MaxValue)]
    public void BonusPreservesNativeZeroAndSaturatesPositiveTotals(short maximum, int bonus, short expected)
        => Assert.That(VitalityHitPoints.AddBonus(maximum, bonus), Is.EqualTo(expected));

    [Test]
    public void NestedChangesToAnotherCreatureRetainTheFirstCreaturesUpdatedScore()
    {
        var state = new VitalityRecalculation();
        nint first = 1, second = 2;
        Assert.That(state.Begin(first, 27), Is.True);
        Assert.That(state.Begin(second, 26), Is.True);
        Assert.That(state.Begin(first, 27), Is.False, "same-creature nesting reuses the active window");
        state.Update(first, 28);
        state.Update(second, 27);
        state.End(second);
        Assert.That(state.TryGetScore(first, out var score), Is.True);
        Assert.That(score, Is.EqualTo(28), "returning from the second creature must not restore a stale first-creature score");
        Assert.That(state.TryGetScore(second, out _), Is.False);
        state.End(first);
        Assert.That(state.TryGetScore(first, out _), Is.False);
        state.Update(first, 30);
        Assert.That(state.TryGetScore(first, out _), Is.False, "completed windows cannot be recreated by a modifier callback");
        Assert.That(state.Begin(first, 29), Is.True, "the next recalculation starts from its own current score");
        Assert.That(state.TryGetScore(first, out score), Is.True);
        Assert.That(score, Is.EqualTo(29));
    }
}
