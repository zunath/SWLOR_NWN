using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// LayoutGroupStamper: pre-designed tileset "group" set pieces (WallRooms hanging off Tunnel
/// corridors, OpenSetPieces dropped into open room interiors) pinned verbatim into a macro layout.
/// Runs the full pipeline against every generation tileset's real .set data, using the same
/// (name -> maxPerArea) configuration StandardTilesetProfiles declares.
/// </summary>
public class GroupStamperTests
{
    private static readonly Dictionary<string, string> TilesetHakDirectories = new()
    {
        ["tdt01"] = "sw_t_minecave",
        ["zsf01"] = "sw_t_scifibase",
        ["tds01"] = "sw_t_sewer",
        ["vmr01"] = "sw_t_alienruin",
    };

    // Slot -> (Dx, Dy) step to the neighboring cell across that edge, matching EdgeSlot's
    // Top=0/Right=1/Bottom=2/Left=3 ordering. Duplicated from LayoutGroupStamper (internal, no
    // InternalsVisibleTo) so this test file can walk edges through the public MacroLayout surface.
    private static readonly (int Dx, int Dy)[] SlotOffsets = { (0, 1), (1, 0), (0, -1), (-1, 0) };

    private static TilesetModel LoadTileset(string tilesetResref)
    {
        var root = FindRepositoryRoot();
        var hakDirectory = TilesetHakDirectories[tilesetResref];
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

    private static MacroLayoutParameters FacilityCorridorComplexParameters(TilesetModel model, Dictionary<string, int> setPieces)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel,
            MinRooms = 6,
            MaxRooms = 9,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            LoopFactor = 0.3,
            Width = 20,
            Height = 20,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = "floor",
            SetPieces = setPieces
        };
    }

    private static MacroLayoutParameters BigOrganicCaveParameters(TilesetModel model, string openTerrain, Dictionary<string, int> setPieces)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.OrganicCave,
            Width = 24,
            Height = 24,
            OpenFillTarget = 0.5,
            SmoothingPasses = 4,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = openTerrain,
            SetPieces = setPieces
        };
    }

    // ---------------- WallRoom (zsf01 Corridor Complex + facility set pieces) ----------------

    [Test]
    public void WallRoom_PinsAreConsistentAndDoorwaysFaceCorridors()
    {
        var model = LoadTileset("zsf01");
        var setPieces = new StandardTilesetProfiles().BuildTilesetProfiles()[StandardTilesetProfiles.Facility].SetPieces;
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

        var failures = new List<string>();
        var totalPins = 0;
        var totalSetPieceRooms = 0;

        for (var seed = 8000; seed < 8015; seed++)
        {
            var rng = new Random(seed);
            var parameters = FacilityCorridorComplexParameters(model, setPieces);

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            totalPins += macro.PinnedTiles.Count;
            totalSetPieceRooms += macro.Rooms.Count(r => r.IsSetPiece);

            var transitionTiles = new HashSet<(int X, int Y)>(macro.Transitions.Select(t => t.Tile));

            foreach (var (cell, pin) in macro.PinnedTiles)
            {
                if (transitionTiles.Contains(cell))
                    failures.Add($"seed {seed}: pin at {cell} sits on a transition tile");

                var resolvedTile = resolved.GetTile(cell.X, cell.Y);
                if (resolvedTile.TileId != pin.TileId || resolvedTile.Orientation != pin.Orientation)
                {
                    failures.Add($"seed {seed}: pin at {cell} expected TILE{pin.TileId} o={pin.Orientation} but resolved TILE{resolvedTile.TileId} o={resolvedTile.Orientation}");
                }

                // Edge agreement, skipping edges shared between two pinned cells (their interior
                // boundary is not required to agree -- see LayoutGroupStamper.WriteMember).
                var record = tilesById[pin.TileId];
                for (var slot = 0; slot < 4; slot++)
                {
                    var (dx, dy) = SlotOffsets[slot];
                    var neighbor = (X: cell.X + dx, Y: cell.Y + dy);
                    if (macro.PinnedTiles.ContainsKey(neighbor)) continue;

                    var actual = record.GetEdgeAt(pin.Orientation, slot) ?? string.Empty;
                    var planned = macro.Crossers.GetEdge(cell.X, cell.Y, slot) ?? string.Empty;
                    if (!string.Equals(actual, planned, StringComparison.OrdinalIgnoreCase))
                    {
                        failures.Add($"seed {seed}: pin at {cell} slot {slot}: tile data says '{actual}' but crosser plan says '{planned}'");
                    }

                    if (!string.Equals(actual, "Doorway", StringComparison.OrdinalIgnoreCase)) continue;
                    if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= resolved.Width || neighbor.Y >= resolved.Height)
                    {
                        failures.Add($"seed {seed}: pin at {cell} has a Doorway edge facing off-grid");
                        continue;
                    }

                    // The corridor-adjacent neighbor must resolve to a tile that itself carries a
                    // matching Doorway edge facing back (the shared crosser-grid slot guarantees the
                    // VALUE agrees; this additionally proves the resolver actually found a real tile).
                    var neighborResolved = resolved.GetTile(neighbor.X, neighbor.Y);
                    var neighborRecord = tilesById[neighborResolved.TileId];
                    var oppositeSlot = (slot + 2) % 4;
                    var neighborEdge = neighborRecord.GetEdgeAt(neighborResolved.Orientation, oppositeSlot) ?? string.Empty;
                    if (!string.Equals(neighborEdge, "Doorway", StringComparison.OrdinalIgnoreCase))
                    {
                        failures.Add($"seed {seed}: pin at {cell} Doorway edge (slot {slot}) has neighbor {neighbor} whose resolved tile lacks a matching Doorway edge");
                    }
                }
            }
        }

        failures.Should().BeEmpty();
        totalSetPieceRooms.Should().BeGreaterThan(0, "at least some of 15 seeds should stamp a WallRoom set piece");
        totalPins.Should().BeGreaterThan(0);
    }

    [Test]
    public void WallRoom_NeverLandsOnTransitionCells()
    {
        var model = LoadTileset("zsf01");
        var setPieces = new StandardTilesetProfiles().BuildTilesetProfiles()[StandardTilesetProfiles.Facility].SetPieces;

        for (var seed = 8100; seed < 8115; seed++)
        {
            var rng = new Random(seed);
            var parameters = FacilityCorridorComplexParameters(model, setPieces);
            parameters.EntranceCount = 2;
            parameters.ExitCount = 3;

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            var transitionTiles = new HashSet<(int X, int Y)>(macro.Transitions.Select(t => t.Tile));
            foreach (var cell in macro.PinnedTiles.Keys)
            {
                transitionTiles.Should().NotContain(cell, $"seed {seed}: pin at {cell}");
            }
        }
    }

    // ---------------- OpenSetPiece (tdt01 big OrganicCave + cavern set pieces) ----------------

    [Test]
    public void OpenSetPiece_LandsInsideRoomsWithMarginAndRemovesFootprintFromRoomTiles()
    {
        var model = LoadTileset("tdt01");
        var setPieces = new StandardTilesetProfiles().BuildTilesetProfiles()[StandardTilesetProfiles.Cavern].SetPieces;
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

        var failures = new List<string>();
        var totalPins = 0;

        for (var seed = 8500; seed < 8515; seed++)
        {
            var rng = new Random(seed);
            var parameters = BigOrganicCaveParameters(model, model.FloorTerrain, setPieces);

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            totalPins += macro.PinnedTiles.Count;

            // No room should still list a pinned (stamped-over) tile as plain spawnable floor.
            foreach (var room in macro.Rooms.Where(r => !r.IsSetPiece))
            {
                foreach (var tile in room.Tiles)
                {
                    if (macro.PinnedTiles.ContainsKey(tile))
                        failures.Add($"seed {seed}: room {room.Id} still lists pinned cell {tile} in Tiles");
                }
            }

            foreach (var (cell, pin) in macro.PinnedTiles)
            {
                var resolvedTile = resolved.GetTile(cell.X, cell.Y);
                if (resolvedTile.TileId != pin.TileId || resolvedTile.Orientation != pin.Orientation)
                {
                    failures.Add($"seed {seed}: pin at {cell} expected TILE{pin.TileId} o={pin.Orientation} but resolved TILE{resolvedTile.TileId} o={resolvedTile.Orientation}");
                }

                var record = tilesById[pin.TileId];
                record.Doors.Count.Should().Be(0, $"seed {seed}: OpenSetPiece member at {cell} must never carry door slots");
            }
        }

        failures.Should().BeEmpty();
        totalPins.Should().BeGreaterThan(0, "at least some of 15 seeds on a large open cavern should stamp an OpenSetPiece");
    }

    // ---------------- Determinism ----------------

    [Test]
    public void Stamping_IsDeterministicPerSeed()
    {
        var model = LoadTileset("zsf01");
        var setPieces = new StandardTilesetProfiles().BuildTilesetProfiles()[StandardTilesetProfiles.Facility].SetPieces;

        ResolvedLayout Resolve(out MacroLayout macro)
        {
            var rng = new Random(8300);
            var parameters = FacilityCorridorComplexParameters(model, setPieces);
            macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            macro.Seed = 8300;
            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);
            return resolved;
        }

        var first = Resolve(out var firstMacro);
        var second = Resolve(out var secondMacro);

        firstMacro.PinnedTiles.Count.Should().Be(secondMacro.PinnedTiles.Count);
        foreach (var (cell, pin) in firstMacro.PinnedTiles)
        {
            secondMacro.PinnedTiles.Should().ContainKey(cell);
            secondMacro.PinnedTiles[cell].Should().Be(pin);
        }

        for (var i = 0; i < first.Tiles.Length; i++)
        {
            first.Tiles[i].TileId.Should().Be(second.Tiles[i].TileId, $"cell index {i}");
            first.Tiles[i].Orientation.Should().Be(second.Tiles[i].Orientation, $"cell index {i}");
        }
    }

    // ---------------- Back-compat ----------------

    [Test]
    public void EmptySetPieces_ProducesZeroPinsAndUnchangedResolution()
    {
        var model = LoadTileset("zsf01");

        var rng = new Random(8400);
        var parameters = FacilityCorridorComplexParameters(model, new Dictionary<string, int>());
        var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
        macro.Seed = 8400;

        macro.PinnedTiles.Should().BeEmpty();

        TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);
        foreach (var tile in resolved.Tiles)
        {
            model.Tiles[tile.TileId].GroupIndex.Should().Be(-1, "empty SetPieces must never place a group tile");
        }
    }

    [Test]
    public void NullTilesetModel_SkipsStampingEntirelyEvenWithSetPiecesConfigured()
    {
        var model = LoadTileset("zsf01");
        var setPieces = new StandardTilesetProfiles().BuildTilesetProfiles()[StandardTilesetProfiles.Facility].SetPieces;

        var rng = new Random(8450);
        var parameters = FacilityCorridorComplexParameters(model, setPieces);

        // No TilesetModel passed -- back-compat overload behavior for existing callers.
        var macro = MacroLayoutGenerator.Generate(parameters, rng);

        macro.PinnedTiles.Should().BeEmpty();
        macro.Rooms.Should().NotContain(r => r.IsSetPiece);
    }

    // ---------------- Full pipeline sweep (all four tilesets, production layout pairing) ----------------

    [TestCase(StandardTilesetProfiles.Facility, StandardLayoutProfiles.Complex)]
    [TestCase(StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Organic)]
    [TestCase(StandardTilesetProfiles.Sewers, StandardLayoutProfiles.Warren)]
    [TestCase(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls)]
    public void FullPipelineSweep_ProfileDefaultsNeverFailGenerationAcrossManySeeds(string tilesetKey, string layoutKey)
    {
        var tilesetProfile = new StandardTilesetProfiles().BuildTilesetProfiles()[tilesetKey];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[layoutKey];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        const int retryBudget = 6;
        var failures = new List<string>();

        for (var seedBase = 10000; seedBase < 10015; seedBase++)
        {
            var succeeded = false;
            var lastFailure = "no attempts made";

            for (var attempt = 0; attempt < retryBudget && !succeeded; attempt++)
            {
                var trySeed = seedBase + attempt;
                var rng = new Random(trySeed);

                var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
                var parameters = composition.BuildLayoutParameters();
                parameters.Width = 20;
                parameters.Height = 20;
                parameters.SolidTerrain = model.DefaultTerrain;
                parameters.OpenTerrain = string.IsNullOrEmpty(tilesetProfile.PrimaryOpenTerrain)
                    ? model.FloorTerrain
                    : tilesetProfile.PrimaryOpenTerrain;

                MacroLayout macro;
                try
                {
                    macro = MacroLayoutGenerator.Generate(parameters, rng, model);
                    macro.Seed = trySeed;
                }
                catch (InvalidOperationException ex)
                {
                    lastFailure = ex.Message;
                    continue;
                }

                if (TileResolver.TryResolve(model, macro, rng, out _, out var reason))
                {
                    succeeded = true;
                }
                else
                {
                    lastFailure = reason;
                }
            }

            if (!succeeded)
                failures.Add($"{tilesetKey}/{layoutKey} seed base {seedBase}: {lastFailure}");
        }

        failures.Should().BeEmpty();
    }

    // ---------------- Group-geometry convention ----------------

    /// <summary>
    /// Pins the row-major "row 0 = south, column 0 = west at orientation 0" convention documented on
    /// LayoutGroupStamper against zsf01's real "Bedroom" group data (Rows=2, Columns=1, TileIds=[70,
    /// 69]) -- the same .set facts this session verified empirically against the hand-built
    /// czs220_maintlvl area (TILE69/70 placements at orientations 1 and 2). If TILE69/70's roles or
    /// this group's shape ever change in zsf01.set, this test forces a re-verification of the
    /// convention before anyone relies on it again.
    /// </summary>
    [Test]
    public void GroupGeometryConvention_BedroomRowZeroIsSouthAtOrientationZero()
    {
        var model = LoadTileset("zsf01");
        var group = model.Groups.First(g => string.Equals(g.Name, "Bedroom", StringComparison.OrdinalIgnoreCase));

        group.Rows.Should().Be(2);
        group.Columns.Should().Be(1);
        group.TileIds.Should().Equal(70, 69);

        var southTile = model.Tiles[group.TileIds[0]]; // row 0
        var northTile = model.Tiles[group.TileIds[1]]; // row 1

        // At orientation 0, row 0 (south) opens toward the world further south, and row 1 (north)
        // opens toward the world further north -- i.e. each member's own perimeter Doorway faces
        // strictly away from the other member, confirming row index tracks world Y directly (no flip).
        southTile.GetEdgeAt(0, EdgeSlot.Bottom).Should().BeEmpty(
            "row 0 (south)'s only Doorway is interior (Top, facing row 1), not this perimeter edge");
        northTile.GetEdgeAt(0, EdgeSlot.Top).Should().Be("Doorway",
            "row 1 (north)'s perimeter Doorway must face further north at orientation 0");
    }

    [Test]
    public void GroupGeometryConvention_TwoByOneRoomMatchesBedroomShape()
    {
        // "2x1Room" is actually Rows=2, Columns=1 in the .set data (same vertical-pair shape as
        // Bedroom, just a different member pair) -- verified directly rather than assumed from name.
        var model = LoadTileset("zsf01");
        var group = model.Groups.First(g => string.Equals(g.Name, "2x1Room", StringComparison.OrdinalIgnoreCase));

        group.Rows.Should().Be(2);
        group.Columns.Should().Be(1);
        group.TileIds.Should().Equal(72, 71);

        var northTile = model.Tiles[group.TileIds[1]];
        northTile.GetEdgeAt(0, EdgeSlot.Top).Should().Be("Doorway");
    }
}
