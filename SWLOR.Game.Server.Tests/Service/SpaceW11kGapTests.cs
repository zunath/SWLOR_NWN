using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The W11k owner decisions: Espionage XP for each signature a Probe Launcher locates, rejoining an open Rift Run after
/// logging out, and the Ship HUD's Hull Traits row.
/// </summary>
public class SpaceW11kGapTests
{
    // Espionage XP for scanning

    [TestCase(0, 99, false)]
    [TestCase(75, 99, false)]
    [TestCase(75, 100, true)]
    [TestCase(0, 100, true)]
    [TestCase(100, 100, false)]
    public void Located_IsTheCycleThatCrossesIntoOneHundredPercent(int before, int after, bool located)
    {
        ScanningRules.Located(before, after).Should().Be(located);
    }

    [TestCase(3, 0, 30)]
    [TestCase(3, 25, 5)]
    [TestCase(3, 45, -15)]
    [TestCase(9, 0, 60)]
    [TestCase(0, 0, 10)]
    public void EspionageXpDelta_IsTheTiersLevelAgainstTheRank_TheGatheringShape(int tier, int rank, int delta)
    {
        ScanningRules.EspionageXpDelta(tier, rank).Should().Be(delta);
    }

    [Test]
    public void ScanXp_GoesToWhoeverRunsTheLauncher_OncePerLocatedSignature()
    {
        var source = ReadSource("Service", "SpaceScanning.cs");
        source.Should().Contain("if (ScanningRules.Located(before, after))\n                    GiveScanExperience(ship, signature.Tier);");
        source.Should().Contain("var operatorPlayer = SpaceCrew.GetStatSource(ship);");
        source.Should().Contain("Skill.GiveSkillXP(operatorPlayer, SkillType.Espionage, xp);");
        ReadSource("Feature", "ShipModuleDefinition", "ShipSalvageActions.cs")
            .Should().Contain("var delta = Math.Clamp(tier, 1, 6) * 10 - rank;", "the scan rule has the Gathering rule's shape");
    }

    // Rift Run rejoin

    private static RiftRun Run(RiftRunPhase phase = RiftRunPhase.Fighting) => new()
    {
        Phase = phase,
        Pilots = new List<uint> { 10, 11 },
        ShipIds = new Dictionary<uint, string> { [10] = "ship-a", [11] = "ship-b" },
    };

    [Test]
    public void ALoggedOutPilotsShip_WaitsInTheOpenRun_AndThePilotRejoinsWithANewCreature()
    {
        var run = Run();
        RiftRunRules.MarkAway(run, 11);
        run.Pilots.Remove(11);

        run.AwayShips.Should().ContainKey("ship-b");
        run.ShipIds.Should().NotContainKey(11);
        RiftRunRules.CanRejoin(run, "ship-b").Should().BeTrue();
        RiftRunRules.CanRejoin(run, "ship-a").Should().BeFalse("its pilot never left");

        RiftRunRules.Rejoin(run, 42, "ship-b").Should().BeTrue();
        run.Pilots.Should().Equal(10, 42);
        run.ShipIds[42].Should().Be("ship-b");
        run.AwayShips.Should().BeEmpty();
        RiftRunRules.Rejoin(run, 42, "ship-b").Should().BeFalse("a ship rejoins once");
    }

    [TestCase(RiftRunPhase.Waiting, true)]
    [TestCase(RiftRunPhase.Fighting, true)]
    [TestCase(RiftRunPhase.Advancing, true)]
    [TestCase(RiftRunPhase.Returning, false)]
    [TestCase(RiftRunPhase.Closing, false)]
    public void Rejoining_NeedsTheRunStillOpen(RiftRunPhase phase, bool open)
    {
        var run = Run(phase);
        RiftRunRules.MarkAway(run, 11);
        RiftRunRules.CanRejoin(run, "ship-b").Should().Be(open);
        RiftRunRules.CanRejoin(null, "ship-b").Should().BeFalse();
        RiftRunRules.MarkAway(run, 99);
        run.AwayShips.Should().HaveCount(1, "a pilot the run does not know leaves nothing behind");
    }

    [Test]
    public void TheLaunch_GoesToTheCurrentChamber_AndTheRunCountsThePilotAgain()
    {
        var runs = ReadSource("Service", "SpaceRiftRuns.cs");
        runs.Should().Contain("RiftRunRules.MarkAway(run, pilot);");
        runs.Should().Contain("run.ShipIds[pilot] = flown.Id;");
        runs.Should().Contain("return RiftRoomLocation(run, run.Room, dx, dy);");
        var space = ReadSource("Service", "Space.cs");
        space.Should().Contain("var rejoinLocation = GetRiftRunRejoinLocation(dbShip.Id);");
        space.Should().Contain("RejoinRiftRun(player, launch.ShipId);");
        runs.Should().Contain("foreach (var pilot in run.Pilots)\n            {\n                var items = Enumerable.Range(0, pulls)",
            "the payout still goes to every pilot in the run, a rejoined one included");
    }

    // W11m live check: the run's check saw a leaving pilot still valid but out of space mode and dropped them unkept.
    [Test]
    public void ALoggingOutPilot_IsMarkedAwayAtLogout_BeforeTheShipIsUnregistered()
    {
        var space = ReadSource("Service", "Space.cs");
        var exit = space.IndexOf("public static void ExitServer()", StringComparison.Ordinal);
        var away = space.IndexOf("MarkRiftRunPilotAway(player);", exit, StringComparison.Ordinal);
        away.Should().BeGreaterThan(exit);
        away.Should().BeLessThan(space.IndexOf("UnregisterShipPilot(player);", exit, StringComparison.Ordinal));
        ReadSource("Service", "SpaceRiftRuns.cs").Should().Contain(
            "foreach (var run in _riftRuns.Values.Where(x => x.Pilots.Contains(pilot)))\n                RiftRunRules.MarkAway(run, pilot);");
    }

    // W11m live check: a client that took over a minute to load the pocket found its solo run already abandoned.
    [TestCase(true, true, false, true, false, TestName = "In the pocket")]
    [TestCase(true, true, true, false, false, TestName = "Still loading into the pocket")]
    [TestCase(true, true, false, false, true, TestName = "In another area")]
    [TestCase(false, true, true, false, true, TestName = "Logged out")]
    [TestCase(true, false, false, true, true, TestName = "No longer flying")]
    public void APilotStillLoadingIntoThePocket_StaysInTheRun(bool valid, bool inSpace, bool betweenAreas, bool inPocket, bool left)
    {
        RiftRunRules.HasLeftPocket(valid, inSpace, betweenAreas, inPocket).Should().Be(left);
        ReadSource("Service", "SpaceRiftRuns.cs").Should().Contain(
            "RiftRunRules.HasLeftPocket(\n                    GetIsObjectValid(x), IsPlayerInSpaceMode(x), !GetIsObjectValid(GetArea(x)), GetArea(x) == run.Area)");
    }

    // Ship HUD Hull Traits

    [Test]
    public void HullTraits_AreOneRowInTheShipSection()
    {
        var rows = ShipHudRules.BuildStatRows(new ShipHudStats());
        var labels = rows.Select(x => x.Label).ToList();
        labels.IndexOf("Hull Traits").Should().Be(labels.IndexOf("Power Routing") + 1);
        rows.Single(x => x.Label == "Hull Traits").Value.Should().Be("None");
        rows.Count(x => x.Label.StartsWith("Hull Traits")).Should().Be(1);
    }

    [Test]
    public void HullTraits_ListTheTraitNames_AndALongListMovesIntoTheLabel()
    {
        var traits = Enum.GetValues<ShipComponentTraitType>().Where(x => x != ShipComponentTraitType.None)
            .Where(x => ShipTraitRules.Get(x) != null).Take(3).ToList();
        var names = traits.Select(x => ShipTraitRules.Get(x).Name).ToList();
        ShipTraitRules.HullTraitNames(traits).Should().Be(string.Join(", ", names));
        ShipTraitRules.HullTraitNames(null).Should().Be("None");
        ShipHudStats.FromStatus(new ShipStatus { HullTraits = traits }, DateTime.UtcNow).HullTraits.Should().Be(string.Join(", ", names));

        ShipHudRules.HullTraitsRow("Reinforced").Should().BeEquivalentTo(new ShipHudStatRow("Hull Traits", "Reinforced"));
        var longList = "Reinforced, Focused, Overcharged";
        var row = ShipHudRules.HullTraitsRow(longList);
        row.Label.Should().Be($"Hull Traits: {longList}");
        row.Value.Should().BeEmpty();
        ShipHudRules.HullTraitsRow(" ").Value.Should().Be("None");
    }

    private static string ReadSource(params string[] relativePath)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = relativePath.Aggregate(Path.Combine(directory!.FullName, "SWLOR.Game.Server"), Path.Combine);
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
