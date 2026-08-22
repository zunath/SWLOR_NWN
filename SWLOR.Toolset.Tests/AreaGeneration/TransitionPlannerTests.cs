using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public sealed class TransitionPlannerTests
{
    [Test]
    public void TileDoorPlanner_DoesNotMoveTheArrivalAnchorOntoASlopedRoomTile()
    {
        var tileset = CreateDoorTileset();
        var layout = CreateLayout(3, 3, (0, 0), (1, 0), "Wall");
        layout.Corners.Labels[0, 0] = "Floor";
        layout.Corners.Labels[1, 0] = "Floor";
        layout.Corners.Labels[0, 1] = "Floor";
        layout.Corners.Labels[1, 1] = "Floor";
        layout.Corners.Heights[0, 0] = 1;
        var tiles = ResolvedTiles(9);

        TileDoorGeometry.IsFlatCell(layout.Corners, 0, 0).Should().BeFalse();
        TileDoorGeometry.IsFlatCell(layout.Corners, 1, 0).Should().BeTrue();
        TileDoorGeometry.IsFlatCell(layout.Corners, 2, 0).Should().BeTrue();

        TileDoorPlanner.ApplyDoorTransitions(tileset, layout, tiles, 3, 3);

        layout.Transitions.Single().Style.Should().Be(TransitionStyle.Placeable);
    }

    [Test]
    public void GroupExitPlanner_DoesNotMoveTheArrivalAnchorOntoASlopedRoomTile()
    {
        var tileset = CreateGroupExitTileset();
        var layout = CreateLayout(3, 3, (0, 0), (1, 0));
        layout.ExitGroups.Add("House 1");
        layout.Corners.Heights[0, 0] = 1;
        var tiles = ResolvedTiles(9);

        TileDoorGeometry.IsFlatCell(layout.Corners, 0, 0).Should().BeFalse();
        TileDoorGeometry.IsFlatCell(layout.Corners, 1, 0).Should().BeTrue();

        GroupExitPlanner.ApplyGroupExits(tileset, layout, tiles, 3, 3);

        layout.Transitions.Single().Style.Should().Be(TransitionStyle.Placeable);
    }

    [Test]
    public void GroupExitPlanner_OrientsTheDoorSlotTowardTheRoomAnchor()
    {
        var tileset = CreateGroupExitTileset();
        var layout = CreateLayout(3, 3, (1, 1), (2, 1));
        layout.ExitGroups.Add("House 1");
        var tiles = ResolvedTiles(9);

        GroupExitPlanner.ApplyGroupExits(tileset, layout, tiles, 3, 3);

        var transition = layout.Transitions.Single();
        transition.Style.Should().Be(TransitionStyle.GroupExit);
        transition.DoorCell.Should().Be((2, 1));
        tiles[5].Orientation.Should().Be(3);
        transition.DoorX.Should().Be(20f);
        transition.DoorY.Should().Be(15f);
    }

    [Test]
    public void GroupExitPlanner_DoesNotOverwriteAResolvedFeatureGroup()
    {
        var tileset = CreateGroupExitTileset();
        var layout = CreateLayout(3, 3, (1, 1), (2, 1));
        layout.ExitGroups.Add("House 1");
        var tiles = ResolvedTiles(9);
        tiles[5] = new ResolvedTile { TileId = 2 };

        GroupExitPlanner.ApplyGroupExits(tileset, layout, tiles, 3, 3);

        tiles[5].TileId.Should().Be(2);
        var transition = layout.Transitions.Single();
        transition.Style.Should().Be(TransitionStyle.GroupExit);
        transition.DoorCell.Should().NotBe((2, 1));
    }

    private static MacroLayout CreateLayout(
        int width,
        int height,
        (int X, int Y) roomTile,
        (int X, int Y) transitionTile,
        string fillTerrain = "Floor")
    {
        var layout = new MacroLayout(new CornerTerrainGrid(width, height, fillTerrain));
        layout.Rooms.Add(new LayoutRoom
        {
            Id = 1,
            Tiles = [roomTile],
            CenterTile = roomTile,
            OpenTerrain = "Floor"
        });
        layout.Transitions.Add(new TransitionPoint
        {
            Kind = TransitionKind.Exit,
            RoomId = 1,
            Tile = transitionTile
        });
        return layout;
    }

    private static TilesetModel CreateDoorTileset()
    {
        return new TilesetModel
        {
            DefaultTerrain = "Wall",
            FloorTerrain = "Floor",
            Tiles =
            [
                Tile(0, ["Floor", "Floor", "Floor", "Floor"]),
                Tile(
                    1,
                    ["Floor", "Wall", "Wall", "Floor"],
                    ["", "Doorway", "", ""],
                    new TileDoorRecord { X = 5f }),
                Tile(
                    2,
                    ["Wall", "Wall", "Wall", "Wall"],
                    ["", "", "", "Doorway"])
            ]
        };
    }

    private static TilesetModel CreateGroupExitTileset()
    {
        return new TilesetModel
        {
            DefaultTerrain = "Wall",
            FloorTerrain = "Floor",
            Tiles =
            [
                Tile(0, ["Floor", "Floor", "Floor", "Floor"]),
                Tile(
                    1,
                    ["Floor", "Floor", "Floor", "Floor"],
                    door: new TileDoorRecord { Y = -5f },
                    groupIndex: 0),
                Tile(2, ["Floor", "Floor", "Floor", "Floor"], groupIndex: 1)
            ],
            Groups =
            [
                new TileGroupRecord { Name = "House 1", Rows = 1, Columns = 1, TileIds = [1] },
                new TileGroupRecord { Name = "Feature", Rows = 1, Columns = 1, TileIds = [2] }
            ]
        };
    }

    private static TileRecord Tile(
        int id,
        string[] corners,
        string[]? edges = null,
        TileDoorRecord? door = null,
        int groupIndex = -1)
    {
        return new TileRecord
        {
            TileId = id,
            Corners = corners,
            Edges = edges ?? ["", "", "", ""],
            GroupIndex = groupIndex,
            Doors = door == null ? [] : [door]
        };
    }

    private static ResolvedTile[] ResolvedTiles(int count) =>
        Enumerable.Range(0, count).Select(_ => new ResolvedTile { TileId = 0 }).ToArray();
}
