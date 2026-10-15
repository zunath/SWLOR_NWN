using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Feature;

public class PercentageDamageOverTimeTests
{
    [TestCase(100, 8, 1, 4)]
    [TestCase(501, 8, 1, 21)]
    [TestCase(14649, 8, 1, 36)]
    [TestCase(17650, 1, 8, 36)]
    [TestCase(25101, 8, 1, 36)]
    [TestCase(32767, 8, 1, 36)]
    [TestCase(14649, -3, -1, 20)]
    [TestCase(0, 0, 0, 1)]
    public void Bleed_PreservesSmallTargetDamageAndCapsBossDamageByTheStrongerSourceStat(
        int targetMaxHP, int might, int perception, int expectedDamage)
    {
        BleedStatusEffect.CalculateTickDamage(targetMaxHP, might, perception)
            .Should().Be(expectedDamage);
    }

    [TestCase(100, 8, 6)]
    [TestCase(501, 8, 31)]
    [TestCase(14649, 8, 54)]
    [TestCase(25101, 8, 54)]
    [TestCase(32767, 8, 54)]
    [TestCase(14649, -3, 30)]
    [TestCase(0, 0, 1)]
    public void Toxin_PreservesSmallTargetDamageAndCapsBossDamageBySourceAgility(
        int targetMaxHP, int agility, int expectedDamage)
    {
        ToxinStatusEffect.CalculateTickDamage(targetMaxHP, agility).Should().Be(expectedDamage);
    }

    [TestCase(20, 44)]
    [TestCase(-50, 18)]
    [TestCase(-100, 1)]
    public void Bleed_OutgoingModifiersApplyAfterTheBaseDamageCap(int bonusPercent, int expectedDamage)
    {
        BleedStatusEffect.CalculateTickDamage(14649, 8, 1, bonusPercent).Should().Be(expectedDamage);
    }

    [Test]
    public void BossTicks_AreBoundedBeforeTraumaResistance()
    {
        // Current Kinrath Queen and Ancient Sand Worm stat skins both have 16% Trauma resistance.
        foreach (var (maxHP, oldTick) in new[] { (14649, 492), (25101, 844) })
        {
            var multiplier = Resistance.CalculateResistanceDamageMultiplier(16);
            Math.Round(GameMath.PercentOf(maxHP, 4) * multiplier).Should().Be(oldTick);
            Math.Round(BleedStatusEffect.CalculateTickDamage(maxHP, 8, 1) * multiplier)
                .Should().Be(30, "boss HP must not amplify the same attacker's Bleed");
        }
    }

    [Test]
    public void LostSource_DoesNotUseTheBossStatsToRaiseTheDamageCap()
    {
        BleedStatusEffect.CalculateTickDamage(32767, 0, 0).Should().Be(20);
        ToxinStatusEffect.CalculateTickDamage(32767, 0).Should().Be(30);
    }
}
