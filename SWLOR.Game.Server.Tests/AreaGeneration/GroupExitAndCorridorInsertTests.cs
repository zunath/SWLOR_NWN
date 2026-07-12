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
/// Corridor inserts (LayoutGroupStamper's new CorridorInsert classification, e.g. tdt01/tds01
/// BigDoor01/02) and the GroupExit transition style (GroupExitPlanner, e.g. tdt01 Exit01-03) — run
/// against every generation tileset's real .set data, same conventions as GroupStamperTests /
/// TileDoorPlannerTests.
/// </summary>
public class GroupExitAndCorridorInsertTests
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

    private static int GroupTileId(TilesetModel model, string groupName)
    {
        var group = model.Groups.First(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
        group.Rows.Should().Be(1);
        group.Columns.Should().Be(1);
        return group.TileIds[0];
    }

    private static bool IsFullyOpenCell(MacroLayout macro, (int X, int Y) tile, string openTerrain)
    {
        var corners = macro.Corners;
        return string.Equals(corners.Labels[tile.X, tile.Y], openTerrain, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(corners.Labels[tile.X + 1, tile.Y], openTerrain, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(corners.Labels[tile.X, tile.Y + 1], openTerrain, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(corners.Labels[tile.X + 1, tile.Y + 1], openTerrain, StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // Corridor inserts (LayoutGroupStamper CorridorInsert classification)
    // ============================================================

    private static MacroLayoutParameters TunnelParametersWithBigDoor(TilesetModel model)
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
            Width = 22,
            Height = 22,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = model.FloorTerrain,
            SetPieces = new Dictionary<string, int> { ["BigDoor01"] = 1, ["BigDoor02"] = 1 }
        };
    }

    [TestCase("tdt01")]
    [TestCase("tds01")]
    public void CorridorInsert_PinsAtStraightTunnelSegmentWithMatchingOrientation(string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);
        var bigDoor01Id = GroupTileId(model, "BigDoor01");
        var bigDoor02Id = GroupTileId(model, "BigDoor02");
        var insertTileIds = new HashSet<int> { bigDoor01Id, bigDoor02Id };
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

        var failures = new List<string>();
        var totalInsertPins = 0;

        for (var seed = 9200; seed < 9215; seed++)
        {
            var rng = new Random(seed);
            var parameters = TunnelParametersWithBigDoor(model);

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

            var insertPins = macro.PinnedTiles.Where(p => insertTileIds.Contains(p.Value.TileId)).ToList();
            totalInsertPins += insertPins.Count;

            foreach (var (cell, pin) in insertPins)
            {
                var record = tilesById[pin.TileId];

                // Resolved grid must place exactly the pinned (tileId, orientation) verbatim.
                var resolvedTile = resolved.GetTile(cell.X, cell.Y);
                if (resolvedTile.TileId != pin.TileId || resolvedTile.Orientation != pin.Orientation)
                {
                    failures.Add($"seed {seed}: insert pin at {cell} expected TILE{pin.TileId} o={pin.Orientation} but resolved TILE{resolvedTile.TileId} o={resolvedTile.Orientation}");
                }

                // All 4 corners must be solid (the classification's own requirement).
                for (var slot = 0; slot < 4; slot++)
                {
                    var corner = record.GetCornerAt(pin.Orientation, slot);
                    if (!string.Equals(corner, model.DefaultTerrain, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"seed {seed}: insert pin at {cell} corner slot {slot} is '{corner}', expected solid '{model.DefaultTerrain}'");
                }

                // The pinned orientation's 4 edges must match the crosser plan exactly at this cell —
                // this is the "orientation aligns the insert's Corridor pair with the plan" guarantee.
                var corridorSlots = new List<int>();
                for (var slot = 0; slot < 4; slot++)
                {
                    var actual = record.GetEdgeAt(pin.Orientation, slot) ?? string.Empty;
                    var planned = macro.Crossers.GetEdge(cell.X, cell.Y, slot) ?? string.Empty;
                    if (!string.Equals(actual, planned, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"seed {seed}: insert pin at {cell} slot {slot}: tile data says '{actual}' but crosser plan says '{planned}'");

                    if (string.Equals(actual, "Corridor", StringComparison.OrdinalIgnoreCase))
                        corridorSlots.Add(slot);
                }

                if (corridorSlots.Count != 2 || Math.Abs(corridorSlots[0] - corridorSlots[1]) != 2)
                    failures.Add($"seed {seed}: insert pin at {cell} does not carry exactly one opposite Corridor pair (slots: {string.Join(",", corridorSlots)})");
            }
        }

        failures.Should().BeEmpty();
        totalInsertPins.Should().BeGreaterThan(0, $"at least some of 15 {tilesetResref} tunnel seeds should stamp a BigDoor corridor insert");
    }

    [Test]
    public void CorridorInsert_IsDeterministicPerSeed()
    {
        var model = LoadTileset("tdt01");

        ResolvedLayout Resolve(out MacroLayout macro)
        {
            var rng = new Random(9300);
            var parameters = TunnelParametersWithBigDoor(model);
            macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            macro.Seed = 9300;
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

    // ============================================================
    // Group exits (GroupExitPlanner / TransitionStyle.GroupExit)
    // ============================================================

    private static MacroLayoutParameters ExitGroupParameters(TilesetModel model, int width, int height, int minRooms, int maxRooms, int exitCount, List<string> exitGroups, int minRoomCornerSize = 3, int maxRoomCornerSize = 7)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            Width = width,
            Height = height,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = model.FloorTerrain,
            MinRooms = minRooms,
            MaxRooms = maxRooms,
            MinRoomCornerSize = minRoomCornerSize,
            MaxRoomCornerSize = maxRoomCornerSize,
            EntranceCount = 1,
            ExitCount = exitCount,
            ExitGroups = exitGroups
        };
    }

    [Test]
    public void GroupExit_ProducesValidPinsAndDoorTransforms()
    {
        var model = LoadTileset("tdt01");
        var exitGroups = new List<string> { "Exit01", "Exit02", "Exit03" };
        var exitTileIds = new HashSet<int> { GroupTileId(model, "Exit01"), GroupTileId(model, "Exit02"), GroupTileId(model, "Exit03") };
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

        var failures = new List<string>();
        var totalGroupExits = 0;
        var totalDoors = 0;
        var totalPlaceables = 0;

        for (var seed = 9500; seed < 9515; seed++)
        {
            var rng = new Random(seed);
            var parameters = ExitGroupParameters(model, 20, 20, 6, 9, 3, exitGroups);

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng);
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

            var exitTransitions = resolved.Transitions.Where(t => t.Kind == TransitionKind.Exit).ToList();
            var claimedPins = new HashSet<(int X, int Y)>();

            foreach (var transition in exitTransitions)
            {
                switch (transition.Style)
                {
                    case TransitionStyle.GroupExit:
                        totalGroupExits++;

                        if (!macro.PinnedTiles.TryGetValue(transition.DoorCell, out var pin))
                        {
                            failures.Add($"seed {seed}: GroupExit transition's DoorCell {transition.DoorCell} is not pinned");
                            break;
                        }

                        if (!claimedPins.Add(transition.DoorCell))
                            failures.Add($"seed {seed}: DoorCell {transition.DoorCell} claimed by more than one transition");

                        if (!exitTileIds.Contains(pin.TileId))
                            failures.Add($"seed {seed}: GroupExit pin at {transition.DoorCell} uses TILE{pin.TileId}, not a configured exit group tile");

                        var resolvedDoorTile = resolved.GetTile(transition.DoorCell.X, transition.DoorCell.Y);
                        if (resolvedDoorTile.TileId != pin.TileId || resolvedDoorTile.Orientation != pin.Orientation)
                            failures.Add($"seed {seed}: GroupExit pin at {transition.DoorCell} disagrees with resolved grid");

                        var record = tilesById[pin.TileId];
                        record.Doors.Should().NotBeEmpty($"seed {seed}: exit group tile at {transition.DoorCell} must carry a door slot");

                        // Corners at the pinned orientation must match the corner grid exactly.
                        var expectedTl = macro.Corners.Labels[transition.DoorCell.X, transition.DoorCell.Y + 1];
                        var expectedTr = macro.Corners.Labels[transition.DoorCell.X + 1, transition.DoorCell.Y + 1];
                        var expectedBr = macro.Corners.Labels[transition.DoorCell.X + 1, transition.DoorCell.Y];
                        var expectedBl = macro.Corners.Labels[transition.DoorCell.X, transition.DoorCell.Y];
                        record.GetCornerAt(pin.Orientation, CornerSlot.TopLeft).Should().Be(expectedTl, $"seed {seed} cell {transition.DoorCell} TL");
                        record.GetCornerAt(pin.Orientation, CornerSlot.TopRight).Should().Be(expectedTr, $"seed {seed} cell {transition.DoorCell} TR");
                        record.GetCornerAt(pin.Orientation, CornerSlot.BottomRight).Should().Be(expectedBr, $"seed {seed} cell {transition.DoorCell} BR");
                        record.GetCornerAt(pin.Orientation, CornerSlot.BottomLeft).Should().Be(expectedBl, $"seed {seed} cell {transition.DoorCell} BL");

                        // The relocated Tile (where players interact from) must be genuinely open floor.
                        if (!IsFullyOpenCell(macro, transition.Tile, model.FloorTerrain))
                            failures.Add($"seed {seed}: GroupExit relocated Tile {transition.Tile} is not fully open floor");

                        break;

                    case TransitionStyle.Door:
                        totalDoors++;
                        break;

                    case TransitionStyle.Placeable:
                        totalPlaceables++;
                        break;
                }
            }
        }

        TestContext.WriteLine($"GroupExit={totalGroupExits}, Door={totalDoors}, Placeable={totalPlaceables}");
        failures.Should().BeEmpty();
        totalGroupExits.Should().BeGreaterThan(0, "at least some of 15 seeds should place a GroupExit-style transition");
    }

    [Test]
    public void GroupExit_UnresolvableExitGroupNamesFallBackToDoorForEveryExit()
    {
        // Real exit-group tiles match a plain rectangular room wall so reliably that empty-map space
        // pressure alone can't force a fallback (GroupExitPlanner's per-transition ring search finds
        // a distinct cell almost every time). Instead this proves the actual fallback WIRING: when
        // every configured name is structurally unusable (typos, or a name genuinely absent from
        // this tileset), GroupExitPlanner must place nothing at all and every Exit transition must
        // still resolve via TileDoorPlanner (a real door) exactly as if ExitGroups had been empty —
        // i.e. GroupExitPlanner failing soft never blocks the existing Door/Placeable chain.
        var model = LoadTileset("tdt01");
        var unusableExitGroups = new List<string> { "NotARealExitGroup", "AlsoNotReal" };

        var failures = new List<string>();
        var totalDoors = 0;

        for (var seed = 9600; seed < 9615; seed++)
        {
            var rng = new Random(seed);
            var parameters = ExitGroupParameters(model, 20, 20, 6, 9, 3, unusableExitGroups);

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng);
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

            foreach (var transition in resolved.Transitions.Where(t => t.Kind == TransitionKind.Exit))
            {
                if (transition.Style == TransitionStyle.GroupExit)
                    failures.Add($"seed {seed}: transition became GroupExit despite no resolvable exit-group name");
                else if (transition.Style == TransitionStyle.Door)
                    totalDoors++;
            }
        }

        failures.Should().BeEmpty();
        totalDoors.Should().BeGreaterThan(0, "tdt01 is door-capable, so unusable exit groups must still fall back to real doors");
    }

    [Test]
    public void GroupExit_IsDeterministicPerSeed()
    {
        var model = LoadTileset("tds01");
        var exitGroups = new List<string> { "Exit01", "Exit02" };

        ResolvedLayout Resolve()
        {
            var rng = new Random(9700);
            var parameters = ExitGroupParameters(model, 20, 20, 6, 9, 3, exitGroups);
            var macro = MacroLayoutGenerator.Generate(parameters, rng);
            macro.Seed = 9700;
            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);
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
    public void Zsf01Facility_HasNoExitGroupsConfigured_NeverProducesGroupExitTransitions()
    {
        var model = LoadTileset("zsf01");
        var tilesetProfile = new StandardTilesetProfiles().BuildTilesetProfiles()[StandardTilesetProfiles.Facility];
        tilesetProfile.ExitGroups.Should().BeEmpty("zsf01 has no verified exit-group tile inventory");

        for (var seed = 9700; seed < 9706; seed++)
        {
            var rng = new Random(seed);
            var parameters = ExitGroupParameters(model, 20, 20, 4, 6, 3, tilesetProfile.ExitGroups);
            parameters.OpenTerrain = tilesetProfile.PrimaryOpenTerrain;

            var macro = MacroLayoutGenerator.Generate(parameters, rng);
            macro.Seed = seed;

            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);

            resolved.Transitions.Should().NotContain(t => t.Style == TransitionStyle.GroupExit,
                $"seed {seed}: facility has no configured exit groups");
        }
    }

    [Test]
    public void TilesetProfiles_DeclareVerifiedExitGroupsAndCorridorInserts()
    {
        var profiles = new StandardTilesetProfiles().BuildTilesetProfiles();

        profiles[StandardTilesetProfiles.Cavern].ExitGroups.Should().Equal("Exit01", "Exit02", "Exit03");
        profiles[StandardTilesetProfiles.Sewers].ExitGroups.Should().Equal("Exit01", "Exit02");
        profiles[StandardTilesetProfiles.AncientRuin].ExitGroups.Should().Equal("ExteriorExit01", "ExteriorExit02");
        profiles[StandardTilesetProfiles.Facility].ExitGroups.Should().BeEmpty();

        profiles[StandardTilesetProfiles.Cavern].SetPieces.Should().ContainKeys("BigDoor01", "BigDoor02");
        profiles[StandardTilesetProfiles.Sewers].SetPieces.Should().ContainKeys("BigDoor01", "BigDoor02");
        profiles[StandardTilesetProfiles.AncientRuin].SetPieces.Should().ContainKey("InteriorHallDoor");
    }
}
