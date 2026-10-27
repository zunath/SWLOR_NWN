using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Ship-to-ship docking (plan section 10): which sizes pair, how many ships a host carries, the Dock permission,
/// range and combat gates, where an undocking ship goes, and who returns to which ship when the host is destroyed.
/// </summary>
public class ShipDockingRulesTests
{
    private static ShipDockRequest Request(
        ShipSize host = ShipSize.Large,
        ShipSize visitor = ShipSize.Small,
        bool permission = true,
        float distance = 10f,
        bool hostInCombat = false,
        bool visitorInCombat = false,
        int docked = 0,
        bool sameShip = false) => new()
    {
        HostSize = host,
        VisitorSize = visitor,
        HasDockPermission = permission,
        Distance = distance,
        HostInCombat = hostInCombat,
        VisitorInCombat = visitorInCombat,
        HostDockedCount = docked,
        SameShip = sameShip,
    };

    [TestCase(ShipSize.Large, ShipSize.Small, ShipDockRefusal.None)]
    [TestCase(ShipSize.Large, ShipSize.Medium, ShipDockRefusal.None)]
    [TestCase(ShipSize.Capital, ShipSize.Small, ShipDockRefusal.None)]
    [TestCase(ShipSize.Capital, ShipSize.Medium, ShipDockRefusal.None)]
    [TestCase(ShipSize.Small, ShipSize.Small, ShipDockRefusal.HostTooSmall)]
    [TestCase(ShipSize.Medium, ShipSize.Small, ShipDockRefusal.HostTooSmall)]
    [TestCase(ShipSize.Capital, ShipSize.Large, ShipDockRefusal.VisitorTooLarge)]
    [TestCase(ShipSize.Capital, ShipSize.Capital, ShipDockRefusal.VisitorTooLarge)]
    public void SmallAndMediumShipsDockAboardLargeAndCapitalShips(ShipSize host, ShipSize visitor, ShipDockRefusal expected)
    {
        ShipDockingRules.GetDockRefusal(Request(host, visitor)).Should().Be(expected);
    }

    [Test]
    public void ALargeHostCarriesOneShipAndACapitalHostTwo()
    {
        ShipDockingRules.GetCapacity(ShipSize.Large).Should().Be(1);
        ShipDockingRules.GetCapacity(ShipSize.Capital).Should().Be(2);
        ShipDockingRules.GetCapacity(ShipSize.Medium).Should().Be(0);

        ShipDockingRules.GetDockRefusal(Request(ShipSize.Large, docked: 1)).Should().Be(ShipDockRefusal.HostFull);
        ShipDockingRules.GetDockRefusal(Request(ShipSize.Capital, docked: 1)).Should().Be(ShipDockRefusal.None);
        ShipDockingRules.GetDockRefusal(Request(ShipSize.Capital, docked: 2)).Should().Be(ShipDockRefusal.HostFull);
    }

    [Test]
    public void DockingNeedsTheDockPermission()
    {
        ShipDockingRules.GetDockRefusal(Request(permission: false)).Should().Be(ShipDockRefusal.NoPermission);
        ShipDockingRules.GetDockRefusal(Request(sameShip: true)).Should().Be(ShipDockRefusal.SameShip);
    }

    [Test]
    public void DockingWorksWithinTheCommandRange()
    {
        ShipDockingRules.DockRange.Should().Be(SpaceRadii.CommandRange).And.Be(30f);
        ShipDockingRules.GetDockRefusal(Request(distance: 30f)).Should().Be(ShipDockRefusal.None);
        ShipDockingRules.GetDockRefusal(Request(distance: 30.5f)).Should().Be(ShipDockRefusal.OutOfRange);
        ShipDockingRules.GetDockRefusal(Request(distance: float.MaxValue)).Should().Be(ShipDockRefusal.OutOfRange,
            "ships in different areas are out of range");
    }

    [Test]
    public void NeitherShipMayBeInCombat()
    {
        var now = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        ShipDockingRules.CombatWindowSeconds.Should().Be(10);

        ShipDockingRules.IsInCombat(true, DateTime.MinValue, now).Should().BeTrue("an enemy holds threat on the ship");
        ShipDockingRules.IsInCombat(false, now.AddSeconds(-9.9), now).Should().BeTrue("it took or dealt damage 9.9 s ago");
        ShipDockingRules.IsInCombat(false, now.AddSeconds(-10), now).Should().BeFalse();
        ShipDockingRules.IsInCombat(false, DateTime.MinValue, now).Should().BeFalse();

        ShipDockingRules.GetDockRefusal(Request(hostInCombat: true)).Should().Be(ShipDockRefusal.HostInCombat);
        ShipDockingRules.GetDockRefusal(Request(visitorInCombat: true)).Should().Be(ShipDockRefusal.VisitorInCombat);
    }

    [Test]
    public void EveryRefusalHasAPlainAsciiMessage()
    {
        foreach (var refusal in Enum.GetValues<ShipDockRefusal>().Where(x => x != ShipDockRefusal.None))
        {
            var message = ShipDockingRules.DescribeRefusal(refusal, ShipSize.Capital);
            message.Should().NotBeNullOrWhiteSpace(refusal.ToString());
            message.All(c => c < 128).Should().BeTrue(message);
        }

        ShipDockingRules.DescribeRefusal(ShipDockRefusal.HostFull, ShipSize.Large).Should().Be("This ship already has a ship docked aboard.");
        ShipDockingRules.DescribeRefusal(ShipDockRefusal.HostFull, ShipSize.Capital).Should().Be("This ship already has 2 ships docked aboard.");
        ShipDockingRules.DescribeRefusal(ShipDockRefusal.OutOfRange, ShipSize.Large).Should().Be("You must be within 30 meters of the ship to dock.");
    }

    [Test]
    public void LegacyHullsWithoutASizeCountSmallOrCapital()
    {
        ShipDockingRules.ResolveSize(ShipSize.Invalid, false).Should().Be(ShipSize.Small);
        ShipDockingRules.ResolveSize(ShipSize.Invalid, true).Should().Be(ShipSize.Capital);
        ShipDockingRules.ResolveSize(ShipSize.Medium, true).Should().Be(ShipSize.Medium);
    }

    [TestCase(true, true, ShipSize.Small, ShipUndockPlacement.Space)]
    [TestCase(true, false, ShipSize.Medium, ShipUndockPlacement.Space)]
    [TestCase(false, true, ShipSize.Small, ShipUndockPlacement.Starport)]
    [TestCase(false, true, ShipSize.Medium, ShipUndockPlacement.Starport)]
    [TestCase(false, true, ShipSize.Capital, ShipUndockPlacement.StarportTooSmall)]
    [TestCase(false, false, ShipSize.Small, ShipUndockPlacement.NoDock)]
    public void UndockingGoesBesideTheHostInSpaceOrIntoItsStarport(bool hostInSpace, bool hostHasDock, ShipSize visitor, ShipUndockPlacement expected)
    {
        ShipDockingRules.ChooseUndockPlacement(hostInSpace, hostHasDock, visitor).Should().Be(expected);
    }

    [Test]
    public void UndockedShipsComeOutBesideTheHostOnAlternateSides()
    {
        // Facing east (0 degrees): the first ship comes out to the right (south), the second to the left (north).
        var (x0, y0) = ShipDockingRules.GetUndockOffset(0f, 0);
        x0.Should().BeApproximately(0f, 0.001f);
        y0.Should().BeApproximately(-ShipDockingRules.UndockDistance, 0.001f);

        var (x1, y1) = ShipDockingRules.GetUndockOffset(0f, 1);
        x1.Should().BeApproximately(0f, 0.001f);
        y1.Should().BeApproximately(ShipDockingRules.UndockDistance, 0.001f);

        // Facing north (90 degrees): right is east.
        var (x2, y2) = ShipDockingRules.GetUndockOffset(90f, 0);
        x2.Should().BeApproximately(ShipDockingRules.UndockDistance, 0.001f);
        y2.Should().BeApproximately(0f, 0.001f);

        var (x3, y3) = ShipDockingRules.GetUndockOffset(0f, 2);
        Math.Sqrt(x3 * x3 + y3 * y3).Should().BeApproximately(2 * ShipDockingRules.UndockDistance, 0.001);
    }

    [Test]
    public void WhenTheHostIsDestroyedPlayersReturnToTheShipTheyCameAboardWith()
    {
        var fighter = new ShipDockVisitor { ShipId = "fighter", OwnerPlayerId = "anna", CrewPlayerIds = new List<string> { "bo", "cy" } };
        var shuttle = new ShipDockVisitor { ShipId = "shuttle", OwnerPlayerId = "dee", CrewPlayerIds = new List<string> { "dee" } };
        var visitors = new List<ShipDockVisitor> { fighter, shuttle };

        var result = ShipDockingRules.AssignReturningPlayers(new[] { "anna", "bo", "cy", "dee", "host-crew", "", "bo" }, visitors);

        result.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["anna"] = "fighter",
            ["bo"] = "fighter",
            ["cy"] = "fighter",
            ["dee"] = "shuttle",
        }, "the host's own crew belong to no docked ship and share its fate");

        // Someone who came aboard with one ship and owns another goes back to the one they came with.
        var owner = new ShipDockVisitor { ShipId = "owned", OwnerPlayerId = "bo" };
        ShipDockingRules.AssignReturningPlayers(new[] { "bo" }, new List<ShipDockVisitor> { owner, fighter })
            .Should().Equal(new Dictionary<string, string> { ["bo"] = "fighter" });

        ShipDockingRules.BelongsToVisitor("anna", fighter).Should().BeTrue("the owner belongs to their ship");
        ShipDockingRules.BelongsToVisitor("cy", fighter).Should().BeTrue();
        ShipDockingRules.BelongsToVisitor("dee", fighter).Should().BeFalse();
        ShipDockingRules.BelongsToVisitor("", fighter).Should().BeFalse();
    }

    [Test]
    public void TheDockingPilotFliesADetachedShipOutFirst()
    {
        var fighter = new ShipDockVisitor { ShipId = "fighter", OwnerPlayerId = "anna", CrewPlayerIds = new List<string> { "bo", "cy" } };

        ShipDockingRules.ChooseDetachPilot(fighter, _ => true).Should().Be("bo");
        ShipDockingRules.ChooseDetachPilot(fighter, id => id != "bo").Should().Be("cy");
        ShipDockingRules.ChooseDetachPilot(fighter, id => id == "anna").Should().Be("anna", "the owner flies it when no crew member can");
        ShipDockingRules.ChooseDetachPilot(fighter, _ => false).Should().BeNull("the ship is left beside the wreck");
    }

    [Test]
    public void StarportsTakeLargeShipsAndSmaller()
    {
        ShipDockingRules.FitsStarport(ShipSize.Small).Should().BeTrue();
        ShipDockingRules.FitsStarport(ShipSize.Medium).Should().BeTrue();
        ShipDockingRules.FitsStarport(ShipSize.Large).Should().BeTrue();
        ShipDockingRules.FitsStarport(ShipSize.Capital).Should().BeFalse();
        ShipDockingRules.FitsStarport(ShipSize.Invalid).Should().BeFalse();
    }

    [Test]
    public void TheDockPermissionIsAStarshipPermissionNextToCrewStations()
    {
        ((int)PropertyPermissionType.Dock).Should().Be(18, "permission records are keyed by the saved number");
        typeof(PropertyPermissionType).GetField(nameof(PropertyPermissionType.Dock))!
            .GetCustomAttributes(typeof(PropertyPermissionAttribute), false)
            .Cast<PropertyPermissionAttribute>()
            .Single().IsActive.Should().BeTrue();

        ReadServerSource("Service", "Property.cs").Should().Contain(
            "PropertyPermissionType.CrewStation,\n                PropertyPermissionType.Dock,\n                PropertyPermissionType.RefitShip");
    }

    [Test]
    public void TheDockedStateIsSavedOnTheVisitingShipAndLoadsEmpty()
    {
        var ship = new PlayerShip();
        ship.DockedToShipId.Should().BeEmpty();
        ship.DockedCrewPlayerIds.Should().BeEmpty();

        typeof(PlayerShip).GetProperty(nameof(PlayerShip.DockedToShipId))!
            .GetCustomAttribute<IndexedAttribute>().Should().NotBeNull("hosts look their docked ships up by it");

        var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<PlayerShip>("{\"OwnerPlayerId\":\"anna\"}")!;
        loaded.DockedToShipId.Should().BeEmpty("older records load undocked");
        loaded.DockedCrewPlayerIds.Should().BeEmpty();
    }

    [Test]
    public void TheShipDestructionDetachesDockedShipsBeforeHandlingTheHostsOccupants()
    {
        var space = ReadServerSource("Service", "Space.cs");
        var detach = space.IndexOf("var returnedToDockedShips = DetachDockedShips(creature);", StringComparison.Ordinal);
        var crew = space.IndexOf("DestroyShipCrew(creature, returnedToDockedShips);", StringComparison.Ordinal);
        var interior = space.IndexOf("if (returnedToDockedShips.Contains(player))", StringComparison.Ordinal);

        detach.Should().BePositive();
        crew.Should().BeGreaterThan(detach);
        interior.Should().BeGreaterThan(crew, "players sent back to a docked ship are not killed with the host's passengers");

        space.Should().Contain("CompleteUndockLaunch(player, launch);");
        space.Should().Contain("MarkShipCombat(attacker, target);");
        space.Should().Contain("IsDockedAboardHost(dbShip, out var hostName)");

        var stations = ReadServerSource("Service", "SpaceCrewStations.cs");
        stations.Should().Contain("if (spared.Contains(crewMember))");

        var management = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs");
        management.Should().Contain("Space.UndockAllToStarport(dbShip, \"was unregistered\");");
        management.Should().Contain("Space.HasShipDockedAboard(Player, propertyId)");

        var commands = ReadServerSource("Feature", "ChatCommandDefinition", "SpaceChatCommand.cs");
        commands.Should().Contain("_builder.Create(\"dock\")");
        commands.Should().Contain("_builder.Create(\"undock\")");
        commands.Should().Contain("Space.LeaveCrewStation(user, ");
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
