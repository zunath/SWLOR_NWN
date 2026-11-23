using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The astromech socket (Readmes/AstromechPlayableSpeciesPlan.md, Ship role): a seat on the Small and Medium combat
/// hulls that only an astromech takes. It owns no slots, so a socket hull flies the same with the socket empty; an
/// astromech in it routes power and works the Pilot's repair and capacitor modules on Engineering, never a weapon.
/// </summary>
public class AstromechSocketTests
{
    [Test]
    public void TheSmallAndMediumCombatHulls_CarryASocket_AndNoOtherHullDoes()
    {
        foreach (var role in Enum.GetValues<ShipRole>())
        {
            var combat = role is ShipRole.Fighter or ShipRole.Interceptor or ShipRole.Bomber or ShipRole.Gunship;
            ShipCrewSeats.HasAstromechSocket(ShipSize.Small, role).Should().Be(combat, $"Small {role}");
            ShipCrewSeats.HasAstromechSocket(ShipSize.Medium, role).Should().Be(combat, $"Medium {role}");
            ShipCrewSeats.HasAstromechSocket(ShipSize.Large, role).Should().BeFalse($"Large {role}");
            ShipCrewSeats.HasAstromechSocket(ShipSize.Capital, role).Should().BeFalse($"Capital {role}");
        }

        var socketHulls = RevampShipCatalog.Hulls.Where(hull => ShipCrewSeats.HasAstromechSocket(hull.Size, hull.Role)).ToList();
        socketHulls.Should().NotBeEmpty();
        socketHulls.Should().OnlyContain(hull => hull.Size == ShipSize.Small || hull.Size == ShipSize.Medium);
        socketHulls.Select(hull => hull.Role).Distinct().Should().BeEquivalentTo(
            new[] { ShipRole.Fighter, ShipRole.Interceptor, ShipRole.Bomber, ShipRole.Gunship });

        // A socket hull has no station slots, so nothing on it goes dark while the socket is empty.
        socketHulls.Should().OnlyContain(hull => hull.StationHigh == 0 && hull.StationMid == 0 && hull.StationLow == 0);
    }

    [Test]
    public void TheSocket_IsASeatPlayersTake_ThatDroidsAreNeverPostedAt()
    {
        ShipCrewSeats.GetStationSeats(ShipSize.Small, true).Should().Equal(ShipCrewSeat.AstromechSocket);
        ShipCrewSeats.GetStationSeats(ShipSize.Medium, false).Should().BeEmpty();
        ShipCrewSeats.GetStationSeats(ShipSize.Large, false).Should().Equal(ShipCrewSeat.Gunner, ShipCrewSeat.Engineer);

        ShipCrewSeats.HasSeat(ShipSize.Small, ShipCrewSeat.AstromechSocket, true).Should().BeTrue();
        ShipCrewSeats.HasSeat(ShipSize.Small, ShipCrewSeat.AstromechSocket, false).Should().BeFalse();
        ShipCrewSeats.HasSeat(ShipSize.Small, ShipCrewSeat.Engineer, true).Should().BeFalse();
        ShipCrewSeats.HasSeat(ShipSize.Large, ShipCrewSeat.Engineer, false).Should().BeTrue();

        // Droid posts come from the size-only list, which never holds the socket, and a droid has no effect there.
        foreach (var size in Enum.GetValues<ShipSize>())
            ShipCrewSeats.GetStationSeats(size).Should().NotContain(ShipCrewSeat.AstromechSocket);
        DroidCrewRules.BestEffectiveness(ShipCrewSeat.AstromechSocket).Should().Be(0);

        var droidCrew = ReadServerSource("Service", "SpaceDroidCrew.cs");
        droidCrew.Should().Contain("var posts = ShipCrewSeats.GetStationSeats(status.Size).ToList();");
    }

    [Test]
    public void TheSocket_ChecksEngineering_AndHasItsOwnName()
    {
        ShipCrewSeats.GetSeatSkill(ShipCrewSeat.AstromechSocket).Should().Be(SkillType.Engineering);
        ShipCrewSeats.GetSeatName(ShipCrewSeat.AstromechSocket).Should().Be("Astromech Socket");
        ShipCrewSeats.GetPostName(ShipCrewSeat.AstromechSocket).Should().Be("the astromech socket");
        ShipCrewSeats.GetPostName(ShipCrewSeat.Engineer).Should().Be("the Engineer station");
        ShipCrewSeats.GetPostName(ShipCrewSeat.DroidBay).Should().Be("the droid bay");
    }

    [Test]
    public void TheSocket_WorksThePilotsRepairAndCapacitorModules_AndNothingElse()
    {
        foreach (var type in Enum.GetValues<ShipModuleType>())
        {
            var socketModule = type is ShipModuleType.HullRepairer or ShipModuleType.ShieldRepairer
                or ShipModuleType.RepairFieldGenerator or ShipModuleType.HypermatterInjector;
            ShipCrewSeats.IsSocketModule(type).Should().Be(socketModule, type.ToString());

            // The socket reaches the Pilot's slots only, and only for those modules.
            ShipCrewSeats.SeatWorksSlot(ShipCrewSeat.AstromechSocket, ShipCrewSeat.Pilot, type).Should().Be(socketModule);
            ShipCrewSeats.SeatWorksSlot(ShipCrewSeat.AstromechSocket, ShipCrewSeat.Gunner, type).Should().BeFalse();
            ShipCrewSeats.SeatWorksSlot(ShipCrewSeat.AstromechSocket, ShipCrewSeat.Engineer, type).Should().BeFalse();

            // Every other seat works its own slots, whatever they hold, exactly as before.
            ShipCrewSeats.SeatWorksSlot(ShipCrewSeat.Engineer, ShipCrewSeat.Engineer, type).Should().BeTrue();
            ShipCrewSeats.SeatWorksSlot(ShipCrewSeat.Gunner, ShipCrewSeat.Engineer, type).Should().BeFalse();
            ShipCrewSeats.SeatWorksSlot(ShipCrewSeat.Engineer, ShipCrewSeat.Pilot, type).Should().BeFalse();
        }

        // Every socket module is one whose output already reads Engineering, so the astromech's rank is what counts.
        foreach (var type in Enum.GetValues<ShipModuleType>().Where(ShipCrewSeats.IsSocketModule))
            ShipSeatSkillRules.GetBonusSkill(type).Should().Be(SkillType.Engineering, type.ToString());
    }

    [Test]
    public void ARunningSocketModule_CyclesAsTheAstromech_OnlyWhileItIsPluggedInAtTheModulesRank()
    {
        const ShipModuleType Repairer = ShipModuleType.ShieldRepairer;

        ShipCrewSeats.GetCycleSeat(ShipCrewSeat.Pilot, Repairer, true, true).Should().Be(ShipCrewSeat.AstromechSocket);

        // Below the module's rank the astromech cannot switch it on, so its cycles and their credit stay the Pilot's.
        ShipCrewSeats.GetCycleSeat(ShipCrewSeat.Pilot, Repairer, true, false).Should().Be(ShipCrewSeat.Pilot);
        ShipCrewSeats.GetCycleSeat(ShipCrewSeat.Pilot, Repairer, false, false).Should().Be(ShipCrewSeat.Pilot);
        ShipCrewSeats.GetCycleSeat(ShipCrewSeat.Pilot, Repairer, false, true).Should().Be(ShipCrewSeat.Pilot);

        // Weapons and other seats' slots are never the socket's.
        foreach (var type in Enum.GetValues<ShipModuleType>().Where(type => !ShipCrewSeats.IsSocketModule(type)))
            ShipCrewSeats.GetCycleSeat(ShipCrewSeat.Pilot, type, true, true).Should().Be(ShipCrewSeat.Pilot, type.ToString());
        ShipCrewSeats.GetCycleSeat(ShipCrewSeat.Engineer, Repairer, true, true).Should().Be(ShipCrewSeat.Engineer);
        ShipCrewSeats.GetCycleSeat(ShipCrewSeat.Gunner, Repairer, true, true).Should().Be(ShipCrewSeat.Gunner);
    }

    [Test]
    public void TheSocket_RoutesPower_LikeTheEngineerStation()
    {
        foreach (var seat in Enum.GetValues<ShipCrewSeat>())
            ShipCrewSeats.RoutesPower(seat).Should().Be(seat is ShipCrewSeat.Engineer or ShipCrewSeat.AstromechSocket, seat.ToString());

        var routing = ReadServerSource("Service", "SpacePowerRouting.cs");
        routing.Should().Contain("!ShipCrewSeats.RoutesPower(seat)");

        var stations = ReadServerSource("Service", "SpaceCrewStations.cs");
        stations.Should().Contain("if (ShipCrewSeats.RoutesPower(seat))");
        stations.Should().Contain("!GetIsObjectValid(SpaceCrew.GetSeatOccupant(pilot, ShipCrewSeat.AstromechSocket));");
    }

    [Test]
    public void OnlyAnAstromech_TakesTheSocket_AndItsStandInStaysPluggedIn()
    {
        var stations = ReadServerSource("Service", "SpaceCrewStations.cs");
        stations.Should().Contain("seat == ShipCrewSeat.AstromechSocket && Race.GetBodyFrame(player) != BodyFrame.Astromech");
        stations.Should().Contain("return \"Only an astromech can plug into the astromech socket.\";");
        stations.Should().Contain("ShipCrewSeats.HasSeat(status.Size, seat, HasAstromechSocket(status))");
        stations.Should().Contain("ActionPlayAnimation(AstromechAnimations.Station, 1f, StandInPoseSeconds)");

        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("ShipCrewSeats.SeatWorksSlot(crewSeat, slotSeat, _shipModules[shipModule.ItemTag].Type)");
        space.Should().Contain("ActionPlayAnimation(AstromechAnimations.Station, 1f, StandInPoseSeconds)");

        // A socket module that is switched on runs its cycles as the astromech while it is plugged in.
        var toggles = ReadServerSource("Service", "SpaceToggles.cs");
        toggles.Should().Contain("seat = ShipCrewSeats.GetCycleSeat(seat, detail.Type, socketCrewed,");
        toggles.Should().Contain("string.IsNullOrEmpty(GetStationRankError(socketCrew, ShipCrewSeat.AstromechSocket, module))");

        // The pose is one the chassis has a clip for.
        AstromechAnimations.CanPlay(AstromechAnimations.Station).Should().BeTrue();

        // An astromech at a station is hidden again once it arrives beside the ship; its client can drop the
        // effect applied before the jump, which left the droid drawn on top of the ship.
        ReadServerSource("Service", "SpaceCrewStations.cs").Should()
            .Contain("if (hasPresence && Astromech.IsAstromech(crewMember))");
        AstromechAnimations.Station.Should().NotBe(Animation.LoopingSitChair);

        var dialog = ReadServerSource("Feature", "DialogDefinition", "CrewStationDialog.cs");
        dialog.Should().Contain("var stations = Space.GetStationSeats(status);");
        dialog.Should().Contain("\"Plug into the astromech socket\"");
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
