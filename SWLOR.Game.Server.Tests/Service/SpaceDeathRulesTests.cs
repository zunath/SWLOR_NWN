using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class SpaceDeathRulesTests
{
    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public void Modules_DropOnlyOnAPlayerKillInWildSpace(bool inWildSpace, bool killerIsPlayer, bool expected)
    {
        SpaceDeathRules.ModulesDrop(inWildSpace, killerIsPlayer).Should().Be(expected);
    }

    [Test]
    public void ModuleChance_IsSixtyFivePercent()
    {
        SpaceDeathRules.ModuleDropChancePercent.Should().Be(65);
        SpaceDeathRules.ModuleDrops(1).Should().BeTrue();
        SpaceDeathRules.ModuleDrops(65).Should().BeTrue();
        SpaceDeathRules.ModuleDrops(66).Should().BeFalse();
        SpaceDeathRules.ModuleDrops(100).Should().BeFalse();
    }

    [Test]
    public void ModuleChance_OverAHundredRolls_DropsSixtyFive()
    {
        Enumerable.Range(1, 100).Count(SpaceDeathRules.ModuleDrops).Should().Be(65);
    }
}
