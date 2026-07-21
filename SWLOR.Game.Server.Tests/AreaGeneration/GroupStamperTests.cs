using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Layouts;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// LayoutGroupStamper: pre-designed tileset "group" set pieces (WallRooms hanging off Tunnel
/// corridors, OpenSetPieces dropped into open room interiors) pinned verbatim into a macro layout.
/// Runs the full pipeline against every generation tileset's real .set data, using the same
/// (name -> maxPerArea) configuration StandardTilesetProfiles declares.
/// </summary>
public class GroupStamperTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    // Slot -> (Dx, Dy) step to the neighboring cell across that edge, matching EdgeSlot's
    // Top=0/Right=1/Bottom=2/Left=3 ordering. Duplicated from LayoutGroupStamper (internal, no
    // InternalsVisibleTo) so this test file can walk edges through the public MacroLayout surface.
    private static readonly (int Dx, int Dy)[] SlotOffsets = { (0, 1), (1, 0), (0, -1), (-1, 0) };

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

    // ---------------- Platform03_2x2 (hole-tolerant group footprint) ----------------

    /// <summary>
    /// tdt01/tds01 "Platform03_2x2": an L-shaped 2x2 OpenSetPiece with a genuine hole (one
    /// group.TileIds slot is -1). Confirms the hole cell is never pinned (no member write ever
    /// touches it -- see LayoutGroupStamper.TryClassify's hole handling) yet still resolves
    /// successfully as an ordinary open floor cell once its neighboring real members write their own
    /// (Floor-cornered) corners around it.
    /// </summary>
    [Test]
    public void Platform03_HoleCellNeverPinnedAndResolvesSuccessfully()
    {
        var cases = new (string Tileset, string ProfileKey)[]
        {
            ("tdt01", StandardTilesetProfiles.Cavern),
            ("tds01", StandardTilesetProfiles.Sewers),
        };

        var failures = new List<string>();
        var totalInstances = 0;

        foreach (var (tilesetResref, profileKey) in cases)
        {
            var model = LoadTileset(tilesetResref);
            var group = model.Groups.First(g => string.Equals(g.Name, "Platform03_2x2", StringComparison.OrdinalIgnoreCase));

            // (row0, col0) -- TileIds[0] -- must be a real member; used below as the stamped
            // instance's anchor reference (StampOpenSetPiece always uses orientation 0 -- see
            // LayoutGroupStamper's class doc comment -- so a member's world cell directly gives the
            // anchor via a fixed local-row/col offset).
            group.TileIds[0].Should().BeGreaterThanOrEqualTo(0, $"{tilesetResref} Platform03_2x2's (row0,col0) member must be real");
            var anchorTileId = group.TileIds[0];

            var holeIndex = group.TileIds.IndexOf(-1);
            holeIndex.Should().BeGreaterThanOrEqualTo(0, $"{tilesetResref} Platform03_2x2 must have a hole in its .set data");
            var holeRow = holeIndex / group.Columns;
            var holeCol = holeIndex % group.Columns;

            var setPieces = new Dictionary<string, int> { ["Platform03_2x2"] = 3 };

            for (var seed = 8600; seed < 8660; seed++)
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
                    failures.Add($"{tilesetResref} seed {seed}: generation failed: {ex.Message}");
                    continue;
                }

                macro.Seed = seed;

                if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
                {
                    failures.Add($"{tilesetResref} seed {seed}: resolution failed: {reason}");
                    continue;
                }

                foreach (var (cell, pin) in macro.PinnedTiles)
                {
                    if (pin.TileId != anchorTileId) continue;

                    totalInstances++;
                    var holeCell = (X: cell.X + holeCol, Y: cell.Y + holeRow);

                    if (macro.PinnedTiles.ContainsKey(holeCell))
                    {
                        failures.Add($"{tilesetResref} seed {seed}: hole cell {holeCell} (anchor {cell}) was pinned -- must stay unwritten");
                        continue;
                    }

                    if (holeCell.X < 0 || holeCell.Y < 0 || holeCell.X >= resolved.Width || holeCell.Y >= resolved.Height)
                    {
                        failures.Add($"{tilesetResref} seed {seed}: hole cell {holeCell} (anchor {cell}) fell off-grid");
                        continue;
                    }

                    var holeResolved = resolved.GetTile(holeCell.X, holeCell.Y);
                    if (holeResolved == null)
                        failures.Add($"{tilesetResref} seed {seed}: hole cell {holeCell} (anchor {cell}) did not resolve");
                }
            }
        }

        failures.Should().BeEmpty();
        totalInstances.Should().BeGreaterThan(0, "at least one Platform03_2x2 instance should stamp across the seed sweep");
    }

    // ---------------- Doorway-pair CorridorInsert (tdt01 Door_Trans + Complex Tunnel mode) ----------------

    private static MacroLayoutParameters TunnelComplexParameters(TilesetModel model, Dictionary<string, int> setPieces)
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
            Width = 24,
            Height = 24,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = model.FloorTerrain,
            SetPieces = setPieces
        };
    }

    /// <summary>
    /// tdt01 "Door_Trans": an all-solid tile with an opposite Doorway PAIR (a pass-through doorway
    /// segment), spliced into a straight Corridor chain by LayoutGroupStamper.TryPlaceDoorwayCorridorInsert
    /// rewriting the two flanking plan edges from Corridor to Doorway. Confirms placement actually
    /// occurs, the insert tile resolves verbatim, both Doorway-axis edges agree with the crosser plan,
    /// and each flanking (unpinned) neighbor re-keys to a real tile whose facing edge agrees Doorway
    /// back -- proving the rewritten adapter edges actually resolve, not just get planned.
    /// </summary>
    [Test]
    public void DoorwayPairCorridorInsert_PlacesAndAgreesAcrossSeeds()
    {
        var model = LoadTileset("tdt01");
        var group = model.Groups.First(g => string.Equals(g.Name, "Door_Trans", StringComparison.OrdinalIgnoreCase));
        var tile = model.Tiles[group.TileIds[0]];
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

        var setPieces = new Dictionary<string, int> { ["Door_Trans"] = 3 };
        var failures = new List<string>();
        var totalPlacements = 0;

        for (var seed = 8700; seed < 8760; seed++)
        {
            var rng = new Random(seed);
            var parameters = TunnelComplexParameters(model, setPieces);

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            macro.Seed = seed;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            foreach (var (cell, pin) in macro.PinnedTiles)
            {
                if (pin.TileId != tile.TileId) continue;

                totalPlacements++;

                var resolvedTile = resolved.GetTile(cell.X, cell.Y);
                if (resolvedTile.TileId != pin.TileId || resolvedTile.Orientation != pin.Orientation)
                    failures.Add($"seed {seed}: Door_Trans pin at {cell} did not resolve verbatim");

                for (var slot = 0; slot < 4; slot++)
                {
                    var (dx, dy) = SlotOffsets[slot];
                    var neighbor = (X: cell.X + dx, Y: cell.Y + dy);

                    var actual = tile.GetEdgeAt(pin.Orientation, slot) ?? string.Empty;
                    var planned = macro.Crossers.GetEdge(cell.X, cell.Y, slot) ?? string.Empty;
                    if (!string.Equals(actual, planned, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"seed {seed}: Door_Trans at {cell} slot {slot}: tile says '{actual}' but crosser plan says '{planned}'");

                    if (actual.Length == 0) continue; // blank edge -- nothing further to check

                    if (macro.PinnedTiles.ContainsKey(neighbor))
                        failures.Add($"seed {seed}: Door_Trans at {cell} flanking neighbor {neighbor} is unexpectedly pinned");

                    if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= resolved.Width || neighbor.Y >= resolved.Height)
                    {
                        failures.Add($"seed {seed}: Door_Trans at {cell} has a Doorway edge facing off-grid");
                        continue;
                    }

                    var neighborResolved = resolved.GetTile(neighbor.X, neighbor.Y);
                    var neighborRecord = tilesById[neighborResolved.TileId];
                    var oppositeSlot = (slot + 2) % 4;
                    var neighborEdge = neighborRecord.GetEdgeAt(neighborResolved.Orientation, oppositeSlot) ?? string.Empty;
                    if (!string.Equals(neighborEdge, "Doorway", StringComparison.OrdinalIgnoreCase))
                        failures.Add($"seed {seed}: Door_Trans at {cell} slot {slot} neighbor {neighbor} lacks a matching Doorway edge (got '{neighborEdge}')");
                }
            }
        }

        failures.Should().BeEmpty();
        totalPlacements.Should().BeGreaterThan(0, "at least one Door_Trans Doorway-pair insert should place across the seed sweep");
    }

    [Test]
    public void DoorwayPairCorridorInsert_IsDeterministicPerSeed()
    {
        var model = LoadTileset("tdt01");
        var setPieces = new Dictionary<string, int> { ["Door_Trans"] = 3 };

        ResolvedLayout Resolve(out MacroLayout macro)
        {
            var rng = new Random(8750);
            var parameters = TunnelComplexParameters(model, setPieces);
            macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            macro.Seed = 8750;
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

    /// <summary>
    /// Back-compat: a tileset profile/parameters combination that never configures "Door_Trans" (or
    /// any SetPieces at all) must never exercise the new Doorway-pair branch and must draw identical
    /// RNG regardless of whether HasCorridorDoorwayAdapter would otherwise succeed -- proven by
    /// comparing against the pre-existing EmptySetPieces_ProducesZeroPinsAndUnchangedResolution
    /// pattern on a different tileset (tdt01) that DOES have a usable adapter tile.
    /// </summary>
    [Test]
    public void EmptySetPieces_Tdt01_ProducesZeroPinsAndUnchangedResolution()
    {
        var model = LoadTileset("tdt01");

        var rng = new Random(8770);
        var parameters = TunnelComplexParameters(model, new Dictionary<string, int>());
        var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
        macro.Seed = 8770;

        macro.PinnedTiles.Should().BeEmpty();

        TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);
        foreach (var tile in resolved.Tiles)
        {
            model.Tiles[tile.TileId].GroupIndex.Should().Be(-1, "empty SetPieces must never place a group tile");
        }
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

    // ---------------- Custom-mode tunnel-body-crosser hardcoded-literal fix (synthetic tilesets) ----------------

    /// <summary>
    /// Hand-built minimal TilesetModel/MacroLayout (bypassing MacroLayoutGenerator's carving and
    /// TileResolver entirely -- LayoutGroupStamper.Stamp is `internal`, reachable directly via this
    /// assembly's InternalsVisibleTo) isolating exactly the bug fixed in LayoutGroupStamper:
    /// TryPlaceDoorwayCorridorInsert/IsValidFlankingChainCell hardcoded the literal canonical "Corridor"
    /// crosser when searching for a straight tunnel-body chain cell to splice a Doorway-pair
    /// CorridorInsert into, instead of also accepting a Custom-mode composition's own declared
    /// TunnelBodyCrosser (e.g. tdc01's "GreyCorridor") -- so a Custom-mode composition could NEVER
    /// place a Doorway-pair CorridorInsert (like tdt01's real "Door_Trans", proven working under
    /// canonical Corridor by DoorwayPairCorridorInsert_PlacesAndAgreesAcrossSeeds above) even though
    /// classification (TryClassifyCorridorInsert, which already threads TunnelBodyCrosser through
    /// CorridorInsertCrossersFor at classify time) recognized the shape correctly. Grid: three solid
    /// cells in a row, (0,0)-(1,0)-(2,0), pre-carved as a straight "GreyCorridor" chain (Left+Right
    /// edges on all three cells, Top/Bottom left blank); the candidate insert tile has an opposite
    /// Doorway pair (Left+Right) on all-solid corners, matching the real Door_Trans shape. A second,
    /// unrelated tile (never a SetPiece) declares a canonical Corridor|Doorway adapter pair purely so
    /// HasCorridorDoorwayAdapter's OWN (unrelated, still-hardcoded-canonical, out of this fix's scope)
    /// capability probe passes -- isolating this test to the site-search bug this fix actually
    /// addresses. Empirically confirmed against this exact scenario: 0 pins with the pre-fix hardcoded
    /// "Corridor" check (git-reverted LayoutGroupStamper.cs), 1 pin after the fix.
    /// </summary>
    [Test]
    public void CustomBodyCrosserDoorwayPairCorridorInsert_PlacesAgainstRenamedTunnelBody()
    {
        var model = new TilesetModel
        {
            Resref = "synthetic_customcorridor",
            DefaultTerrain = "Wall",
            FloorTerrain = "Floor",
            Tiles = new List<TileRecord>
            {
                // Tile 0: the Doorway-pair CorridorInsert candidate (mirrors tdt01's real "Door_Trans").
                new TileRecord
                {
                    TileId = 0,
                    Corners = new[] { "Wall", "Wall", "Wall", "Wall" },
                    Edges = new[] { "", "Doorway", "", "Doorway" }, // Top,Right,Bottom,Left -- Left+Right pair
                    CornerHeights = new[] { 0, 0, 0, 0 }
                },
                // Tile 1: unrelated canonical Corridor|Doorway adapter tile, satisfying
                // HasCorridorDoorwayAdapter's own (separately hardcoded, out-of-scope-for-this-fix)
                // capability probe. Never registered as a SetPiece.
                new TileRecord
                {
                    TileId = 1,
                    Corners = new[] { "Wall", "Wall", "Wall", "Wall" },
                    Edges = new[] { "Corridor", "", "Doorway", "" }, // Top=Corridor, Bottom=Doorway -- opposite pair
                    CornerHeights = new[] { 0, 0, 0, 0 }
                }
            },
            Groups = new List<TileGroupRecord>
            {
                new TileGroupRecord { Name = "TestDoorTrans", Rows = 1, Columns = 1, TileIds = new List<int> { 0 } }
            }
        };

        var parameters = new MacroLayoutParameters
        {
            Width = 3,
            Height = 1,
            SolidTerrain = "Wall",
            OpenTerrain = string.Empty,
            CorridorCrosserType = CorridorCrosserType.Custom,
            TunnelBodyCrosser = "GreyCorridor",
            TunnelPortCrosser = "Doorway",
            SetPieces = new Dictionary<string, int> { ["TestDoorTrans"] = 1 }
        };

        var layout = new MacroLayout(new CornerTerrainGrid(3, 1, "Wall"));

        // Pre-carve a straight "GreyCorridor" chain across all three cells (Left+Right on each,
        // Top/Bottom left blank -- IsStraightCorridorCell's required shape).
        layout.Crossers.SetEdge(0, 0, EdgeSlot.Left, "GreyCorridor");
        layout.Crossers.SetEdge(0, 0, EdgeSlot.Right, "GreyCorridor"); // = cell (1,0) Left, shared storage
        layout.Crossers.SetEdge(1, 0, EdgeSlot.Right, "GreyCorridor"); // = cell (2,0) Left, shared storage
        layout.Crossers.SetEdge(2, 0, EdgeSlot.Right, "GreyCorridor");

        LayoutGroupStamper.Stamp(layout, parameters, model, new Random(1));

        layout.PinnedTiles.Should().ContainSingle(
            "a Custom-mode composition's own renamed tunnel-body crosser (GreyCorridor) must be recognized as a " +
            "valid straight chain to splice a Doorway-pair CorridorInsert into, the same way canonical Corridor already is");
        var pin = layout.PinnedTiles[(1, 0)];
        pin.TileId.Should().Be(0);
        layout.Crossers.GetEdge(1, 0, EdgeSlot.Left).Should().Be("Doorway");
        layout.Crossers.GetEdge(1, 0, EdgeSlot.Right).Should().Be("Doorway");
    }

    /// <summary>
    /// Same synthetic-model technique as CustomBodyCrosserDoorwayPairCorridorInsert_PlacesAgainstRenamedTunnelBody
    /// above, isolating the OTHER hardcoded-literal site this fix addresses: IsWallRoomSiteValid's
    /// neighborHasCorridor check. A 1x1 WallRoom candidate (single Doorway port, all-solid corners) is
    /// anchored at (0,0) in a 2x1 grid; its ONLY perimeter neighbor, cell (1,0), carries the
    /// composition's renamed "GreyCorridor" body crosser on its OWN far edge (never on the shared edge
    /// with the candidate, which must stay blank for the candidate's own footprint to qualify).
    /// OpenTerrain is deliberately left empty so SupportsWallRoomOpenLaneBoundary's capability probe
    /// can never return true, forcing corridor-adjacency to be the ONLY possible site-validity path --
    /// unlike every currently-onboarded Custom-mode profile's own "Door - Transition"-style WallRoom
    /// group, whose port always faces a genuine open-terrain room boundary in practice (see this
    /// session's own measurement notes), so the OpenLane fallback masks this exact bug for all
    /// currently-wired content. Empirically confirmed against this exact scenario: 0 pins pre-fix
    /// (git-reverted LayoutGroupStamper.cs), 1 pin after.
    /// </summary>
    [Test]
    public void CustomBodyCrosserWallRoom_PlacesAgainstRenamedTunnelBody_WithNoOpenLaneFallback()
    {
        var model = new TilesetModel
        {
            Resref = "synthetic_customcorridor_wallroom",
            DefaultTerrain = "Wall",
            FloorTerrain = "Floor",
            Tiles = new List<TileRecord>
            {
                new TileRecord
                {
                    TileId = 0,
                    Corners = new[] { "Wall", "Wall", "Wall", "Wall" },
                    Edges = new[] { "", "Doorway", "", "" }, // single Right-facing Doorway port
                    CornerHeights = new[] { 0, 0, 0, 0 }
                }
            },
            Groups = new List<TileGroupRecord>
            {
                new TileGroupRecord { Name = "TestWallRoom", Rows = 1, Columns = 1, TileIds = new List<int> { 0 } }
            }
        };

        var parameters = new MacroLayoutParameters
        {
            Width = 2,
            Height = 1,
            SolidTerrain = "Wall",
            OpenTerrain = string.Empty, // forces SupportsWallRoomOpenLaneBoundary false -- no fallback masking
            CorridorCrosserType = CorridorCrosserType.Custom,
            TunnelBodyCrosser = "GreyCorridor",
            TunnelPortCrosser = "Doorway",
            SetPieces = new Dictionary<string, int> { ["TestWallRoom"] = 1 }
        };

        var layout = new MacroLayout(new CornerTerrainGrid(2, 1, "Wall"));

        // (1,0)'s own far (Right) edge carries the renamed tunnel body crosser -- never the shared
        // edge with (0,0), which must stay blank for (0,0)'s WallRoom footprint to qualify.
        layout.Crossers.SetEdge(1, 0, EdgeSlot.Right, "GreyCorridor");

        LayoutGroupStamper.Stamp(layout, parameters, model, new Random(1));

        layout.PinnedTiles.Should().ContainSingle(
            "a WallRoom port whose only neighbor is a Custom-mode renamed tunnel-body chain cell (GreyCorridor) " +
            "must validate via corridor-adjacency the same way canonical Corridor already does, even with no " +
            "OpenLane-boundary fallback available");
        var pin = layout.PinnedTiles[(0, 0)];
        pin.TileId.Should().Be(0);
    }
}
