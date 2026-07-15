using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// ROAD (LayoutRoadCarver / RoadVocabularyCheck) system: street-style lanes carved between transition
/// anchors and room centers, through open space, AFTER LayoutGroupStamper has already stamped set
/// pieces -- see LayoutRoadCarver's own class doc comment for why it runs last and how it uses
/// PinnedTiles as its "occupied by a building" signal. Same conventions as BridgeChannelTests/
/// FenceAndAlleyTests: run the full pipeline against real fcx01 .set data across many seeds.
/// </summary>
public class RoadCarverTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static readonly Dictionary<string, DungeonTilesetProfile> OnboardedProfiles =
        new BaseGameTilesetProfiles().BuildTilesetProfiles();

    // ============================================================
    // RoadVocabularyCheck: shape coverage, not just crosser-name presence.
    // ============================================================

    [Test]
    public void SupportsRoads_fcx01Cobble_AllFiveShapesPresent()
    {
        var model = LoadTileset("fcx01");
        RoadVocabularyCheck.SupportsRoads(model, "Cobble", "Routes").Should().BeTrue();
    }

    [Test]
    public void SupportsRoads_fcx01Cobble2_AllFiveShapesPresent()
    {
        var model = LoadTileset("fcx01");
        RoadVocabularyCheck.SupportsRoads(model, "Cobble2", "Routes").Should().BeTrue();
    }

    [Test]
    public void SupportsRoads_EmptyCrosser_ReturnsFalse()
    {
        var model = LoadTileset("fcx01");
        RoadVocabularyCheck.SupportsRoads(model, "Cobble", "").Should().BeFalse();
    }

    [Test]
    public void SupportsRoads_UndeclaredCrosserName_ReturnsFalse()
    {
        var model = LoadTileset("fcx01");
        RoadVocabularyCheck.SupportsRoads(model, "Cobble", "NotARealCrosser").Should().BeFalse();
    }

    /// <summary>A tileset with no "Routes"-named crosser at all (e.g. tds01, canonical Corridor/Doorway/
    /// Bridge/Fence vocabulary only) must never claim road support.</summary>
    [Test]
    public void SupportsRoads_TilesetWithoutRoutesCrosser_ReturnsFalse()
    {
        var model = LoadTileset("tds01");
        RoadVocabularyCheck.SupportsRoads(model, model.FloorTerrain, "Routes").Should().BeFalse();
    }

    // ============================================================
    // Full pipeline against the real onboarded FutCity/FutCityPlaza compositions.
    // ============================================================

    private static MacroLayoutParameters FutCityParameters(DungeonTilesetProfile profile, int width = 24)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel, // downgrades to OpenLane for fcx01 -- see MacroLayoutGenerator
            MinRooms = 6,
            MaxRooms = 9,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            CorridorWidth = Math.Max(2, profile.MinimumOpeningWidth),
            LoopFactor = 0.3,
            Width = width,
            Height = width,
            SolidTerrain = profile.SolidTerrainOverride,
            OpenTerrain = profile.PrimaryOpenTerrain,
            SetPieces = profile.SetPieces,
            ExitGroups = profile.ExitGroups,
            FeatureTiles = profile.FeatureTiles,
            DoorSlotCrossers = profile.DoorSlotCrossers,
            RoadLanes = 6,
            RoadCrosser = profile.RoadCrosser,
        };
    }

    private static IEnumerable<((int X, int Y) Cell, int Slot)> AllRoadEdges(MacroLayout macro, int width, int height, string road)
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        for (var slot = 0; slot < 4; slot++)
        {
            if (string.Equals(macro.Crossers.GetEdge(x, y, slot), road, StringComparison.OrdinalIgnoreCase))
                yield return ((x, y), slot);
        }
    }

    [TestCase(BaseGameTilesetProfiles.FutCity, "fcx01")]
    [TestCase(BaseGameTilesetProfiles.FutCityPlaza, "fcx01")]
    public void RoadLanes_FullPipelineSucceedsAcrossManySeeds(string profileKey, string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);
        var profile = OnboardedProfiles[profileKey];
        var failures = new List<string>();
        var seedsWithRoad = 0;
        const int seedCount = 30;

        for (var seed = 30000; seed < 30000 + seedCount; seed++)
        {
            var rng = new Random(seed);
            var parameters = FutCityParameters(profile);

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

            var roadEdges = AllRoadEdges(macro, parameters.Width, parameters.Height, "Routes").ToList();
            if (roadEdges.Count > 0) seedsWithRoad++;

            if (!TileResolver.TryResolve(model, macro, rng, out _, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
            }
        }

        failures.Should().BeEmpty();
        seedsWithRoad.Should().BeGreaterThan(0, $"{profileKey} should place at least one Road lane across {seedCount} seeds");
        TestContext.WriteLine($"{profileKey}: {seedsWithRoad}/{seedCount} seeds produced a Road lane, 0 resolution failures");
    }

    /// <summary>
    /// Every carved Road segment is part of a connected chain between two real anchors (a transition's
    /// interior tile, or a room's own CenterTile) -- LayoutRoadCarver only ever commits an entire
    /// anchor-to-anchor lane atomically, so this must hold by construction; this test proves it holds
    /// for real generated layouts rather than trusting the construction argument alone. Also proves the
    /// "routes around buildings" property: no Road-crossed cell is ever one LayoutGroupStamper already
    /// pinned to a set piece.
    /// </summary>
    [TestCase(BaseGameTilesetProfiles.FutCity, "fcx01")]
    public void RoadLanes_EveryCarvedSegmentConnectsTwoRealAnchorsAndAvoidsStampedTiles(string profileKey, string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);
        var profile = OnboardedProfiles[profileKey];
        var seedsWithRoad = 0;

        for (var seed = 30100; seed < 30130; seed++)
        {
            var rng = new Random(seed);
            var parameters = FutCityParameters(profile);
            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);

            var roadEdges = AllRoadEdges(macro, parameters.Width, parameters.Height, "Routes").ToList();
            if (roadEdges.Count == 0) continue;
            seedsWithRoad++;

            // Build the cell-adjacency graph induced purely by Road edges.
            var roadCells = new HashSet<(int X, int Y)>();
            var adjacency = new Dictionary<(int X, int Y), List<(int X, int Y)>>();
            void Link((int X, int Y) a, (int X, int Y) b)
            {
                roadCells.Add(a);
                roadCells.Add(b);
                if (!adjacency.TryGetValue(a, out var la)) adjacency[a] = la = new List<(int X, int Y)>();
                la.Add(b);
            }

            foreach (var (cell, slot) in roadEdges)
            {
                var (dx, dy) = slot switch
                {
                    EdgeSlot.Top => (0, 1),
                    EdgeSlot.Right => (1, 0),
                    EdgeSlot.Bottom => (0, -1),
                    EdgeSlot.Left => (-1, 0),
                    _ => (0, 0)
                };
                var neighbor = (X: cell.X + dx, Y: cell.Y + dy);
                Link(cell, neighbor);
                Link(neighbor, cell);
            }

            // Every Road-crossed cell must stay fully open (Roads never repaint terrain) and must never
            // be a pinned (stamped set-piece) tile -- the "routes between buildings" property.
            foreach (var cell in roadCells)
            {
                macro.PinnedTiles.ContainsKey(cell).Should().BeFalse(
                    $"seed {seed}: Road cell {cell} must never overlap a LayoutGroupStamper-pinned tile");
                LayoutCornerUtilsIsFullyOpen(macro, cell, parameters.OpenTerrain).Should().BeTrue(
                    $"seed {seed}: Road cell {cell} must stay fully open terrain");
            }

            // Anchor set: every transition's interior tile plus every room's own CenterTile -- the same
            // anchor pool LayoutRoadCarver draws from.
            var anchors = macro.Transitions.Select(t => t.Tile)
                .Concat(macro.Rooms.Select(r => r.CenterTile))
                .ToHashSet();

            // Connected-component membership over the Road-edge graph.
            var componentOf = new Dictionary<(int X, int Y), int>();
            var nextComponent = 0;
            foreach (var start in roadCells)
            {
                if (componentOf.ContainsKey(start)) continue;
                var queue = new Queue<(int X, int Y)>();
                queue.Enqueue(start);
                componentOf[start] = nextComponent;
                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    if (!adjacency.TryGetValue(cur, out var neighbors)) continue;
                    foreach (var n in neighbors)
                    {
                        if (componentOf.ContainsKey(n)) continue;
                        componentOf[n] = nextComponent;
                        queue.Enqueue(n);
                    }
                }
                nextComponent++;
            }

            // Every component of the Road graph must contain at least TWO real anchors (endpoints of
            // whichever lane(s) built it) -- a lone stray Road cell touching no anchor at all would mean
            // a lane's own endpoint bookkeeping is wrong.
            var anchorsPerComponent = new Dictionary<int, int>();
            foreach (var cell in roadCells)
            {
                if (!anchors.Contains(cell)) continue;
                var c = componentOf[cell];
                anchorsPerComponent[c] = anchorsPerComponent.GetValueOrDefault(c) + 1;
            }

            foreach (var component in componentOf.Values.Distinct())
            {
                anchorsPerComponent.GetValueOrDefault(component).Should().BeGreaterOrEqualTo(2,
                    $"seed {seed}: Road component {component} must connect at least two real anchors");
            }
        }

        seedsWithRoad.Should().BeGreaterThan(0, "should exercise Road connectivity at least once across 30 seeds");
    }

    private static bool LayoutCornerUtilsIsFullyOpen(MacroLayout macro, (int X, int Y) cell, string open)
    {
        var c = macro.Corners;
        return string.Equals(c.Labels[cell.X, cell.Y], open, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(c.Labels[cell.X + 1, cell.Y], open, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(c.Labels[cell.X, cell.Y + 1], open, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(c.Labels[cell.X + 1, cell.Y + 1], open, StringComparison.OrdinalIgnoreCase);
    }

    [TestCase(BaseGameTilesetProfiles.FutCity, "fcx01")]
    public void RoadLanes_IsDeterministicPerSeed(string profileKey, string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);
        var profile = OnboardedProfiles[profileKey];

        MacroLayout Generate()
        {
            var rng = new Random(30200);
            return MacroLayoutGenerator.Generate(FutCityParameters(profile), rng, model);
        }

        var first = Generate();
        var second = Generate();

        for (var y = 0; y < 24; y++)
        for (var x = 0; x < 24; x++)
        {
            first.Corners.Labels[x, y].Should().Be(second.Corners.Labels[x, y], $"corner ({x},{y})");
            for (var slot = 0; slot < 4; slot++)
                first.Crossers.GetEdge(x, y, slot).Should().Be(second.Crossers.GetEdge(x, y, slot), $"cell ({x},{y}) slot {slot}");
        }
    }

    /// <summary>Back-compat: RoadLanes=0 must never emit a Road edge even on an otherwise-eligible
    /// composition.</summary>
    [Test]
    public void RoadLanes_Zero_ProducesNoRoadEdges()
    {
        var model = LoadTileset("fcx01");
        var profile = OnboardedProfiles[BaseGameTilesetProfiles.FutCity];

        for (var seed = 30300; seed < 30310; seed++)
        {
            var parameters = FutCityParameters(profile);
            parameters.RoadLanes = 0;
            var macro = MacroLayoutGenerator.Generate(parameters, new Random(seed), model);

            AllRoadEdges(macro, parameters.Width, parameters.Height, "Routes")
                .Should().BeEmpty($"seed {seed}: RoadLanes=0 must never place a Road edge");
        }
    }

    /// <summary>Back-compat: a composition whose tileset never declares RoadCrosser (every tileset
    /// except fcx01 today) must never emit a Road edge, regardless of MacroLayoutParameters.RoadLanes'
    /// own nonzero default -- DungeonComposition.BuildLayoutParameters zeroes RoadLanes alongside an
    /// empty RoadCrosser (mirroring AccentChannels/ChannelTerrain's gating), and LayoutRoadCarver itself
    /// also no-ops on an empty RoadCrosser as a second, independent guard.</summary>
    [Test]
    public void RoadLanes_TilesetWithoutRoadCrosser_ProducesNoRoadEdges()
    {
        var model = LoadTileset("tds01");

        var parameters = new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            MinRooms = 4,
            MaxRooms = 6,
            MinRoomCornerSize = 4,
            MaxRoomCornerSize = 7,
            LoopFactor = 0.3,
            Width = 24,
            Height = 24,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = model.FloorTerrain,
            RoadLanes = 6, // the default -- proves the tileset-side gate, not a test-supplied zero
            RoadCrosser = string.Empty,
        };

        var macro = MacroLayoutGenerator.Generate(parameters, new Random(30400), model);
        AllRoadEdges(macro, parameters.Width, parameters.Height, "Routes").Should().BeEmpty();
    }

    /// <summary>Back-compat with a null tileset: LayoutRoadCarver must no-op even when RoadLanes/
    /// RoadCrosser are configured, since it has no tileset to probe capability against -- the same
    /// guard LayoutFenceCarver/LayoutGroupStamper use.</summary>
    [Test]
    public void RoadLanes_NullTilesetSkipsCarvingEntirely()
    {
        var model = LoadTileset("fcx01");
        var profile = OnboardedProfiles[BaseGameTilesetProfiles.FutCity];
        var parameters = FutCityParameters(profile);

        var macro = MacroLayoutGenerator.Generate(parameters, new Random(30500));

        AllRoadEdges(macro, parameters.Width, parameters.Height, "Routes").Should().BeEmpty();
    }

    /// <summary>DungeonComposition.BuildLayoutParameters wiring: FutCity/FutCityPlaza declare
    /// RoadCrosser("Routes"), and the layout template's default RoadLanes survives the gate (RoadCrosser
    /// non-empty).</summary>
    [TestCase(BaseGameTilesetProfiles.FutCity)]
    [TestCase(BaseGameTilesetProfiles.FutCityPlaza)]
    public void BuildLayoutParameters_FutCityDeclaresRoadCrosser(string profileKey)
    {
        var tileset = OnboardedProfiles[profileKey];
        tileset.RoadCrosser.Should().Be("Routes");

        var composition = new DungeonComposition
        {
            Content = new DungeonDetail(),
            Tileset = tileset,
            Layout = new DungeonLayoutProfile { Template = new MacroLayoutParameters { RoadLanes = 6 } },
        };

        var parameters = composition.BuildLayoutParameters();
        parameters.RoadCrosser.Should().Be("Routes");
        parameters.RoadLanes.Should().Be(6);
    }

    /// <summary>The gating direction proven the other way: a layout template with RoadLanes disabled
    /// (0) stays 0 even when the tileset declares RoadCrosser.</summary>
    [Test]
    public void BuildLayoutParameters_RoadLanesZero_StaysZeroEvenWithRoadCrosserDeclared()
    {
        var tileset = OnboardedProfiles[BaseGameTilesetProfiles.FutCity];

        var composition = new DungeonComposition
        {
            Content = new DungeonDetail(),
            Tileset = tileset,
            Layout = new DungeonLayoutProfile { Template = new MacroLayoutParameters { RoadLanes = 0 } },
        };

        var parameters = composition.BuildLayoutParameters();
        parameters.RoadLanes.Should().Be(0);
    }
}
