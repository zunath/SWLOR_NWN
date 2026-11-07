using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class ReadyCueTests
{
    [TestCase(true, false, 35, true)]
    [TestCase(true, false, -3, true)]
    [TestCase(true, true, 35, false)]
    [TestCase(false, false, 35, false)]
    [TestCase(true, false, 0, false)]
    public void ShouldShow_OnlyForAPlayerArmingANewBonus(bool isPlayer, bool wasArmed, int bonus, bool expected)
    {
        ReadyCue.ShouldShow(isPlayer, wasArmed, bonus).Should().Be(expected);
    }

    [Test]
    public void Damage_NamesTheBonusAndItsValue()
    {
        ReadyCue.Damage("Guard Reversal", 35).Should().Be("Guard Reversal Ready: +35 DMG");
    }

    [TestCase(-3)]
    [TestCase(3)]
    public void StaminaDiscount_ShowsTheAmountOnce(int adjustment)
    {
        ReadyCue.StaminaDiscount(adjustment).Should().Be("Next Ability -3 STM");
    }
}
