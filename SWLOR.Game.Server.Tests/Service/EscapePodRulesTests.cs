using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class EscapePodRulesTests
{
    [Test]
    public void Manifest_ListsPilotFirstThenCrewThenPassengers()
    {
        var manifest = EscapePodRules.BuildManifest(
            "pilot",
            new[] { "gunner", "engineer", "watcher" },
            new[] { "passenger1", "passenger2" });

        manifest.Should().Equal("pilot", "gunner", "engineer", "watcher", "passenger1", "passenger2");
    }

    [Test]
    public void Manifest_ListsEachPlayerOnce()
    {
        var manifest = EscapePodRules.BuildManifest(
            "pilot",
            new[] { "gunner", "pilot", "gunner" },
            new[] { "gunner", "passenger" });

        manifest.Should().Equal("pilot", "gunner", "passenger");
    }

    [Test]
    public void Manifest_WithNoOneElseAboard_IsJustThePilot()
    {
        EscapePodRules.BuildManifest<string>("pilot", null, null).Should().Equal("pilot");
        EscapePodRules.BuildManifest("pilot", Array.Empty<string>(), Array.Empty<string>()).Should().Equal("pilot");
    }

    [Test]
    public void Manifest_HoldsOnlyThePlayersItIsGiven_SoNpcsTakeNoPart()
    {
        // Callers pass player objects only; an NPC is never part of a manifest.
        var manifest = EscapePodRules.BuildManifest<uint>(10, new uint[] { 11 }, new uint[] { 12 });

        manifest.Should().Equal(10u, 11u, 12u);
    }

    [TestCase(true, true, EscapePodDestinationType.Dock)]
    [TestCase(true, false, EscapePodDestinationType.Dock)]
    [TestCase(false, true, EscapePodDestinationType.SafeDock)]
    [TestCase(false, false, EscapePodDestinationType.None)]
    public void Destination_PrefersTheDockTheShipIsTowedTo(bool dockReachable, bool safeReachable, EscapePodDestinationType expected)
    {
        EscapePodRules.ChooseDestination(dockReachable, safeReachable).Should().Be(expected);
    }

    [TestCase(100, 50)]
    [TestCase(101, 50)]
    [TestCase(2, 1)]
    [TestCase(1, 1)]
    [TestCase(0, 1)]
    public void EjectionHitPoints_AreHalfOfMaximum_AtLeastOne(int max, int expected)
    {
        EscapePodRules.GetEjectionHitPoints(max).Should().Be(expected);
    }
}
