using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Acceptance gate for the Content Builder "Advanced Settings" hardening effort: every layout style
/// must generate and resolve successfully (under the standard 6-attempt retry) for every corner of
/// its CONSTRAINED knob envelope -- i.e. after <see cref="LayoutParameterConstraints.ClampToValid"/>
/// normalizes whatever the UI's sliders could have produced. This is corner sampling, not a full
/// cartesian sweep: a full cross-product of size x room-size x room-count x corridor-width x
/// loop-factor x entrance/exit would be thousands of generations per style. Instead each size tests
/// the all-low extreme, the all-high extreme, and two mixed corners that pair a high value on one
/// knob with a low value on another, which exercises every knob's extreme in combination with the
/// rest of the envelope at least once while keeping total runtime well under a minute.
/// </summary>
public class AdvancedKnobEnvelopeTests
{
    private static readonly Dictionary<DungeonLayoutStyle, (string TilesetResref, string HakDirectory, string OpenTerrain)> StyleTilesets = new()
    {
        [DungeonLayoutStyle.RoomsAndCorridors] = ("vmr01", "sw_t_alienruin", "Plaza"),
        [DungeonLayoutStyle.PackedRooms] = ("zsf01", "sw_t_scifibase", "floor"),
        [DungeonLayoutStyle.OrganicCave] = ("tdt01", "sw_t_minecave", ""),
        [DungeonLayoutStyle.Warren] = ("tds01", "sw_t_sewer", ""),
        [DungeonLayoutStyle.Labyrinth] = ("tdt01", "sw_t_minecave", ""),
    };

    private static readonly DungeonLayoutStyle[] AllStyles =
    {
        DungeonLayoutStyle.RoomsAndCorridors,
        DungeonLayoutStyle.PackedRooms,
        DungeonLayoutStyle.OrganicCave,
        DungeonLayoutStyle.Warren,
        DungeonLayoutStyle.Labyrinth,
    };

    private const int SeedsPerCorner = 5;

    [Test]
    public void EveryStyle_ConstrainedEnvelopeCorners_GenerateReliably()
    {
        var failures = new List<string>();

        foreach (var style in AllStyles)
        {
            var (resref, hakDir, openTerrain) = StyleTilesets[style];
            var model = LoadTileset(resref, hakDir);
            var floor = LayoutStyleSizeFloor.For(style);

            foreach (var size in DistinctSizes(floor))
            {
                var (_, maxRoomSize) = LayoutParameterConstraints.RoomSizeBounds(style, size, size);
                var minRoomSize = Math.Min(2, maxRoomSize);
                var minFill = LayoutParameterConstraints.MinSafeOpenFillTarget(size, size);
                const double maxFill = 0.60; // UI slider ceiling; probe confirmed safe at every size.

                foreach (var corner in BuildCorners(style, minRoomSize, maxRoomSize, minFill, maxFill))
                {
                    RunCorner(style, model, openTerrain, size, corner, failures);
                }
            }
        }

        failures.Should().BeEmpty();
    }

    private static IEnumerable<int> DistinctSizes(int floor)
    {
        var seen = new HashSet<int>();
        foreach (var size in new[] { floor, 16, 24, 32 })
        {
            if (seen.Add(size))
                yield return size;
        }
    }

    private record struct Corner(
        string Name, int MinRooms, int MaxRooms, int MinRoomSize, int MaxRoomSize,
        int CorridorWidth, double LoopFactor, double OpenFillTarget, int Entrances, int Exits);

    /// <summary>
    /// All-low, all-high, and two mixed corners of the constrained envelope. Room counts use 4/12 (the
    /// UI's Min Rooms slider floor/ceiling-ish default spread) rather than the sliders' own absolute
    /// extremes (2 and 16) since probe Part 2/6/9 already covers the full 2-16 spread at the room-size
    /// boundary and confirms room count alone never drives a failure once room size is bounded.
    /// </summary>
    private static IEnumerable<Corner> BuildCorners(DungeonLayoutStyle style, int minRoomSize, int maxRoomSize, double minFill, double maxFill)
    {
        yield return new Corner("all-low", 4, 4, minRoomSize, minRoomSize, 1, 0.0, minFill, 1, 1);
        yield return new Corner("all-high", 12, 12, maxRoomSize, maxRoomSize, 3, 1.0, maxFill, 3, 3);
        yield return new Corner("mixed-a", 4, 4, maxRoomSize, maxRoomSize, 3, 0.0, maxFill, 3, 1);
        yield return new Corner("mixed-b", 12, 12, minRoomSize, minRoomSize, 1, 1.0, minFill, 1, 3);
    }

    private static void RunCorner(
        DungeonLayoutStyle style, TilesetModel model, string openTerrain, int size, Corner corner, List<string> failures)
    {
        for (var seed = 0; seed < SeedsPerCorner; seed++)
        {
            var succeeded = false;
            string lastReason = null;

            // Mirror the production retry: up to 6 attempts with derived seeds (see
            // LayoutSizeFloorTests, GenerationEngine).
            for (var attempt = 0; attempt < 6 && !succeeded; attempt++)
            {
                var parameters = new MacroLayoutParameters
                {
                    Style = style,
                    Width = size,
                    Height = size,
                    SolidTerrain = model.DefaultTerrain,
                    OpenTerrain = openTerrain.Length == 0 ? model.FloorTerrain : openTerrain,
                    MinRooms = corner.MinRooms,
                    MaxRooms = corner.MaxRooms,
                    MinRoomCornerSize = corner.MinRoomSize,
                    MaxRoomCornerSize = corner.MaxRoomSize,
                    CorridorWidth = corner.CorridorWidth,
                    LoopFactor = corner.LoopFactor,
                    OpenFillTarget = corner.OpenFillTarget,
                    EntranceCount = corner.Entrances,
                    ExitCount = corner.Exits,
                };

                LayoutParameterConstraints.ClampToValid(parameters);

                var rng = new Random((style.GetHashCode() * 104729) + (size * 7919) + (seed * 97) + attempt);
                try
                {
                    var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
                    succeeded = TileResolver.TryResolve(model, macro, rng, out _, out lastReason);
                }
                catch (InvalidOperationException ex)
                {
                    lastReason = ex.Message;
                }
            }

            if (!succeeded)
                failures.Add($"{style}/{corner.Name} at {size}x{size}, seed {seed}: {lastReason}");
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
