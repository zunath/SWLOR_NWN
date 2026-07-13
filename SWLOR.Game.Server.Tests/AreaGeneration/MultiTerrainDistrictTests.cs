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
/// Multi-terrain districts: a single generated area mixing multiple open terrains as separate,
/// walled districts joined by Tunnel-mode corridors/doorways (MacroLayoutParameters.
/// SecondaryOpenTerrain, RoomsAndCorridorsLayout's per-room terrain roll, LayoutTunnelCarver's
/// per-room port terrain). This is the biggest tile-coverage unlock in the generator: zsf01's
/// 'Floor2' (~13 tiles) and vmr01's 'Floor' (~50 tiles, including feature/set-piece groups keyed to
/// Floor corners) were both entirely unreachable under the single-open-terrain constraint.
///
/// v1 scope: RoomsAndCorridors + Tunnel mode only, and only with the plain Corridor/Doorway crosser
/// vocabulary (not Alley -- verified present only for vmr01's Plaza terrain, see
/// RoomsAndCorridorsLayout's useDistricts gate). Runs the full pipeline against every generation
/// tileset's real .set data, same conventions as TunnelCorridorTests/FenceAndAlleyTests.
/// </summary>
public class MultiTerrainDistrictTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static MacroLayoutParameters DistrictParameters(
        TilesetModel model, string primaryOpen, string secondaryOpen, int width = 22, double secondaryFraction = 0.35)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel,
            CorridorCrosserType = CorridorCrosserType.Corridor,
            MinRooms = 6,
            MaxRooms = 9,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            LoopFactor = 0.3,
            Width = width,
            Height = width,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = primaryOpen,
            SecondaryOpenTerrain = secondaryOpen,
            SecondaryRoomFraction = secondaryFraction,
        };
    }

    /// <summary>
    /// Global edge-agreement proof, mirroring TunnelCorridorTests/FenceAndAlleyTests: every resolved
    /// tile's oriented edges must match the crosser plan on all four sides.
    /// </summary>
    private static void AssertEdgeAgreement(TilesetModel model, MacroLayout macro, ResolvedLayout resolved, int seed, List<string> failures)
    {
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

        var doorCells = new HashSet<(int X, int Y)>();
        foreach (var transition in resolved.Transitions.Where(t => t.Style == TransitionStyle.Door))
        {
            doorCells.Add(transition.Tile);
            doorCells.Add(transition.DoorCell);
            doorCells.Add(transition.DoorwayCell);
        }

        for (var y = 0; y < resolved.Height; y++)
        {
            for (var x = 0; x < resolved.Width; x++)
            {
                if (doorCells.Contains((x, y))) continue;

                var tile = resolved.GetTile(x, y);
                var record = tilesById[tile.TileId];

                for (var slot = 0; slot < 4; slot++)
                {
                    var actual = record.GetEdgeAt(tile.Orientation, slot) ?? string.Empty;
                    var expected = macro.Crossers.GetEdge(x, y, slot) ?? string.Empty;
                    if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        failures.Add($"seed {seed}: cell ({x},{y}) slot {slot}: planned '{expected}' but resolved TILE{tile.TileId} o={tile.Orientation} has '{actual}'");
                    }
                }
            }
        }
    }

    private static bool RoomInteriorMatchesOwnTerrain(MacroLayout macro, LayoutRoom room, out string failure)
    {
        failure = null;
        foreach (var (x, y) in room.Tiles)
        {
            var tl = macro.Corners.Labels[x, y + 1];
            var tr = macro.Corners.Labels[x + 1, y + 1];
            var br = macro.Corners.Labels[x + 1, y];
            var bl = macro.Corners.Labels[x, y];

            if (!Eq(tl, room.OpenTerrain) || !Eq(tr, room.OpenTerrain) || !Eq(br, room.OpenTerrain) || !Eq(bl, room.OpenTerrain))
            {
                failure = $"room {room.Id} (OpenTerrain='{room.OpenTerrain}') tile ({x},{y}) corners=[{tl},{tr},{br},{bl}]";
                return false;
            }
        }

        return true;
    }

    private static bool Eq(string a, string b) => string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);

    // ============================================================
    // zsf01: floor (primary) + Floor2 (secondary) districts
    // ============================================================

    [Test]
    public void ZsfDistricts_FullPipelineSucceedsAndBothTerrainsAppearAcrossManySeeds()
    {
        var model = LoadTileset("zsf01");
        var failures = new List<string>();
        var layoutsWithSecondary = 0;
        var roomsWithSecondary = 0;
        var roomsTotal = 0;
        const int seedCount = 30;

        for (var seed = 30000; seed < 30000 + seedCount; seed++)
        {
            var rng = new Random(seed);
            var parameters = DistrictParameters(model, "floor", "Floor2");

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

            var nonSetPieceRooms = macro.Rooms.Where(r => !r.IsSetPiece).ToList();
            roomsTotal += nonSetPieceRooms.Count;
            var secondaryRooms = nonSetPieceRooms.Count(r => Eq(r.OpenTerrain, "Floor2"));
            roomsWithSecondary += secondaryRooms;
            if (secondaryRooms > 0) layoutsWithSecondary++;

            // Room 0 (entrance-biased) must always stay primary.
            nonSetPieceRooms.Should().Contain(r => r.Id == 0);
            nonSetPieceRooms.First(r => r.Id == 0).OpenTerrain.Should().Be("floor", $"seed {seed}: room 0 must always be the primary district");

            foreach (var room in nonSetPieceRooms)
            {
                if (!RoomInteriorMatchesOwnTerrain(macro, room, out var mismatch))
                    failures.Add($"seed {seed}: {mismatch}");
            }

            macro.Rooms.Should().Contain(r => r.Role == RoomRole.Boss, $"seed {seed} must assign a boss room across districts");

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            AssertEdgeAgreement(model, macro, resolved, seed, failures);

            // Every transition must land on a tile that belongs to some non-set-piece room.
            var roomTiles = new HashSet<(int X, int Y)>(nonSetPieceRooms.SelectMany(r => r.Tiles));
            foreach (var transition in resolved.Transitions)
            {
                if (transition.Style == TransitionStyle.Placeable && !roomTiles.Contains(transition.Tile))
                    failures.Add($"seed {seed}: transition tile {transition.Tile} is not inside any room");
            }
        }

        failures.Should().BeEmpty();
        layoutsWithSecondary.Should().BeGreaterThan(seedCount / 2, "most zsf01 district layouts should carve at least one Floor2 room");
        roomsWithSecondary.Should().BeGreaterThan(0);
        TestContext.WriteLine($"zsf01 districts: {roomsWithSecondary}/{roomsTotal} rooms were Floor2 across {seedCount} seeds, {layoutsWithSecondary} layouts had at least one");
    }

    [Test]
    public void ZsfDistricts_IsDeterministicPerSeed()
    {
        var model = LoadTileset("zsf01");

        (MacroLayout Macro, ResolvedLayout Resolved) Generate()
        {
            var rng = new Random(30500);
            var parameters = DistrictParameters(model, "floor", "Floor2");
            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            macro.Seed = 30500;
            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);
            return (macro, resolved);
        }

        var first = Generate();
        var second = Generate();

        for (var y = 0; y <= first.Macro.Corners.Height; y++)
        for (var x = 0; x <= first.Macro.Corners.Width; x++)
            first.Macro.Corners.Labels[x, y].Should().Be(second.Macro.Corners.Labels[x, y], $"corner ({x},{y})");

        for (var i = 0; i < first.Resolved.Tiles.Length; i++)
        {
            first.Resolved.Tiles[i].TileId.Should().Be(second.Resolved.Tiles[i].TileId, $"cell index {i}");
            first.Resolved.Tiles[i].Orientation.Should().Be(second.Resolved.Tiles[i].Orientation, $"cell index {i}");
        }

        first.Macro.Rooms.Count.Should().Be(second.Macro.Rooms.Count);
        for (var i = 0; i < first.Macro.Rooms.Count; i++)
            first.Macro.Rooms[i].OpenTerrain.Should().Be(second.Macro.Rooms[i].OpenTerrain, $"room {i}");
    }

    /// <summary>
    /// Back-compat: SecondaryOpenTerrain empty must reproduce byte-identical resolution to a baseline
    /// that never sets the field at all -- proves the district roll consumes zero extra RNG and every
    /// new code path is a true no-op when districts are inactive.
    /// </summary>
    [TestCase("zsf01", "floor")]
    [TestCase("vmr01", "Plaza")]
    public void SecondaryOpenTerrain_Empty_MatchesBaselineByteForByte(string tilesetResref, string primaryOpen)
    {
        var model = LoadTileset(tilesetResref);

        for (var seed = 31000; seed < 31015; seed++)
        {
            var withEmptySecondary = DistrictParameters(model, primaryOpen, string.Empty);
            var macroA = MacroLayoutGenerator.Generate(withEmptySecondary, new Random(seed), model);

            var withoutFieldAtAll = new MacroLayoutParameters
            {
                Style = DungeonLayoutStyle.RoomsAndCorridors,
                CorridorMode = CorridorMode.Tunnel,
                CorridorCrosserType = CorridorCrosserType.Corridor,
                MinRooms = 6,
                MaxRooms = 9,
                MinRoomCornerSize = 3,
                MaxRoomCornerSize = 5,
                LoopFactor = 0.3,
                Width = 22,
                Height = 22,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = primaryOpen,
            };
            withoutFieldAtAll.SecondaryOpenTerrain.Should().BeEmpty();
            var macroB = MacroLayoutGenerator.Generate(withoutFieldAtAll, new Random(seed), model);

            for (var y = 0; y <= macroA.Corners.Height; y++)
            for (var x = 0; x <= macroA.Corners.Width; x++)
                macroA.Corners.Labels[x, y].Should().Be(macroB.Corners.Labels[x, y], $"seed {seed} corner ({x},{y})");

            for (var y = 0; y < macroA.Corners.Height; y++)
            for (var x = 0; x < macroA.Corners.Width; x++)
            for (var slot = 0; slot < 4; slot++)
                macroA.Crossers.GetEdge(x, y, slot).Should().Be(macroB.Crossers.GetEdge(x, y, slot), $"seed {seed} cell ({x},{y}) slot {slot}");

            macroA.Rooms.Select(r => r.OpenTerrain).Should().Equal(macroB.Rooms.Select(r => r.OpenTerrain), $"seed {seed}: every room must be the primary terrain");
            macroA.Rooms.Should().OnlyContain(r => Eq(r.OpenTerrain, primaryOpen), $"seed {seed}");
        }
    }

    // ============================================================
    // vmr01: Plaza (primary) + Floor (secondary) districts
    // ============================================================

    [Test]
    public void VmrDistricts_FullPipelineSucceedsAndBothTerrainsAppearAcrossManySeeds()
    {
        var model = LoadTileset("vmr01");
        var failures = new List<string>();
        var layoutsWithSecondary = 0;
        var roomsWithSecondary = 0;
        var roomsTotal = 0;
        const int seedCount = 30;

        for (var seed = 32000; seed < 32000 + seedCount; seed++)
        {
            var rng = new Random(seed);
            var parameters = DistrictParameters(model, "Plaza", "Floor");

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

            var nonSetPieceRooms = macro.Rooms.Where(r => !r.IsSetPiece).ToList();
            roomsTotal += nonSetPieceRooms.Count;
            var secondaryRooms = nonSetPieceRooms.Count(r => Eq(r.OpenTerrain, "Floor"));
            roomsWithSecondary += secondaryRooms;
            if (secondaryRooms > 0) layoutsWithSecondary++;

            nonSetPieceRooms.First(r => r.Id == 0).OpenTerrain.Should().Be("Plaza", $"seed {seed}: room 0 must always be the primary district");

            foreach (var room in nonSetPieceRooms)
            {
                if (!RoomInteriorMatchesOwnTerrain(macro, room, out var mismatch))
                    failures.Add($"seed {seed}: {mismatch}");
            }

            macro.Rooms.Should().Contain(r => r.Role == RoomRole.Boss, $"seed {seed} must assign a boss room across districts");

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            AssertEdgeAgreement(model, macro, resolved, seed, failures);
        }

        failures.Should().BeEmpty();
        layoutsWithSecondary.Should().BeGreaterThan(seedCount / 2, "most vmr01 district layouts should carve at least one Floor room");
        roomsWithSecondary.Should().BeGreaterThan(0);
        TestContext.WriteLine($"vmr01 districts: {roomsWithSecondary}/{roomsTotal} rooms were Floor across {seedCount} seeds, {layoutsWithSecondary} layouts had at least one");
    }

    [Test]
    public void VmrDistricts_IsDeterministicPerSeed()
    {
        var model = LoadTileset("vmr01");

        (MacroLayout Macro, ResolvedLayout Resolved) Generate()
        {
            var rng = new Random(32500);
            var parameters = DistrictParameters(model, "Plaza", "Floor");
            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            macro.Seed = 32500;
            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);
            return (macro, resolved);
        }

        var first = Generate();
        var second = Generate();

        for (var i = 0; i < first.Resolved.Tiles.Length; i++)
        {
            first.Resolved.Tiles[i].TileId.Should().Be(second.Resolved.Tiles[i].TileId, $"cell index {i}");
            first.Resolved.Tiles[i].Orientation.Should().Be(second.Resolved.Tiles[i].Orientation, $"cell index {i}");
        }
    }

    /// <summary>
    /// The Alley crosser vocabulary is verified only for vmr01's Plaza terrain (see
    /// CorridorCrosserType doc comment); districts must stay inactive when composed with Alley, even
    /// though the AncientRuin tileset profile now declares SecondaryOpenTerrain -- proven directly
    /// against RoomsAndCorridorsLayout's useDistricts gate rather than just re-running
    /// FenceAndAlleyTests' existing Streets composition tests.
    /// </summary>
    [Test]
    public void AlleyCrosserType_NeverActivatesDistrictsEvenWithSecondaryOpenTerrainConfigured()
    {
        var model = LoadTileset("vmr01");

        for (var seed = 33000; seed < 33010; seed++)
        {
            var parameters = DistrictParameters(model, "Plaza", "Floor");
            parameters.CorridorCrosserType = CorridorCrosserType.Alley;

            var macro = MacroLayoutGenerator.Generate(parameters, new Random(seed), model);

            macro.Rooms.Where(r => !r.IsSetPiece).Should().OnlyContain(r => Eq(r.OpenTerrain, "Plaza"),
                $"seed {seed}: Alley mode must never carve a Floor district (unverified Alley-port vocabulary for Floor)");
        }
    }

    // ============================================================
    // Group stamper cross-district: vmr01 InteriorMosaic_2x2 only stamps into Floor rooms
    // ============================================================

    [Test]
    public void InteriorMosaic_OnlyStampsIntoFloorDistrictRoomsNeverPlaza()
    {
        var model = LoadTileset("vmr01");
        var mosaicGroup = model.Groups.First(g => string.Equals(g.Name, "InteriorMosaic_2x2", StringComparison.OrdinalIgnoreCase));
        var mosaicTileIds = new HashSet<int>(mosaicGroup.TileIds.Where(id => id >= 0));

        var totalStamped = 0;
        var failures = new List<string>();

        for (var seed = 34000; seed < 34040; seed++)
        {
            var rng = new Random(seed);
            var parameters = DistrictParameters(model, "Plaza", "Floor", width: 34, secondaryFraction: 0.5);
            // InteriorMosaic_2x2 needs its 2x2 footprint PLUS a 1-cell margin ring (a 4x4 block)
            // entirely inside one room's own rectangle and clear of that room's single center tile
            // (LayoutGroupStamper.IsOpenSetPieceSiteValid). For a rectangular RoomsAndCorridors room,
            // a 4-wide window can only dodge a centered point when the room is at least ~9 tiles along
            // that axis (verified offline by brute force: 5x6 and 7x7 rooms can NEVER produce a valid
            // anchor -- the center point falls inside every possible 4-wide window along either axis).
            // Widen the room size band well past the default so Floor district rooms are large enough.
            parameters.MinRooms = 5;
            parameters.MaxRooms = 7;
            parameters.MinRoomCornerSize = 6;
            parameters.MaxRoomCornerSize = 10;
            // Bump maxPerArea well above the default (1) so 40 seeds give the stamper a realistic shot
            // at demonstrating the cross-district restriction, mirroring FenceAndAlleyTests' own
            // maxPerArea-bumping convention for low-probability-per-seed set pieces.
            parameters.SetPieces = new Dictionary<string, int> { ["InteriorMosaic_2x2"] = 4 };

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            var roomsById = macro.Rooms.ToDictionary(r => r.Id);

            foreach (var (cell, pin) in macro.PinnedTiles)
            {
                if (!mosaicTileIds.Contains(pin.TileId)) continue;
                totalStamped++;

                // OpenSetPiece writes corners directly; verify the actual grid corners at this cell are
                // Floor, never Plaza (the structural proof, independent of room bookkeeping).
                var tl = macro.Corners.Labels[cell.X, cell.Y + 1];
                var tr = macro.Corners.Labels[cell.X + 1, cell.Y + 1];
                var br = macro.Corners.Labels[cell.X + 1, cell.Y];
                var bl = macro.Corners.Labels[cell.X, cell.Y];

                if (new[] { tl, tr, br, bl }.Any(c => Eq(c, "Plaza")))
                    failures.Add($"seed {seed}: InteriorMosaic cell {cell} has a Plaza corner: [{tl},{tr},{br},{bl}]");
            }
        }

        failures.Should().BeEmpty();
        totalStamped.Should().BeGreaterThan(0, "at least some of 40 vmr01 district seeds should stamp an InteriorMosaic_2x2 tile into a Floor room");
    }

    // ============================================================
    // Full-pipeline sweep across all four tilesets with production defaults
    // ============================================================

    [TestCase(StandardTilesetProfiles.Facility, StandardLayoutProfiles.Complex)]
    [TestCase(StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Organic)]
    [TestCase(StandardTilesetProfiles.Sewers, StandardLayoutProfiles.Warren)]
    [TestCase(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls)]
    public void FullPipelineSweep_ProductionDefaultsStillSucceedWithSecondaryOpenTerrainStamped(string tilesetKey, string layoutKey)
    {
        var tilesetProfile = new StandardTilesetProfiles().BuildTilesetProfiles()[tilesetKey];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[layoutKey];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        const int retryBudget = 6;
        var failures = new List<string>();
        var districtedLayouts = 0;

        for (var seedBase = 35000; seedBase < 35015; seedBase++)
        {
            var succeeded = false;
            var lastFailure = "no attempts made";

            for (var attempt = 0; attempt < retryBudget && !succeeded; attempt++)
            {
                var trySeed = seedBase + attempt;
                var rng = new Random(trySeed);

                var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
                var parameters = composition.BuildLayoutParameters();
                parameters.Width = 22;
                parameters.Height = 22;
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

                if (macro.Rooms.Any(r => !r.IsSetPiece && !Eq(r.OpenTerrain, parameters.OpenTerrain)))
                    districtedLayouts++;

                if (TileResolver.TryResolve(model, macro, rng, out _, out var reason))
                    succeeded = true;
                else
                    lastFailure = reason;
            }

            if (!succeeded)
                failures.Add($"{tilesetKey}/{layoutKey} seed base {seedBase}: {lastFailure}");
        }

        failures.Should().BeEmpty();

        // Facility's shipped default pairing (Complex = Tunnel mode) is the one composition where
        // districts activate automatically today; every other shipped default pairing stays OpenLane
        // (or, for AncientRuin/Halls, Tunnel mode is simply never selected), so districts must never
        // silently appear there.
        if (tilesetKey == StandardTilesetProfiles.Facility)
            districtedLayouts.Should().BeGreaterThan(0, "Facility's default Complex/Tunnel pairing should activate districts automatically");
        else
            districtedLayouts.Should().Be(0, $"{tilesetKey}/{layoutKey} is not a Tunnel-mode district pairing and must never carve a secondary room");
    }

    [Test]
    public void FacilityProfile_DeclaresSecondaryOpenTerrainFloor2()
    {
        var profiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        profiles[StandardTilesetProfiles.Facility].SecondaryOpenTerrain.Should().Be("Floor2");
        profiles[StandardTilesetProfiles.AncientRuin].SecondaryOpenTerrain.Should().Be("Floor");
        profiles[StandardTilesetProfiles.Cavern].SecondaryOpenTerrain.Should().BeEmpty();
        profiles[StandardTilesetProfiles.Sewers].SecondaryOpenTerrain.Should().BeEmpty();
    }
}
