using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Definitions;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public class DungeonDecorationPlannerReviewRegressionTests
{
    [Test]
    public void DoorTransition_FlanksRoomSideTileInsteadOfOutsideDoorwayCell()
    {
        var roomTiles = new HashSet<(int X, int Y)>
        {
            (0, 0), (1, 0), (2, 0),
            (0, 1), (1, 1), (2, 1),
            (0, 2), (1, 2), (2, 2)
        };
        var transition = new TransitionPoint
        {
            Kind = TransitionKind.Exit,
            Style = TransitionStyle.Door,
            RoomId = 1,
            Tile = (1, 0),
            DoorCell = (1, -1),
            DoorwayCell = (1, -1)
        };
        var excluded = new HashSet<(int X, int Y)>
        {
            transition.Tile,
            transition.DoorCell,
            transition.DoorwayCell
        };
        var tileToRoom = new Dictionary<(int X, int Y), int>();
        foreach (var tile in roomTiles)
            tileToRoom[tile] = 0;

        var pair = DungeonDecorationPlanner.FindDoorwayFlankPair(
            transition, excluded, tileToRoom, [roomTiles]);

        pair.Should().NotBeNull();
        new[] { pair!.Value.A, pair.Value.B }.Should().BeEquivalentTo(new[] { (0, 0), (2, 0) });
    }
}

public class TileResolverReviewRegressionTests
{
    [Test]
    public void ElevatedLayout_SprinklesFlatFeatureOnCompatibleLevelCellAtItsGridHeight()
    {
        var corners = new CornerTerrainGrid(3, 1, "Floor");
        for (var y = 0; y <= 1; y++)
        {
            corners.Heights[0, y] = 0;
            corners.Heights[1, y] = 0;
            corners.Heights[2, y] = 1;
            corners.Heights[3, y] = 1;
        }

        var layout = new MacroLayout(corners)
        {
            Seed = 23,
            DoorTransitions = false,
            FeatureDensity = 1,
            FeatureTiles = new Dictionary<string, int> { ["Feature"] = 1 },
            Transitions = [new TransitionPoint { Tile = (0, 0) }]
        };
        var tileset = new TilesetModel
        {
            Resref = "test",
            Tiles =
            [
                Tile(0, [0, 0, 0, 0]),
                Tile(1, [0, 0, 0, 0], groupIndex: 0),
                Tile(2, [0, 1, 1, 0])
            ],
            Groups =
            [
                new TileGroupRecord
                {
                    Name = "Feature",
                    Rows = 1,
                    Columns = 1,
                    TileIds = [1]
                }
            ]
        };

        TileResolver.TryResolve(tileset, layout, new Random(5), out var resolved, out var failure)
            .Should().BeTrue(failure);

        resolved.FeatureTileCells.Should().ContainKey((2, 0)).WhoseValue.Should().Be("Feature");
        resolved.Tiles[2].TileId.Should().Be(1);
        resolved.Tiles[2].Height.Should().Be(1);
    }

    private static TileRecord Tile(int id, int[] heights, int groupIndex = -1) => new()
    {
        TileId = id,
        GroupIndex = groupIndex,
        PathNode = "A",
        Corners = ["Floor", "Floor", "Floor", "Floor"],
        CornerHeights = heights
    };
}

public class GeneratedTreasureReviewRegressionTests
{
    [Test]
    public void Anchor_SkipsFeatureRoadAndStampedStructureTilesBeforeTestingClearance()
    {
        const string road = "Road";
        var crossers = new EdgeCrosserGrid(4, 1);
        crossers.SetEdge(1, 0, EdgeSlot.Top, road);
        var bossRoom = new LayoutRoom
        {
            Id = 1,
            Role = RoomRole.Boss,
            CenterTile = (0, 0),
            Tiles = [(0, 0), (1, 0), (2, 0), (3, 0)]
        };
        var resolved = new ResolvedLayout
        {
            Width = 4,
            Height = 1,
            Tiles = [new ResolvedTile(), new ResolvedTile(), new ResolvedTile(), new ResolvedTile()],
            Rooms = [bossRoom],
            Crossers = crossers,
            StampedStructureTiles = [(2, 0)],
            FeatureTileCells = new Dictionary<(int X, int Y), string>
            {
                [(0, 0)] = "Pillars"
            }
        };
        var draft = new AreaGenerationDraft(
            new AreaGenerationSettings { ThemeKey = "test" },
            new DungeonComposition
            {
                Content = new DungeonDetail(),
                Tileset = new DungeonTilesetProfile { RoadCrosser = road },
                Layout = new DungeonLayoutProfile()
            },
            new TilesetModel(),
            new GenerationResult { Success = true, Resolved = resolved });

        GeneratedAreaDocumentPopulator.FindTreasureAnchor(draft, bossRoom)
            .Should().Be((35f, 5f));
    }

    [Test]
    public void CreatureAnchors_SkipStructuresAndRespectLargeCollisionRadii()
    {
        var room = new LayoutRoom
        {
            Id = 7,
            Role = RoomRole.Standard,
            CenterTile = (2, 0),
            Tiles =
            [
                (0, 0), (1, 0), (2, 0), (3, 0),
                (0, 1), (1, 1), (2, 1), (3, 1)
            ]
        };
        var resolved = new ResolvedLayout
        {
            Width = 4,
            Height = 2,
            Rooms = [room],
            FeatureTileCells = new Dictionary<(int X, int Y), string> { [(0, 0)] = "Pillars" },
            StampedStructureTiles = [(1, 0)]
        };
        var occupied = new List<(float X, float Y, float Radius)>();

        var anchors = GeneratedAreaDocumentPopulator.SelectCreatureAnchors(
            resolved,
            room,
            [5.5f, 5.5f],
            occupied,
            new Random(91));

        anchors.Should().HaveCount(2);
        anchors.Select(anchor => ((int)(anchor.X / 10f), (int)(anchor.Y / 10f)))
            .Should().NotContain((0, 0)).And.NotContain((1, 0));
        var dx = anchors[0].X - anchors[1].X;
        var dy = anchors[0].Y - anchors[1].Y;
        MathF.Sqrt(dx * dx + dy * dy).Should().BeGreaterThanOrEqualTo(11f);
    }

    [Test]
    public async Task CreatureAnchors_FailPromptlyWhenLargeCreaturesCannotAllFit()
    {
        var room = new LayoutRoom
        {
            Id = 8,
            Role = RoomRole.Standard,
            CenterTile = (1, 0),
            Tiles =
            [
                (0, 0), (1, 0), (2, 0), (3, 0),
                (0, 1), (1, 1), (2, 1), (3, 1)
            ]
        };
        var resolved = new ResolvedLayout
        {
            Width = 4,
            Height = 2,
            Rooms = [room]
        };
        var occupied = new List<(float X, float Y, float Radius)>();

        var action = () => Task.Run(() =>
                GeneratedAreaDocumentPopulator.SelectCreatureAnchors(
                    resolved,
                    room,
                    [5.5f, 5.5f, 5.5f, 5.5f],
                    occupied,
                    new Random(91)))
            .WaitAsync(TimeSpan.FromSeconds(3));

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot fit 4 creatures*");
        occupied.Should().BeEmpty("failed placement must not reserve partial anchors");
    }

    [Test]
    public void SciFiBaseTierTwo_UsesOnlyMortalAmbientBlueprints()
    {
        var tier = new SciFiBaseDungeonDefinition()
            .BuildDungeons()[SciFiBaseDungeonDefinition.ThemeKey]
            .Tiers[2];

        tier.Creatures.Select(creature => creature.Resref)
            .Should().NotContain("republictrooper")
            .And.Contain("vrepnpctroop1");
    }
}
