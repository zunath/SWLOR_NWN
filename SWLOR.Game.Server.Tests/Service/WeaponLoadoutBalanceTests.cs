using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Controlled damage-formula comparisons. These isolate equipment/timing budgets;
/// they are not a substitute for live encounter, resource, or PvP playtesting.
/// </summary>
public class WeaponLoadoutBalanceTests
{
    [TestCase(5, 220)]
    [TestCase(5, 230)]
    [TestCase(5, 240)]
    [TestCase(19, 220)]
    [TestCase(22, 220)]
    [TestCase(24, 230)]
    [TestCase(21, 240)]
    public void NaturalBonus_ProvidesTheEmptyOffhandDamageTradeoff(int rating, int delay)
    {
        foreach (var haste in new[] { -50, 0, 15, 30, 50 })
        {
            var single = MeanDamage(WeaponDamage.CalculateEffectiveDMG(rating, 20), 10) / Interval(delay, 0, haste, 0);
            var paired = 2 * MeanDamage(rating, 10) / Interval(delay, delay, haste, 0);
            (single / paired).Should().BeInRange(1.15, 1.32,
                "untrained paired weapons share the same total baseline roll rate; the natural bonus trades an off-hand item's stats/enhancement slots for harder hits, with larger rounding effects on starter weapons");
            TestContext.Out.WriteLine($"DMG {rating}, Delay {delay}, Haste {haste}: natural/paired {single / paired:0.000}");
        }
    }

    [TestCase(19, 220)]
    [TestCase(22, 220)]
    [TestCase(24, 230)]
    [TestCase(21, 240)]
    public void EqualPriceFrenzyLoadouts_RemainCloseWithPerHitAndCycleDamage(int rating, int delay)
    {
        // Both builds purchase Frenzy (60 SP), their three-rank equipment trait (9 SP),
        // and Provoke (5 SP): 74 SP each. Equal attack/defense, attributes, hit chance,
        // crit, enhancements, and resources. Capped Rundown is +15 after formula;
        // Follow-Through adds +10 formula DMG every third landed auto; Savage Reflexes
        // has a 15% chance of +10 after formula. None of these bonuses is scaled by Doublehand.
        double PerHit(int dmg) => (2 * MeanDamage(dmg, 10) + MeanDamage(dmg + 10, 10)) / 3 + 15 + 1.5;
        foreach (var haste in new[] { -50, 0, 15, 23, 30, 50 })
        {
            var single = PerHit(WeaponDamage.CalculateEffectiveDMG(rating, 60)) / Interval(delay, 0, haste, 0);
            var paired = 2 * PerHit(rating) / Interval(delay, delay, haste, 30);
            (single / paired).Should().BeInRange(0.90, 1.10,
                "equal-SP sustained per-hit builds should retain comparable damage across ordinary haste");
            TestContext.Out.WriteLine($"DMG {rating}, Delay {delay}, Haste {haste}: Doublehand/Frenzy vs Dual/Frenzy {single / paired:0.000}");
        }
    }

    [Test]
    public void QueuedWeaponPower_IsAddedAfterTheRatingBonus()
    {
        var item = WeaponDamage.CalculateEffectiveDMG(24, 60);
        var riot = MeanDamage(item + 25, 10);
        var untrained = MeanDamage(24 + 25, 10);
        (riot / untrained).Should().BeInRange(1.25, 1.35,
            "Riot Blade's +25 power receives no percentage multiplier; queued damage scales less than pure weapon DMG");
        WeaponDamage.CalculateEffectiveDMG(24 + 25, 60).Should().BeGreaterThan(item + 25);
    }

    [TestCase(220)]
    [TestCase(230)]
    [TestCase(240)]
    public void FractionalBatching_DeliversTheAdvertisedRollRate(int delay)
    {
        foreach (var haste in new[] { 0, 15, 30, 50 })
        foreach (var paired in new[] { false, true })
        {
            var interval = Interval(delay, paired ? delay : 0, haste, paired ? 30 : 0);
            var swing = Combat.CalculateAttackSwingDelay(interval);
            var debt = 0f;
            var rolls = 0;
            const int swings = 1000;
            for (var i = 0; i < swings; i++)
                rolls += Combat.CalculateAttacksPerSwing(interval, debt, out debt) * (paired ? 2 : 1);
            var expected = swings * swing / (double)interval * (paired ? 2 : 1);
            Math.Abs(rolls - expected).Should().BeLessThan(2,
                "the native animation floor must carry fractional cycles without creating extra damage rolls");
        }
    }

    private static int Interval(int main, int off, int haste, int reduction) =>
        Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(main, off, haste, reduction));

    private static double MeanDamage(int dmg, int critChance)
    {
        var normal = Combat.CalculateDamageRange(100, dmg, 26, 100, 26, 0);
        var critical = Combat.CalculateDamageRange(100, dmg, 26, 100, 26, Combat.StandardCriticalRating);
        return (normal.Item1 + normal.Item2) / 2d * (100 - critChance) / 100d +
               (critical.Item1 + critical.Item2) / 2d * critChance / 100d;
    }
}
