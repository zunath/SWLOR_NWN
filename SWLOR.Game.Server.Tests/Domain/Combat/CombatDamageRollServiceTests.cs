using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The damage roll part of <c>Combat</c>: one draw from the shared <c>Random</c> between the bounds of the rule.
/// </summary>
[TestFixture]
public class CombatDamageRollServiceTests
{
    private readonly CombatDamageRollService _service = new();

    [TearDown]
    public void TearDown() => Random.ResetSeed();

    [TestCase(100, 45, 40, 100, 10, 0, 0)]
    [TestCase(100, 45, 40, 100, 10, 120, 5)]
    [TestCase(60, 8, 14, 90, 30, 0, 2)]
    public void TheDamageRollDrawsOneValueBetweenTheRuleBounds(int attack, int dmg, int attackStat, int defense, int defenseStat, int critical, int deltaCap)
    {
        const int seed = 4242;
        var (minimum, maximum) = CombatRules.CalculateDamageRange(attack, dmg, attackStat, defense, defenseStat, critical, deltaCap);
        Random.SetSeed(seed);
        var expected = (int)Random.NextFloat(minimum, maximum);
        var next = Random.Next();

        Random.SetSeed(seed);
        _service.CalculateDamage(attack, dmg, attackStat, defense, defenseStat, critical, deltaCap).Should().Be(expected);
        Random.Next().Should().Be(next, "the roll draws exactly one value from the shared generator");
    }

    [Test]
    public void TheDamageRollCapsTheStatDeltaAtZeroByDefault()
    {
        const int seed = 99;
        Random.SetSeed(seed);
        var withDefault = _service.CalculateDamage(100, 45, 40, 100, 10, 0);
        Random.SetSeed(seed);

        _service.CalculateDamage(100, 45, 40, 100, 10, 0, 0).Should().Be(withDefault);
    }

    [Test]
    public void TheDamageRollThroughGameServicesIsTheServicesRoll()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();
        const int seed = 7;

        Random.SetSeed(seed);
        var direct = facades.Get<ICombatDamageRollService>().CalculateDamage(100, 45, 40, 100, 10, 0, 5);
        Random.SetSeed(seed);

        GameServices.CombatDamageRoll.CalculateDamage(100, 45, 40, 100, 10, 0, 5).Should().Be(direct);
    }
}
