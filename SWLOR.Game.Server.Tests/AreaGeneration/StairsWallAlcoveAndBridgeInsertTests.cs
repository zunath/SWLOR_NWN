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
/// Closing-the-tail mechanisms added for 100% tile-coverage: LayoutGroupStamper's new CorridorStub
/// kind (dead-end stairs spliced onto an existing Tunnel-mode chain), WallAlcove kind (vmr01
/// "Room 1-5 2x2" doorframed wall chambers with no Doorway crosser of their own), the OpenSetPiece
/// door-slot tolerance (2x2 stairs/tower pieces), and the CorridorInsert Bridge-crosser extension
/// (BridgeDoor/BridgeDoor01 splicing into an accent channel span). Runs the full pipeline against
/// every generation tileset's real .set data, mirroring GroupStamperTests' structure.
/// </summary>
public class StairsWallAlcoveAndBridgeInsertTests
{
    private static readonly Dictionary<string, string> TilesetHakDirectories = new()
    {
        ["tdt01"] = "sw_t_minecave",
        ["zsf01"] = "sw_t_scifibase",
        ["tds01"] = "sw_t_sewer",
        ["vmr01"] = "sw_t_alienruin",
    };

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

    private static Dictionary<string, DungeonTilesetProfile> Profiles() => new StandardTilesetProfiles().BuildTilesetProfiles();
    private static Dictionary<string, DungeonLayoutProfile> Layouts() => new StandardLayoutProfiles().BuildLayoutProfiles();

    /// <summary>
    /// Counts how many pinned cells across a seed range resolve to any of the given tile IDs (looked
    /// up from the group's own TileIds), skipping generation/resolution failures the way every other
    /// AreaGeneration test file does.
    /// </summary>
    private static int CountPinnedTileIds(
        TilesetModel model, DungeonComposition composition, int width, int height,
        HashSet<int> tileIds, int seedStart, int seedCount, List<string> failures)
    {
        var placed = 0;

        for (var seed = seedStart; seed < seedStart + seedCount; seed++)
        {
            var rng = new Random(seed);
            var parameters = composition.BuildLayoutParameters();
            parameters.Width = width;
            parameters.Height = height;
            parameters.SolidTerrain = model.DefaultTerrain;
            parameters.OpenTerrain = string.IsNullOrEmpty(composition.Tileset.PrimaryOpenTerrain)
                ? model.FloorTerrain
                : composition.Tileset.PrimaryOpenTerrain;

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

            foreach (var tile in resolved.Tiles)
            {
                if (tileIds.Contains(tile.TileId))
                    placed++;
            }
        }

        return placed;
    }

    // ---------------- CorridorStub (dead-end stairs on a Tunnel-mode chain) ----------------

    [Test]
    public void CorridorStub_FacilityDefaultCompositionPlacesStairsUpAndDown()
    {
        var model = LoadTileset("zsf01");
        var tileset = Profiles()[StandardTilesetProfiles.Facility];
        var composition = new DungeonComposition { Tileset = tileset, Layout = Layouts()[StandardLayoutProfiles.Complex] };

        var stairTileIds = new HashSet<int> { GroupTileId(model, "StairsUP"), GroupTileId(model, "StairsDOWN") };
        var failures = new List<string>();
        var placed = CountPinnedTileIds(model, composition, 24, 24, stairTileIds, 20000, 40, failures);

        failures.Should().BeEmpty();
        placed.Should().BeGreaterThan(0, "Facility's own default Complex (Tunnel) pairing should splice at least one CorridorStub stair across 40 seeds");
    }

    [Test]
    public void CorridorStub_CavernComplexCompositionPlacesStairsDownAndUp()
    {
        var model = LoadTileset("tdt01");
        var tileset = Profiles()[StandardTilesetProfiles.Cavern];
        var composition = new DungeonComposition { Tileset = tileset, Layout = Layouts()[StandardLayoutProfiles.Complex] };

        var stairTileIds = new HashSet<int> { GroupTileId(model, "StairsDown01"), GroupTileId(model, "StairsUp01") };
        var failures = new List<string>();
        var placed = CountPinnedTileIds(model, composition, 24, 24, stairTileIds, 20100, 40, failures);

        failures.Should().BeEmpty();
        placed.Should().BeGreaterThan(0, "Cavern composed with Complex (Tunnel) should splice at least one CorridorStub stair across 40 seeds");
    }

    [Test]
    public void CorridorStub_AncientRuinStreetsCompositionPlacesExteriorStairs()
    {
        var model = LoadTileset("vmr01");
        var tileset = Profiles()[StandardTilesetProfiles.AncientRuin];
        var composition = new DungeonComposition { Tileset = tileset, Layout = Layouts()[StandardLayoutProfiles.Streets] };

        var stairTileIds = new HashSet<int> { GroupTileId(model, "ExteriorStairsDown"), GroupTileId(model, "ExteriorStairsUp") };
        var failures = new List<string>();
        var placed = CountPinnedTileIds(model, composition, 24, 24, stairTileIds, 20200, 40, failures);

        failures.Should().BeEmpty();
        placed.Should().BeGreaterThan(0, "AncientRuin composed with Streets (Alley Tunnel) should splice at least one CorridorStub exterior stair across 40 seeds");
    }

    // ---------------- WallAlcove (vmr01 Room 1-5 2x2) ----------------

    [Test]
    public void WallAlcove_AncientRuinDefaultCompositionPlacesRoomAlcoves()
    {
        var model = LoadTileset("vmr01");
        var tileset = Profiles()[StandardTilesetProfiles.AncientRuin];
        var composition = new DungeonComposition { Tileset = tileset, Layout = Layouts()[StandardLayoutProfiles.Halls] };

        var roomGroupNames = new[] { "Room 1 2x2", "Room 2 2x2", "Room 3 2x2", "Room 4 2x2", "Room 5 2x2" };
        var roomTileIds = new HashSet<int>();
        foreach (var name in roomGroupNames)
            foreach (var id in GroupTileIds(model, name))
                roomTileIds.Add(id);

        var failures = new List<string>();
        var placed = CountPinnedTileIds(model, composition, 28, 28, roomTileIds, 20300, 40, failures);

        failures.Should().BeEmpty();
        placed.Should().BeGreaterThan(0, "AncientRuin's own default Halls (OpenLane) pairing should stamp at least one WallAlcove Room piece across 40 seeds");
    }

    // ---------------- OpenSetPiece door-slot tolerance (2x2 stairs/tower) ----------------

    [Test]
    public void OpenSetPieceDoorTolerance_CavernCompositionPlaces2x2Stairs()
    {
        // A 2x2 OpenSetPiece needs a full 4x4 clear zone (footprint + 1-cell margin) entirely inside
        // one room's tracked open tiles (IsOpenSetPieceSiteValid) — Organic-cave-shaped rooms are
        // irregular blobs that rarely clear that much room at the default OpenFillTarget/room-count
        // tuning (the existing GroupStamperTests.OpenSetPiece_LandsInsideRoomsWithMarginAndRemovesFootprintFromRoomTiles
        // proves overall OpenSetPiece placement via smaller pieces, not specifically a 2x2 with margin).
        // Bigger rectangular rooms reliably clear it, so this uses a generous RoomsAndCorridors/OpenLane
        // composition (same style/mechanism, larger room-size knobs) to prove the mechanism places —
        // still a "supported configuration" in the same sense as StandardLayoutProfiles.Halls/Complex.
        var model = LoadTileset("tdt01");
        var setPieces = Profiles()[StandardTilesetProfiles.Cavern].SetPieces;

        var stairTileIds = new HashSet<int>();
        foreach (var id in GroupTileIds(model, "StairsDown_2x2")) stairTileIds.Add(id);
        foreach (var id in GroupTileIds(model, "StairsUp_2x2")) stairTileIds.Add(id);

        var failures = new List<string>();
        var placed = 0;

        for (var seed = 20400; seed < 20440; seed++)
        {
            var rng = new Random(seed);
            var parameters = new MacroLayoutParameters
            {
                Style = DungeonLayoutStyle.RoomsAndCorridors,
                MinRooms = 4,
                MaxRooms = 6,
                MinRoomCornerSize = 6,
                MaxRoomCornerSize = 9,
                CorridorWidth = 2,
                LoopFactor = 0.3,
                Width = 30,
                Height = 30,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = model.FloorTerrain,
                SetPieces = setPieces,
            };

            MacroLayout macro;
            try { macro = MacroLayoutGenerator.Generate(parameters, rng, model); }
            catch (InvalidOperationException ex) { failures.Add($"seed {seed}: generation failed: {ex.Message}"); continue; }
            macro.Seed = seed;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            foreach (var tile in resolved.Tiles)
                if (stairTileIds.Contains(tile.TileId)) placed++;
        }

        failures.Should().BeEmpty();
        placed.Should().BeGreaterThan(0, "a generous large-room RoomsAndCorridors composition should stamp at least one 2x2 stairs OpenSetPiece across 40 seeds");
    }

    [Test]
    public void OpenSetPieceDoorTolerance_AncientRuinCompositionPlacesExteriorStairsAndTower()
    {
        // See OpenSetPieceDoorTolerance_CavernCompositionPlaces2x2Stairs: a generous large-room
        // composition reliably clears the 4x4 margin a 2x2 OpenSetPiece needs.
        var model = LoadTileset("vmr01");
        var setPieces = Profiles()[StandardTilesetProfiles.AncientRuin].SetPieces;

        var pieceTileIds = new HashSet<int>();
        foreach (var name in new[] { "ExteriorStairsDown_2x2", "ExteriorStairsUp_2x2", "ExteriorRuinedTower_2x2" })
            foreach (var id in GroupTileIds(model, name))
                pieceTileIds.Add(id);

        var failures = new List<string>();
        var placed = 0;

        for (var seed = 20500; seed < 20540; seed++)
        {
            var rng = new Random(seed);
            var parameters = new MacroLayoutParameters
            {
                Style = DungeonLayoutStyle.RoomsAndCorridors,
                MinRooms = 4,
                MaxRooms = 6,
                MinRoomCornerSize = 6,
                MaxRoomCornerSize = 9,
                CorridorWidth = 2,
                LoopFactor = 0.3,
                Width = 30,
                Height = 30,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = "Plaza",
                SetPieces = setPieces,
            };

            MacroLayout macro;
            try { macro = MacroLayoutGenerator.Generate(parameters, rng, model); }
            catch (InvalidOperationException ex) { failures.Add($"seed {seed}: generation failed: {ex.Message}"); continue; }
            macro.Seed = seed;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            foreach (var tile in resolved.Tiles)
                if (pieceTileIds.Contains(tile.TileId)) placed++;
        }

        failures.Should().BeEmpty();
        placed.Should().BeGreaterThan(0, "a generous large-room RoomsAndCorridors composition should stamp at least one 2x2 exterior stairs/tower OpenSetPiece across 40 seeds");
    }

    // ---------------- CorridorInsert Bridge extension (BridgeDoor / BridgeDoor01) ----------------

    [Test]
    public void BridgeInsert_CavernOrganicCompositionSplicesBridgeDoor()
    {
        var model = LoadTileset("tdt01");
        var tileset = Profiles()[StandardTilesetProfiles.Cavern];
        var composition = new DungeonComposition { Tileset = tileset, Layout = Layouts()[StandardLayoutProfiles.Organic] };

        var bridgeDoorId = GroupTileId(model, "BridgeDoor");
        var failures = new List<string>();
        var placed = CountPinnedTileIds(model, composition, 28, 28, new HashSet<int> { bridgeDoorId }, 20600, 60, failures);

        failures.Should().BeEmpty();
        placed.Should().BeGreaterThan(0, "Cavern's own default Organic pairing (Water channels active) should splice at least one BridgeDoor across 60 seeds");
    }

    [Test]
    public void BridgeInsert_AncientRuinDefaultCompositionSplicesBridgeDoor()
    {
        var model = LoadTileset("vmr01");
        var tileset = Profiles()[StandardTilesetProfiles.AncientRuin];
        var composition = new DungeonComposition { Tileset = tileset, Layout = Layouts()[StandardLayoutProfiles.Halls] };

        var bridgeDoorId = GroupTileId(model, "BridgeDoor01");
        var failures = new List<string>();
        var placed = CountPinnedTileIds(model, composition, 28, 28, new HashSet<int> { bridgeDoorId }, 20700, 60, failures);

        failures.Should().BeEmpty();
        placed.Should().BeGreaterThan(0, "AncientRuin's own default Halls pairing (Chasm channels now active via ChannelTerrain) should splice at least one BridgeDoor01 across 60 seeds");
    }


    // ---------------- helpers ----------------

    private static int GroupTileId(TilesetModel model, string groupName)
    {
        var group = model.Groups.First(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
        return group.TileIds[0];
    }

    private static List<int> GroupTileIds(TilesetModel model, string groupName)
    {
        var group = model.Groups.First(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
        return group.TileIds.Where(id => id >= 0).ToList();
    }
}
