using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class HitPointSpendStaminaRestoreTests
{
    [TestCase(200, 35, 50, 50, 70)]
    [TestCase(200, 35, 50, 0, 70)]
    [TestCase(200, 35, 25, 50, 35)]
    [TestCase(200, 35, 1, 100, 1)]
    public void Restore_ScalesWithTheShareOfTheHitPointCostActuallyPaid(
        int maxStamina,
        int percent,
        int hitPointsSpent,
        int hitPointCost,
        int expected)
    {
        Combat.CalculateHitPointSpendStaminaRestore(maxStamina, percent, hitPointsSpent, hitPointCost)
            .Should().Be(expected);
    }

    [TestCase(200, 35, 0, 50)]
    [TestCase(0, 35, 10, 10)]
    [TestCase(200, 0, 10, 10)]
    public void Restore_IsZeroWhenNothingWasPaidOrRestorable(int maxStamina, int percent, int hitPointsSpent, int hitPointCost)
    {
        Combat.CalculateHitPointSpendStaminaRestore(maxStamina, percent, hitPointsSpent, hitPointCost)
            .Should().Be(0);
    }
}
