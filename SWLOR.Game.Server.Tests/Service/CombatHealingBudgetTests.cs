using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatHealingBudgetTests
{
    [Test]
    public void PassiveHealing_SharesItsWindowAcrossFastAttacksAndAreaTargets()
    {
        var now = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
        foreach (var hits in new[] { 10, 20, 100 })
        {
            var budget = NewWindow();
            var total = 0;
            for (var i = 0; i < hits; i++)
                total += budget.Take(Combat.CalculateMaxHPHealingBudget(1000,
                    Combat.MaximumPassiveDamageHealingMaxHPPercentPerWindow), 24, now.AddMilliseconds(i));

            total.Should().Be(240);
        }
    }

    [Test]
    public void RollingWindow_OnlyReleasesTheReceiptsThatHaveExpired()
    {
        var now = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
        var budget = NewWindow();
        budget.Take(60, 30, now).Should().Be(30);
        budget.Take(60, 30, now.AddSeconds(3)).Should().Be(30);
        budget.Take(60, 100, now.AddSeconds(5.999)).Should().Be(0);
        budget.Take(60, 100, now.AddSeconds(6)).Should().Be(30);
        budget.Take(60, 100, now.AddSeconds(8.999)).Should().Be(0);
        budget.Take(60, 100, now.AddSeconds(9)).Should().Be(30);
    }

    [Test]
    public void ActivatedHealing_SharesOneAllowanceAcrossTargetsAndDelayedPhases()
    {
        var sequence = new AbilityImpactSequence();
        sequence.TakeDamageDerivedHealing(1000, 80).Should().Be(80);
        sequence.TakeDamageDerivedHealing(1000, 100).Should().Be(70);
        sequence.TakeDamageDerivedHealing(1000, 100).Should().Be(0);
        sequence.TakeDamageDerivedHealing(2000, 100).Should().Be(0,
            "changing maximum HP during an activation cannot refill its allowance");
        new AbilityImpactSequence().TakeDamageDerivedHealing(1000, 500).Should().Be(150);
    }

    [Test]
    public void PassiveAndKillWindows_DoNotSpendTheActivatedHealingAllowance()
    {
        var now = DateTime.UtcNow;
        var passive = NewWindow();
        var kills = NewWindow();
        passive.Take(240, 1000, now).Should().Be(240);
        kills.Take(120, 1000, now).Should().Be(120);
        new AbilityImpactSequence().TakeDamageDerivedHealing(1000, 500).Should().Be(150);
    }

    [Test]
    public void MultipleKillsAndHealingBonuses_CannotExceedTheSharedKillAllowance()
    {
        var now = DateTime.UtcNow;
        var budget = NewWindow();
        var maximum = Combat.CalculateMaxHPHealingBudget(1000,
            Combat.MaximumDefeatedEnemyHealingMaxHPPercentPerWindow);
        var healed = Enumerable.Range(0, 5).Sum(_ => budget.Take(maximum, 297, now));
        healed.Should().Be(120);
    }

    [TestCase(70, 3, 2)]
    [TestCase(999, 15, 149)]
    [TestCase(1000, 6, 60)]
    [TestCase(0, 15, 0)]
    [TestCase(-100, 15, 0)]
    public void MaximumHPCeilings_NeverRoundAboveTheirPercentage(int hp, int percent, int expected)
    {
        Combat.CalculateMaxHPHealingBudget(hp, percent).Should().Be(expected);
    }

    [TestCase(684, 1000, 0, 684)]
    [TestCase(684, 20, 0, 20)]
    [TestCase(684, 100, 80, 20)]
    [TestCase(684, 100, 200, 0)]
    [TestCase(684, 0, 0, 0)]
    [TestCase(-10, 100, 0, 0)]
    public void OverkillAndPreviouslyQueuedDamage_DoNotSupplyHealing(
        int damage, int targetHP, int pendingDamage, int expected)
    {
        Combat.CalculateDamageEligibleForHealing(damage, targetHP, pendingDamage).Should().Be(expected);
    }

    [Test]
    public void SmallerHealthPool_DoesNotRefundPreviouslySpentHealing()
    {
        var now = DateTime.UtcNow;
        var budget = NewWindow();
        budget.Take(60, 40, now).Should().Be(40);
        budget.Take(30, 100, now.AddSeconds(1)).Should().Be(0);
        budget.Take(60, 100, now.AddSeconds(2)).Should().Be(20);
    }

    private static RollingHealingBudget NewWindow() => new(TimeSpan.FromSeconds(Combat.CombatHealingWindowSeconds));
}
