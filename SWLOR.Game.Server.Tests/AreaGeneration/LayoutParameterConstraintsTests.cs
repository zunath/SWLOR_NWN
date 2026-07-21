using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Direct unit coverage for LayoutParameterConstraints -- the normalization layer that makes every
/// combination Content Builder's Advanced Settings sliders can produce generation-safe. The broader
/// acceptance sweep lives in AdvancedKnobEnvelopeTests; this file pins the individual clamp behaviors
/// and the exact reported repro (RoomsAndCorridors 16x16, Min Room Size dragged to 10 with Max Room
/// Size left at its UI default of 7 -- "could not place enough rooms: only 1 fit").
/// </summary>
public class LayoutParameterConstraintsTests
{
    private static MacroLayoutParameters Params(DungeonLayoutStyle style = DungeonLayoutStyle.RoomsAndCorridors, int width = 20, int height = 20)
    {
        return new MacroLayoutParameters
        {
            Style = style,
            Width = width,
            Height = height,
            SolidTerrain = "Wall",
            OpenTerrain = "Floor",
        };
    }

    [Test]
    public void ClampToValid_SwapsInvertedRoomCounts()
    {
        var p = Params();
        p.MinRooms = 12;
        p.MaxRooms = 2;

        LayoutParameterConstraints.ClampToValid(p);

        p.MinRooms.Should().BeLessOrEqualTo(p.MaxRooms);
        p.MinRooms.Should().Be(2);
        p.MaxRooms.Should().Be(12);
    }

    [Test]
    public void ClampToValid_InvertedRoomCounts_NoLongerThrowsOnGenerate()
    {
        // Confirmed by probe: an un-swapped MinRooms(12) > MaxRooms(2) reaches
        // System.Random.Next(minValue, maxValue) with minValue > maxValue and throws
        // ArgumentOutOfRangeException in every style but PackedRooms (which never rolls a room count).
        // This must never reach the caller once ClampToValid runs inside MacroLayoutGenerator.Generate.
        foreach (var style in new[]
                 {
                     DungeonLayoutStyle.RoomsAndCorridors, DungeonLayoutStyle.OrganicCave,
                     DungeonLayoutStyle.Warren, DungeonLayoutStyle.Labyrinth
                 })
        {
            var floor = LayoutStyleSizeFloor.For(style);
            var p = Params(style, floor, floor);
            p.MinRooms = 12;
            p.MaxRooms = 2;

            Action act = () => MacroLayoutGenerator.Generate(p, new Random(1));

            act.Should().NotThrow<ArgumentOutOfRangeException>();
        }
    }

    [Test]
    public void ClampToValid_SwapsInvertedRoomSizes()
    {
        var p = Params();
        p.MinRoomCornerSize = 10;
        p.MaxRoomCornerSize = 3;

        LayoutParameterConstraints.ClampToValid(p);

        p.MinRoomCornerSize.Should().BeLessOrEqualTo(p.MaxRoomCornerSize);
    }

    [Test]
    public void ClampToValid_CapsOversizedRoomForStyleAndAreaSize()
    {
        var p = Params(DungeonLayoutStyle.RoomsAndCorridors, width: 11, height: 11);
        p.MinRoomCornerSize = 7;
        p.MaxRoomCornerSize = 7;

        LayoutParameterConstraints.ClampToValid(p);

        var (_, expectedMax) = LayoutParameterConstraints.RoomSizeBounds(DungeonLayoutStyle.RoomsAndCorridors, 11, 11);
        p.MaxRoomCornerSize.Should().Be(expectedMax);
        p.MinRoomCornerSize.Should().BeLessOrEqualTo(expectedMax);
    }

    [Test]
    public void ClampToValid_RaisesUndersizedAreaToStyleFloor()
    {
        var p = Params(DungeonLayoutStyle.PackedRooms, width: 4, height: 4);

        LayoutParameterConstraints.ClampToValid(p);

        var floor = LayoutStyleSizeFloor.For(DungeonLayoutStyle.PackedRooms);
        p.Width.Should().Be(floor);
        p.Height.Should().Be(floor);
    }

    [Test]
    public void ClampToValid_RaisesOrganicFillTowardSafeFloorAtSmallSizes()
    {
        var p = Params(DungeonLayoutStyle.OrganicCave, width: 12, height: 12);
        p.OpenFillTarget = 0.30; // UI slider's own floor; measured 0% single-attempt success at size 12.

        LayoutParameterConstraints.ClampToValid(p);

        p.OpenFillTarget.Should().BeGreaterOrEqualTo(LayoutParameterConstraints.MinSafeOpenFillTarget(12, 12));
    }

    [Test]
    public void ClampToValid_DoesNotLowerAnAlreadySafeOrganicFill()
    {
        var p = Params(DungeonLayoutStyle.OrganicCave, width: 32, height: 32);
        p.OpenFillTarget = 0.60;

        LayoutParameterConstraints.ClampToValid(p);

        p.OpenFillTarget.Should().Be(0.60);
    }

    [Test]
    public void ClampToValid_ClampsCorridorWidthAndEntranceExitCounts()
    {
        var p = Params();
        p.CorridorWidth = 0;
        p.EntranceCount = 9;
        p.ExitCount = -1;

        LayoutParameterConstraints.ClampToValid(p);

        p.CorridorWidth.Should().BeGreaterOrEqualTo(1);
        p.EntranceCount.Should().BeInRange(1, 3);
        p.ExitCount.Should().BeInRange(1, 3);
    }

    [Test]
    public void ClampToValid_AlreadyValidParameters_ChangesNothing()
    {
        var p = Params(DungeonLayoutStyle.RoomsAndCorridors, width: 20, height: 20);
        p.MinRooms = 4;
        p.MaxRooms = 8;
        p.MinRoomCornerSize = 3;
        p.MaxRoomCornerSize = 6;
        p.CorridorWidth = 2;
        p.EntranceCount = 1;
        p.ExitCount = 1;

        NeedsClamping_MatchesActualClampBehavior(p);
    }

    private static void NeedsClamping_MatchesActualClampBehavior(MacroLayoutParameters p)
    {
        var needsClamping = LayoutParameterConstraints.NeedsClamping(p);

        var clone = p.Clone();
        LayoutParameterConstraints.ClampToValid(clone);

        var actuallyChanged =
            clone.Width != p.Width || clone.Height != p.Height ||
            clone.MinRooms != p.MinRooms || clone.MaxRooms != p.MaxRooms ||
            clone.MinRoomCornerSize != p.MinRoomCornerSize || clone.MaxRoomCornerSize != p.MaxRoomCornerSize ||
            Math.Abs(clone.OpenFillTarget - p.OpenFillTarget) > 1e-9 ||
            clone.CorridorWidth != p.CorridorWidth ||
            clone.EntranceCount != p.EntranceCount || clone.ExitCount != p.ExitCount;

        needsClamping.Should().Be(actuallyChanged);
    }

    [Test]
    public void NeedsClamping_MatchesClampToValid_AcrossRepresentativeCases()
    {
        // Valid case: nothing to do.
        NeedsClamping_MatchesActualClampBehavior(Params(DungeonLayoutStyle.PackedRooms, 20, 20));

        // Invalid cases across each independent knob this class normalizes.
        var invertedRooms = Params();
        invertedRooms.MinRooms = 10;
        invertedRooms.MaxRooms = 4;
        NeedsClamping_MatchesActualClampBehavior(invertedRooms);

        var invertedSize = Params();
        invertedSize.MinRoomCornerSize = 9;
        invertedSize.MaxRoomCornerSize = 3;
        NeedsClamping_MatchesActualClampBehavior(invertedSize);

        var tooSmall = Params(DungeonLayoutStyle.Warren, 4, 4);
        NeedsClamping_MatchesActualClampBehavior(tooSmall);

        var lowFill = Params(DungeonLayoutStyle.OrganicCave, 12, 12);
        lowFill.OpenFillTarget = 0.30;
        NeedsClamping_MatchesActualClampBehavior(lowFill);

        var zeroCorridor = Params();
        zeroCorridor.CorridorWidth = 0;
        NeedsClamping_MatchesActualClampBehavior(zeroCorridor);

        var badEntrances = Params();
        badEntrances.EntranceCount = 5;
        NeedsClamping_MatchesActualClampBehavior(badEntrances);
    }

    [Test]
    public void RoomSizeBounds_MatchesWhatClampToValidActuallyEnforces()
    {
        foreach (DungeonLayoutStyle style in Enum.GetValues(typeof(DungeonLayoutStyle)))
        {
            foreach (var size in new[] { 9, 11, 16, 24, 32 })
            {
                var (min, max) = LayoutParameterConstraints.RoomSizeBounds(style, size, size);
                min.Should().Be(2, $"{style} at {size}x{size}");

                // OrganicCave's room-size knobs are structurally unused (rooms are sampled from the
                // smoothed cave, not rectangles), so RoomSizeBounds reports no ceiling -- there is
                // nothing for ClampToValid to cap, and asserting Max+5 would overflow int.MaxValue.
                if (max == int.MaxValue) continue;

                var p = Params(style, size, size);
                p.MinRoomCornerSize = max + 5;
                p.MaxRoomCornerSize = max + 5;

                LayoutParameterConstraints.ClampToValid(p);

                p.MaxRoomCornerSize.Should().Be(max, $"{style} at {size}x{size}");
            }
        }
    }

    [Test]
    public void RoomSizeBounds_WarrenAndLabyrinth_HardCappedAtFiveAndFour()
    {
        LayoutParameterConstraints.RoomSizeBounds(DungeonLayoutStyle.Warren, 32, 32).Max.Should().Be(5);
        LayoutParameterConstraints.RoomSizeBounds(DungeonLayoutStyle.Labyrinth, 32, 32).Max.Should().Be(4);
    }

    [Test]
    public void RoomSizeBounds_OrganicCave_IsUnbounded()
    {
        LayoutParameterConstraints.RoomSizeBounds(DungeonLayoutStyle.OrganicCave, 12, 12).Max.Should().Be(int.MaxValue);
    }

    [Test]
    public void MinSafeOpenFillTarget_DecreasesAsAreaGrows()
    {
        LayoutParameterConstraints.MinSafeOpenFillTarget(12, 12).Should().Be(0.60);
        LayoutParameterConstraints.MinSafeOpenFillTarget(16, 16).Should().Be(0.50);
        LayoutParameterConstraints.MinSafeOpenFillTarget(24, 24).Should().Be(0.40);
        LayoutParameterConstraints.MinSafeOpenFillTarget(32, 32).Should().Be(0.35);
    }

    [Test]
    public void ReportedRepro_RoomsAndCorridors16x16_MinRoomSize10_GeneratesReliably()
    {
        // Exact reported failure: Style=RoomsAndCorridors, 16x16, Min Room Size dragged to 10 with Max
        // Room Size left at the UI's default of 7 -- "could not place enough rooms: only 1 fit".
        // MacroLayoutGenerator.Generate now clamps through LayoutParameterConstraints internally, so
        // this must succeed across many seeds under the standard 6-attempt retry without the caller
        // doing anything differently.
        var tileset = LoadTileset("vmr01", "sw_t_alienruin");

        for (var baseSeed = 0; baseSeed < 20; baseSeed++)
        {
            var succeeded = false;
            string lastReason = null;

            for (var attempt = 0; attempt < 6 && !succeeded; attempt++)
            {
                var p = new MacroLayoutParameters
                {
                    Style = DungeonLayoutStyle.RoomsAndCorridors,
                    Width = 16,
                    Height = 16,
                    SolidTerrain = tileset.DefaultTerrain,
                    OpenTerrain = "Plaza",
                    MinRoomCornerSize = 10,
                    MaxRoomCornerSize = 7,
                };

                var rng = new Random(baseSeed * 13 + attempt);
                try
                {
                    var macro = MacroLayoutGenerator.Generate(p, rng, tileset);
                    succeeded = TileResolver.TryResolve(tileset, macro, rng, out _, out lastReason);
                }
                catch (InvalidOperationException ex)
                {
                    lastReason = ex.Message;
                }
            }

            succeeded.Should().BeTrue($"seed {baseSeed}: {lastReason}");
        }
    }

    private static TilesetModel LoadTileset(string tilesetResref, string hakDirectory)
    {
        var root = FindRepositoryRoot();
        var contents = File.ReadAllText(Path.Combine(root.FullName, "SWLOR_Haks", hakDirectory, $"{tilesetResref}.set"));
        return TilesetSetParser.Parse(tilesetResref, contents);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SWLOR.Game.Server.sln")))
                return current;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (SWLOR.Game.Server.sln).");
    }
}
