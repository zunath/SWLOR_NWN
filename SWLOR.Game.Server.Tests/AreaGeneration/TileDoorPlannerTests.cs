using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Validates TileDoorPlanner's opportunistic door-style transition substitution against the four
/// real tilesets probed for door-slot coverage: tdt01/tds01/vmr01 have usable flat, ungrouped
/// single-Doorway-edge tile pairs; zsf01 has none, so every transition there must fall back to
/// Placeable. Also pins the door world-transform formula against hand-built module doors.
/// </summary>
public class TileDoorPlannerTests
{
    private static readonly (string Resref, string HakFolder, string SetFile)[] DoorCapableTilesets =
    {
        ("tdt01", "sw_t_minecave", "tdt01.set"),
        ("tds01", "sw_t_sewer", "tds01.set"),
        ("vmr01", "sw_t_alienruin", "vmr01.set"),
    };

    private static readonly DungeonLayoutStyle[] AllStyles =
    {
        DungeonLayoutStyle.RoomsAndCorridors,
        DungeonLayoutStyle.OrganicCave,
        DungeonLayoutStyle.Warren,
        DungeonLayoutStyle.PackedRooms,
        DungeonLayoutStyle.Labyrinth
    };

    private static TilesetModel LoadTileset(string resref, string hakFolder, string setFile)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root.FullName, "SWLOR_Haks", hakFolder, setFile);
        return TilesetSetParser.Parse(resref, File.ReadAllText(path));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static MacroLayoutParameters Parameters(DungeonLayoutStyle style, TilesetModel tileset, int entrances, int exits, bool doorTransitions)
    {
        return new MacroLayoutParameters
        {
            Width = 20,
            Height = 20,
            SolidTerrain = tileset.DefaultTerrain,
            OpenTerrain = tileset.FloorTerrain,
            Style = style,
            MinRooms = 4,
            MaxRooms = 6,
            EntranceCount = entrances,
            ExitCount = exits,
            DoorTransitions = doorTransitions
        };
    }

    [Test]
    public void DoorCapableTilesets_EveryTransitionIsValidDoorOrPlaceable()
    {
        var doorCount = 0;
        var placeableCount = 0;

        foreach (var (resref, hakFolder, setFile) in DoorCapableTilesets)
        {
            var tileset = LoadTileset(resref, hakFolder, setFile);

            foreach (var style in AllStyles)
            {
                for (var seed = 1; seed <= 8; seed++)
                {
                    foreach (var (entrances, exits) in new[] { (1, 1), (2, 2), (3, 2) })
                    {
                        var parameters = Parameters(style, tileset, entrances, exits, doorTransitions: true);
                        var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
                        layout.Seed = seed;

                        var resolveOk = TileResolver.TryResolve(tileset, layout, new Random(seed * 31 + 7), out var resolved, out var failure);
                        resolveOk.Should().BeTrue($"{resref} {style} seed {seed}: {failure}");

                        var context = $"{resref} {style} seed {seed}";
                        var claimedCells = new HashSet<(int X, int Y)>();

                        foreach (var transition in resolved.Transitions)
                        {
                            if (transition.Style == TransitionStyle.Placeable)
                            {
                                placeableCount++;
                                continue;
                            }

                            doorCount++;
                            transition.Style.Should().Be(TransitionStyle.Door);

                            // No duplicate cell claims across transitions.
                            claimedCells.Should().NotContain(transition.Tile, $"{context}: room-edge cell reused");
                            claimedCells.Should().NotContain(transition.DoorCell, $"{context}: solid cell reused");
                            claimedCells.Add(transition.Tile);
                            claimedCells.Add(transition.DoorCell);

                            // (a) the room-edge tile actually has a door slot.
                            var roomEdgeResolved = resolved.GetTile(transition.Tile.X, transition.Tile.Y);
                            var roomEdgeRecord = tileset.Tiles[roomEdgeResolved.TileId];
                            roomEdgeRecord.Doors.Should().NotBeEmpty($"{context}: room-edge tile must have a door slot");

                            // (b) substituted tiles' corners still match the corner grid labels.
                            AssertCornersMatch(tileset, layout, resolved, transition.Tile, context);
                            AssertCornersMatch(tileset, layout, resolved, transition.DoorCell, context);

                            // (c) facing edges both Doorway.
                            var dx = transition.DoorCell.X - transition.Tile.X;
                            var dy = transition.DoorCell.Y - transition.Tile.Y;
                            var (edgeFromCell, edgeBack) = DirectionToEdgeSlots(dx, dy);

                            var edgeFromCellValue = roomEdgeRecord.GetEdgeAt(roomEdgeResolved.Orientation, edgeFromCell);
                            edgeFromCellValue.Should().BeEquivalentTo("Doorway", $"{context}: room-edge tile must face Doorway toward the solid cell");

                            var solidResolved = resolved.GetTile(transition.DoorCell.X, transition.DoorCell.Y);
                            var solidRecord = tileset.Tiles[solidResolved.TileId];
                            var edgeBackValue = solidRecord.GetEdgeAt(solidResolved.Orientation, edgeBack);
                            edgeBackValue.Should().BeEquivalentTo("Doorway", $"{context}: solid-side tile must face Doorway back toward the room");

                            // Solid-side tile must actually be all-solid-cornered.
                            solidRecord.GetCornerAt(solidResolved.Orientation, CornerSlot.TopLeft).Should().BeEquivalentTo(tileset.DefaultTerrain);
                            solidRecord.GetCornerAt(solidResolved.Orientation, CornerSlot.TopRight).Should().BeEquivalentTo(tileset.DefaultTerrain);
                            solidRecord.GetCornerAt(solidResolved.Orientation, CornerSlot.BottomRight).Should().BeEquivalentTo(tileset.DefaultTerrain);
                            solidRecord.GetCornerAt(solidResolved.Orientation, CornerSlot.BottomLeft).Should().BeEquivalentTo(tileset.DefaultTerrain);
                        }
                    }
                }
            }
        }

        // Sanity: door-capable tilesets should actually produce some doors across this sweep.
        doorCount.Should().BeGreaterThan(0, "door-capable tilesets should substitute at least some doors across this sweep");
    }

    [Test]
    public void Zsf01_AllTransitionsStayPlaceable_GenerationStillSucceeds()
    {
        var tileset = LoadTileset("zsf01", "sw_t_scifibase", "zsf01.set");

        foreach (var style in AllStyles)
        {
            for (var seed = 1; seed <= 6; seed++)
            {
                var parameters = Parameters(style, tileset, entrances: 2, exits: 2, doorTransitions: true);
                var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
                layout.Seed = seed;

                var resolveOk = TileResolver.TryResolve(tileset, layout, new Random(seed), out var resolved, out var failure);
                resolveOk.Should().BeTrue($"zsf01 {style} seed {seed}: {failure}");

                resolved.Transitions.Should().OnlyContain(t => t.Style == TransitionStyle.Placeable,
                    $"zsf01 {style} seed {seed}: has zero usable flat ungrouped door tiles, so every transition must fall back to Placeable");
            }
        }
    }

    [Test]
    public void SameSeed_ProducesIdenticalTransitionData()
    {
        var tileset = LoadTileset("tds01", "sw_t_sewer", "tds01.set");
        var parameters = Parameters(DungeonLayoutStyle.RoomsAndCorridors, tileset, entrances: 3, exits: 2, doorTransitions: true);

        ResolvedLayout Resolve()
        {
            var layout = MacroLayoutGenerator.Generate(parameters.Clone(), new Random(2024));
            layout.Seed = 2024;
            TileResolver.TryResolve(tileset, layout, new Random(2024 * 31 + 7), out var resolved, out _).Should().BeTrue();
            return resolved;
        }

        var a = Resolve();
        var b = Resolve();

        a.Transitions.Should().HaveCount(b.Transitions.Count);
        for (var i = 0; i < a.Transitions.Count; i++)
        {
            var ta = a.Transitions[i];
            var tb = b.Transitions[i];
            ta.Style.Should().Be(tb.Style, $"transition {i}");
            ta.Tile.Should().Be(tb.Tile, $"transition {i}");
            ta.DoorCell.Should().Be(tb.DoorCell, $"transition {i}");
            ta.DoorX.Should().Be(tb.DoorX, $"transition {i}");
            ta.DoorY.Should().Be(tb.DoorY, $"transition {i}");
            ta.DoorZ.Should().Be(tb.DoorZ, $"transition {i}");
            ta.DoorOrientation.Should().Be(tb.DoorOrientation, $"transition {i}");
        }

        for (var i = 0; i < a.Tiles.Length; i++)
        {
            a.Tiles[i].TileId.Should().Be(b.Tiles[i].TileId, $"tile index {i}");
            a.Tiles[i].Orientation.Should().Be(b.Tiles[i].Orientation, $"tile index {i}");
        }
    }

    [Test]
    public void DoorTransitionsDisabled_AllPlaceable_NoDoorSlotTilesInGrid()
    {
        var tileset = LoadTileset("tds01", "sw_t_sewer", "tds01.set");

        foreach (var style in AllStyles)
        {
            for (var seed = 1; seed <= 5; seed++)
            {
                var parameters = Parameters(style, tileset, entrances: 3, exits: 3, doorTransitions: false);
                var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
                layout.Seed = seed;

                TileResolver.TryResolve(tileset, layout, new Random(seed), out var resolved, out var failure)
                    .Should().BeTrue($"{style} seed {seed}: {failure}");

                resolved.Transitions.Should().OnlyContain(t => t.Style == TransitionStyle.Placeable,
                    $"{style} seed {seed}: DoorTransitions=false must leave every transition Placeable");

                foreach (var tile in resolved.Tiles)
                {
                    tileset.Tiles[tile.TileId].Doors.Should().BeEmpty(
                        $"{style} seed {seed}: DoorTransitions=false must never select a door-slot tile");
                }
            }
        }
    }

    /// <summary>
    /// Pins the door world-transform formula (position and bearing) against hand-built module doors
    /// sitting on unambiguous (single-door-slot) tiles, one sample per orientation value (0-3),
    /// extracted from Module/are/*.are.json + Module/git/*.git.json via a throwaway scan harness.
    /// World position = cell center (cellX*10+5, cellY*10+5) + the tile-local door (x, y), rotated
    /// orientation*90 degrees counterclockwise (exact 90-degree-multiple rotation: swap/negate, no
    /// trig). World bearing (degrees) = local door Orientation + orientation*90, normalized to
    /// (-180, 180]. Real hand-placed doors sometimes carry a designer-applied +/-180 flip on top of
    /// this (a door is symmetric along its hinge axis so a "backwards" placement still fits the same
    /// slot) — samples below are the ones matching the raw, un-flipped formula exactly, which is what
    /// TileDoorPlanner emits for newly-generated doors.
    /// </summary>
    // Samples extracted from hand-built module areas (Module/are/*.are.json + Module/git/*.git.json)
    // via a throwaway scan harness: for each, the door sits on a tile with exactly one door slot (so
    // slot->door mapping is unambiguous), and the predicted position/bearing match the actual .git
    // door X/Y/Bearing (bearing converted git-radians -> degrees) essentially exactly.
    [TestCase("dan_battlemon", 6, 0, 0, 0.0f, -0.81f, -180f, 65f, 4.190000057220459f, 180f)]
    [TestCase("dan_interiors", 5, 4, 1, 0.0f, 2.0f, 0f, 53f, 45f, 90f)]
    [TestCase("ar_scor_korrcan", 4, 3, 2, 0.0f, 5.0f, 0f, 45f, 30f, -180f)]
    [TestCase("ar_scor_kvalinte", 10, 14, 3, 0.0f, -0.2f, 0f, 104.8000030517578f, 145f, -89.99998201391065f)]
    public void DoorWorldTransform_MatchesHandBuiltModuleDoors(
        string area, int cellX, int cellY, int tileOrientation,
        float localDoorX, float localDoorY, float localDoorOrientation,
        float expectedWorldX, float expectedWorldY, float expectedBearingDeg)
    {
        _ = area; // documents provenance only

        var (worldX, worldY) = RotateAndTranslate(cellX, cellY, localDoorX, localDoorY, tileOrientation);
        worldX.Should().BeApproximately(expectedWorldX, 0.05f);
        worldY.Should().BeApproximately(expectedWorldY, 0.05f);

        // Compare via normalized angle difference: +180 and -180 are the same physical bearing, and
        // real hand-placed doors' Bearing (radians -> degrees) round-trips with tiny float noise.
        var predictedBearing = NormalizeDegrees(localDoorOrientation + tileOrientation * 90f);
        var diff = NormalizeDegrees(predictedBearing - expectedBearingDeg);
        Math.Abs(diff).Should().BeLessThan(0.1f);
    }

    private static (float X, float Y) RotateAndTranslate(int cellX, int cellY, float localX, float localY, int orientation)
    {
        var (rx, ry) = ((orientation % 4 + 4) % 4) switch
        {
            0 => (localX, localY),
            1 => (-localY, localX),
            2 => (-localX, -localY),
            3 => (localY, -localX),
            _ => (localX, localY)
        };

        return (cellX * 10f + 5f + rx, cellY * 10f + 5f + ry);
    }

    private static float NormalizeDegrees(float degrees)
    {
        var d = degrees % 360f;
        if (d > 180f) d -= 360f;
        if (d <= -180f) d += 360f;
        return d;
    }

    private static void AssertCornersMatch(TilesetModel tileset, MacroLayout layout, ResolvedLayout resolved, (int X, int Y) cell, string context)
    {
        var expectedTl = layout.Corners.Labels[cell.X, cell.Y + 1];
        var expectedTr = layout.Corners.Labels[cell.X + 1, cell.Y + 1];
        var expectedBr = layout.Corners.Labels[cell.X + 1, cell.Y];
        var expectedBl = layout.Corners.Labels[cell.X, cell.Y];

        var resolvedTile = resolved.GetTile(cell.X, cell.Y);
        var tileRecord = tileset.Tiles[resolvedTile.TileId];

        tileRecord.GetCornerAt(resolvedTile.Orientation, CornerSlot.TopLeft).Should().Be(expectedTl, $"{context} cell {cell} TL");
        tileRecord.GetCornerAt(resolvedTile.Orientation, CornerSlot.TopRight).Should().Be(expectedTr, $"{context} cell {cell} TR");
        tileRecord.GetCornerAt(resolvedTile.Orientation, CornerSlot.BottomRight).Should().Be(expectedBr, $"{context} cell {cell} BR");
        tileRecord.GetCornerAt(resolvedTile.Orientation, CornerSlot.BottomLeft).Should().Be(expectedBl, $"{context} cell {cell} BL");
    }

    private static (int EdgeFromCell, int EdgeBack) DirectionToEdgeSlots(int dx, int dy)
    {
        if (dx == 0 && dy == 1) return (EdgeSlot.Top, EdgeSlot.Bottom);
        if (dx == 1 && dy == 0) return (EdgeSlot.Right, EdgeSlot.Left);
        if (dx == 0 && dy == -1) return (EdgeSlot.Bottom, EdgeSlot.Top);
        if (dx == -1 && dy == 0) return (EdgeSlot.Left, EdgeSlot.Right);
        throw new InvalidOperationException($"DoorCell must be an orthogonal neighbor of Tile; got delta ({dx},{dy}).");
    }
}
