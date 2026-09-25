using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class MarkedForDeathStatusEffectTests
{
    [TestCase(1, 2)]
    [TestCase(8, 2)]
    [TestCase(10, 3)]
    [TestCase(20, 5)]
    [TestCase(30, 8)]
    [TestCase(40, 10)]
    [TestCase(50, 12)]
    [TestCase(60, 12)]
    public void NPCDamageBonus_ScalesWithSourceLevel(int npcLevel, int expectedBonus)
    {
        MarkedForDeathStatusEffect.GetNPCDamageBonus(npcLevel).Should().Be(expectedBonus);
    }

    [Test]
    public void NPCDamageBonus_KeepsFullBonusWhenSourceHasNoNPCLevel()
    {
        MarkedForDeathStatusEffect.GetNPCDamageBonus(0)
            .Should().Be(MarkedForDeathStatusEffect.MaxDamageBonus);
    }

    [Test]
    public void LevelOneMark_CannotRemoveMostOfANewCharactersHP()
    {
        // Three marked hits from a level 1 source against the level 1 base HP pool.
        var totalBonus = MarkedForDeathStatusEffect.GetNPCDamageBonus(1) * 3;

        totalBonus.Should().BeLessThanOrEqualTo(SWLOR.Game.Server.Service.Stat.BaseHP / 10);
    }
}
