using System;
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
    public void PvpStakes_FollowARecentPlayerHit_NotTheFinalBlow()
    {
        SpaceDeathRules.HasPvpStakes(true, double.MaxValue).Should().BeTrue("a player's final blow");
        SpaceDeathRules.HasPvpStakes(false, 12).Should().BeTrue("an NPC finishing a ship a player hit 12 s ago");
        SpaceDeathRules.HasPvpStakes(false, SpaceDeathRules.PvpStakesSeconds).Should().BeTrue();
        SpaceDeathRules.HasPvpStakes(false, SpaceDeathRules.PvpStakesSeconds + 1).Should().BeFalse("the fight is over");
        SpaceDeathRules.HasPvpStakes(false, double.MaxValue).Should().BeFalse("no player ever hit it");
    }

    [Test]
    public void LoggingOutOfAWildSpaceFight_ForfeitsTheShip()
    {
        SpaceDeathRules.LogoutForfeitsShip(true, 5).Should().BeTrue();
        SpaceDeathRules.LogoutForfeitsShip(true, SpaceDeathRules.PvpStakesSeconds + 1).Should().BeFalse();
        SpaceDeathRules.LogoutForfeitsShip(false, 5).Should().BeFalse("orbits keep their modules");
        var space = System.IO.File.ReadAllText(System.IO.Path.Combine(FindRoot(), "SWLOR.Game.Server", "Service", "Space.cs")).Replace("\r\n", "\n");
        space.IndexOf("ForfeitShipOnPvpLogout(player);", StringComparison.Ordinal)
            .Should().BeLessThan(space.IndexOf("CloneShip(player);\n", space.IndexOf("ForfeitShipOnPvpLogout(player);", StringComparison.Ordinal), StringComparison.Ordinal) + 1,
                "the ship is forfeited before it would be left in space as a clone");
    }

    private static string FindRoot()
    {
        for (var directory = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new System.IO.DirectoryNotFoundException("Could not locate the repository root.");
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
