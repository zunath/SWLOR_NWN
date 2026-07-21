using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

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
                            claimedCells.Should().NotContain(transition.Tile, $"{context}: anchor cell reused");
                            claimedCells.Should().NotContain(transition.DoorwayCell, $"{context}: room-edge cell reused");
                            claimedCells.Should().NotContain(transition.DoorCell, $"{context}: solid cell reused");
                            claimedCells.Add(transition.Tile);
                            claimedCells.Add(transition.DoorwayCell);
                            claimedCells.Add(transition.DoorCell);

                            // The anchor stays on plain open room floor directly in front of the
                            // doorway — waypoints and arrival jumps use it and must never sit in
                            // the doorway wall tile or under feature-tile decor.
                            var anchorDx = Math.Abs(transition.DoorwayCell.X - transition.Tile.X);
                            var anchorDy = Math.Abs(transition.DoorwayCell.Y - transition.Tile.Y);
                            (anchorDx + anchorDy).Should().Be(1, $"{context}: anchor must be orthogonally adjacent to the doorway cell");
                            var anchorResolved = resolved.GetTile(transition.Tile.X, transition.Tile.Y);
                            tileset.Tiles[anchorResolved.TileId].GroupIndex.Should().Be(-1,
                                $"{context}: anchor cell must not hold a feature/group tile");

                            // (a) the room-edge doorway tile actually has a door slot.
                            var roomEdgeResolved = resolved.GetTile(transition.DoorwayCell.X, transition.DoorwayCell.Y);
                            var roomEdgeRecord = tileset.Tiles[roomEdgeResolved.TileId];
                            roomEdgeRecord.Doors.Should().NotBeEmpty($"{context}: room-edge tile must have a door slot");

                            // (b) substituted tiles' corners still match the corner grid labels.
                            AssertCornersMatch(tileset, layout, resolved, transition.DoorwayCell, context);
                            AssertCornersMatch(tileset, layout, resolved, transition.DoorCell, context);

                            // (c) facing edges both Doorway.
                            var dx = transition.DoorCell.X - transition.DoorwayCell.X;
                            var dy = transition.DoorCell.Y - transition.DoorwayCell.Y;
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

    // ---------------- Grouped terminator tolerance (Fix A: tds01/vmr01 "Door_Trans"/"Door_Trans_Exterior") ----------------

    /// <summary>
    /// Structural check mirroring TileDoorPlanner.BuildTerminatorCandidates' post-Fix-A rule: a
    /// tile wrapped in a trivial 1x1 [GROUPn] entry, flat, all-solid-cornered, with at least one
    /// rotation carrying exactly one Doorway edge, registers as a terminator candidate exactly like
    /// an ungrouped tile would. Duplicated here (rather than reaching into TileDoorPlanner's
    /// internals) per this file's existing convention.
    /// </summary>
    [TestCase("tds01", "sw_t_sewer", "tds01.set", 174)]
    [TestCase("vmr01", "sw_t_alienruin", "vmr01.set", 152)]
    [TestCase("vmr01", "sw_t_alienruin", "vmr01.set", 60)]
    public void GroupedTerminator_StructurallyEligibleUnderNewGroupTolerance(string resref, string hak, string setFile, int tileId)
    {
        var model = LoadTileset(resref, hak, setFile);
        var tile = model.Tiles[tileId];

        tile.GroupIndex.Should().NotBe(-1, $"{resref} TILE{tileId} is expected to be group-wrapped -- exactly the case Fix A tolerates");
        var group = model.Groups[tile.GroupIndex];
        group.Rows.Should().Be(1);
        group.Columns.Should().Be(1);

        tile.CornerHeights.Should().OnlyContain(h => h == 0);
        tile.Corners.Should().OnlyContain(c => string.Equals(c, model.DefaultTerrain, StringComparison.OrdinalIgnoreCase));

        var doorwayOnlyOrientations = 0;
        for (var orientation = 0; orientation < 4; orientation++)
        {
            var doorwayCount = 0;
            var disqualified = false;
            for (var slot = 0; slot < 4; slot++)
            {
                var edge = tile.GetEdgeAt(orientation, slot);
                if (string.Equals(edge, "Doorway", StringComparison.OrdinalIgnoreCase)) doorwayCount++;
                else if (!string.IsNullOrEmpty(edge)) disqualified = true;
            }
            if (!disqualified && doorwayCount == 1) doorwayOnlyOrientations++;
        }

        doorwayOnlyOrientations.Should().BeGreaterThan(0,
            $"{resref} TILE{tileId} must have at least one orientation with exactly one Doorway edge to register as a terminator candidate");
    }

    /// <summary>
    /// vmr01 has real ungrouped terminator competitors too, but TILE152/60 win the deterministic
    /// "smallest TileId per facing direction" pick often enough to observe directly across an
    /// ordinary seed sweep (empirically ~1200+ hits per 900 attempts) -- so this asserts real,
    /// unmodified end-to-end usage rather than falling back to a forced scenario.
    /// </summary>
    [Test]
    public void GroupedTerminator_Vmr01_RealSeedSweepProducesAtLeastOneUsage()
    {
        var tileset = LoadTileset("vmr01", "sw_t_alienruin", "vmr01.set");
        var groupedTerminatorIds = new HashSet<int> { 152, 60 };
        var hits = 0;

        foreach (var style in AllStyles)
        {
            for (var seed = 1; seed <= 20 && hits == 0; seed++)
            {
                var parameters = Parameters(style, tileset, entrances: 2, exits: 2, doorTransitions: true);
                MacroLayout layout;
                try { layout = MacroLayoutGenerator.Generate(parameters, new Random(seed)); }
                catch (InvalidOperationException) { continue; }
                layout.Seed = seed;

                if (!TileResolver.TryResolve(tileset, layout, new Random(seed * 31 + 7), out var resolved, out _)) continue;

                foreach (var t in resolved.Transitions)
                {
                    if (t.Style != TransitionStyle.Door) continue;
                    var doorCellTile = resolved.GetTile(t.DoorCell.X, t.DoorCell.Y);
                    if (groupedTerminatorIds.Contains(doorCellTile.TileId)) hits++;
                }
            }
        }

        hits.Should().BeGreaterThan(0, "vmr01 TILE152/60 should be selected as a real terminator at least once across the seed sweep");
    }

    /// <summary>
    /// tds01 TILE174 ("Door_Trans") can never win TileDoorPlanner's deterministic "smallest TileId
    /// per facing direction" terminator pick over TILE157 (an ungrouped terminator of the identical
    /// shape with a smaller TileId, verified the sole competitor for all four directions) under
    /// normal generation -- so real-usage occurrence is zero, not just rare. This forces the real
    /// pipeline to pick TILE174 anyway by disqualifying a CLONED copy of TILE157 (raised corners,
    /// so every flat-tile pool -- including BuildTerminatorCandidates -- rejects it), proving TILE174
    /// really is wired into terminator selection end-to-end, not just structurally eligible per
    /// GroupedTerminator_StructurallyEligibleUnderNewGroupTolerance above.
    /// </summary>
    [Test]
    public void GroupedTerminator_Tds01_IsSelectedWhenItIsTheOnlyCandidate()
    {
        var baseModel = LoadTileset("tds01", "sw_t_sewer", "tds01.set");
        var forcedModel = new TilesetModel
        {
            Resref = baseModel.Resref,
            Name = baseModel.Name,
            IsInterior = baseModel.IsInterior,
            HasHeightTransition = baseModel.HasHeightTransition,
            HeightTransition = baseModel.HeightTransition,
            BorderTerrain = baseModel.BorderTerrain,
            DefaultTerrain = baseModel.DefaultTerrain,
            FloorTerrain = baseModel.FloorTerrain,
            Terrains = baseModel.Terrains,
            Crossers = baseModel.Crossers,
            Tiles = baseModel.Tiles.Select(CloneTile).ToList(),
            Groups = baseModel.Groups
        };
        // Raised corners disqualify TILE157 from every flat-tile pool (ordinary resolution, feature
        // lookup, AND terminator candidates) in this isolated clone only -- the shared model other
        // tests load is unaffected since LoadTileset re-parses the .set file fresh each call.
        forcedModel.Tiles[157].CornerHeights = new[] { 1, 0, 0, 0 };

        var found = false;
        var context = string.Empty;

        foreach (var style in AllStyles)
        {
            for (var seed = 1; seed <= 40 && !found; seed++)
            {
                var parameters = Parameters(style, forcedModel, entrances: 2, exits: 2, doorTransitions: true);
                MacroLayout layout;
                try { layout = MacroLayoutGenerator.Generate(parameters, new Random(seed)); }
                catch (InvalidOperationException) { continue; }
                layout.Seed = seed;

                if (!TileResolver.TryResolve(forcedModel, layout, new Random(seed * 31 + 7), out var resolved, out _)) continue;

                foreach (var t in resolved.Transitions)
                {
                    if (t.Style != TransitionStyle.Door) continue;
                    var doorCellTile = resolved.GetTile(t.DoorCell.X, t.DoorCell.Y);
                    if (doorCellTile.TileId == 174)
                    {
                        found = true;
                        context = $"{style} seed {seed}";
                        break;
                    }
                }
            }
        }

        found.Should().BeTrue($"with TILE157 disqualified, TILE174 is the sole remaining terminator candidate for every direction; expected a hit, found at [{context}]");
    }

    private static TileRecord CloneTile(TileRecord t) => new()
    {
        TileId = t.TileId,
        Model = t.Model,
        WalkMesh = t.WalkMesh,
        PathNode = t.PathNode,
        ImageMap2D = t.ImageMap2D,
        Corners = (string[])t.Corners.Clone(),
        CornerHeights = (int[])t.CornerHeights.Clone(),
        Edges = (string[])t.Edges.Clone(),
        GroupIndex = t.GroupIndex,
        Doors = t.Doors
    };

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
