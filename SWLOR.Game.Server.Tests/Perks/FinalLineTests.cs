using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Perks;

public class FinalLineTests
{
    private static readonly Func<int, int, int, int> Bonus = typeof(FinalLineTechniqueAbilityDefinition).Assembly
        .GetType("SWLOR.Game.Server.Feature.AbilityDefinition.NPC.InnateAbility")!
        .GetMethod("CalculateMissingHpBonus", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Func<int, int, int, int>>();
    private static readonly Func<uint, int, Func<uint, int>, int> Apply = typeof(Ability)
        .GetMethod("ApplyDamagePercentAdjustment", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Func<uint, int, Func<uint, int>, int>>();

    [TestCase(100, 0)]
    [TestCase(99, 0)]
    [TestCase(98, 0)]
    [TestCase(97, 1)]
    [TestCase(80, 7)]
    [TestCase(75, 8)]
    [TestCase(60, 14)]
    [TestCase(50, 17)]
    [TestCase(40, 21)]
    [TestCase(25, 26)]
    [TestCase(20, 28)]
    [TestCase(10, 31)]
    [TestCase(1, 34)]
    [TestCase(0, 35)]
    [TestCase(-10, 35)]
    [TestCase(110, 0)]
    public void TargetHealthDeterminesBonus(int hp, int expected) =>
        Bonus(hp, 100, 35).Should().Be(expected);

    [TestCase(0)]
    [TestCase(-100)]
    public void InvalidMaximumHealthGrantsNoBonus(int maxHp) =>
        Bonus(1, maxHp, 35).Should().Be(0);

    [Test]
    public void EveryHealthValueRespectsLinearRampAndCap()
    {
        foreach (var maximum in new[] { 1, 7, 100, 350, 1000, 32767 })
        {
            var previous = 35;
            for (var hp = 0; hp <= maximum; hp++)
            {
                var actual = Bonus(hp, maximum, 35);
                actual.Should().Be((int)decimal.Floor(35m * (maximum - hp) / maximum));
                actual.Should().BeInRange(0, previous);
                previous = actual;
            }
        }
        Bonus(0, int.MaxValue, 35).Should().Be(35, "intermediate multiplication must not overflow");
        Bonus(int.MinValue, int.MaxValue, 35).Should().Be(35);
    }

    [TestCase(100, 100, 100)]
    [TestCase(80, 100, 107)]
    [TestCase(50, 100, 117)]
    [TestCase(25, 100, 126)]
    [TestCase(1, 100, 134)]
    [TestCase(50, 40, 47)]
    [TestCase(50, 200, 234)]
    [TestCase(50, 1, 2)]
    public void BonusMultipliesResolvedDamageAndRoundsExtraDamageUp(int hp, int rolledDamage, int expected) =>
        Apply(1, rolledDamage, _ => Bonus(hp, 100, 35)).Should().Be(expected);

    [Test]
    public void EachTargetAndSubsequentHitUsesFreshHealth()
    {
        var health = new Dictionary<uint, int> { [1] = 100, [2] = 50, [3] = 1 };
        var calls = new List<uint>();
        int Adjustment(uint target)
        {
            calls.Add(target);
            return Bonus(health[target], 100, 35);
        }
        Apply(1, 100, Adjustment).Should().Be(100);
        Apply(2, 100, Adjustment).Should().Be(117);
        Apply(3, 100, Adjustment).Should().Be(134);
        health[1] = 25;
        Apply(1, 100, Adjustment).Should().Be(126);
        health[1] = 100;
        Apply(1, 100, Adjustment).Should().Be(100, "healing must remove the previous bonus");
        calls.Should().Equal(1u, 2u, 3u, 1u, 1u);
    }

    [Test]
    public void ZeroDamageDoesNotBecomeDamageOrEvaluateTheBonus()
    {
        Apply(1, 0, _ => throw new AssertionException("Zero damage should bypass the bonus"))
            .Should().Be(0);
        Apply(1, 100, null).Should().Be(100);
    }
}
