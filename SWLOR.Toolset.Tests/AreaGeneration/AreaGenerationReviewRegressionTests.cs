using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
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
