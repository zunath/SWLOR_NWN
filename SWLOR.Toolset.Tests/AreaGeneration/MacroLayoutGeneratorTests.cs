using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.AreaGeneration;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public class MacroLayoutGeneratorTests
{
    private static readonly DungeonLayoutStyle[] AllStyles =
    {
        DungeonLayoutStyle.RoomsAndCorridors,
        DungeonLayoutStyle.OrganicCave,
        DungeonLayoutStyle.Warren,
        DungeonLayoutStyle.PackedRooms,
        DungeonLayoutStyle.Labyrinth
    };

    private static MacroLayoutParameters DefaultParameters(
        DungeonLayoutStyle style = DungeonLayoutStyle.RoomsAndCorridors,
        int width = 20, int height = 20, int minRooms = 4, int maxRooms = 6)
    {
        return new MacroLayoutParameters
        {
            Width = width,
            Height = height,
            SolidTerrain = "Wall",
            OpenTerrain = "Floor",
            Style = style,
            MinRooms = minRooms,
            MaxRooms = maxRooms
        };
    }

    [Test]
    public void Generate_Transitions_HonorCountsAndPlacementInvariants()
    {
        foreach (var style in AllStyles)
        {
            for (var seed = 1; seed <= 15; seed++)
            {
                foreach (var (entrances, exits) in new[] { (1, 1), (2, 2), (3, 3), (1, 3) })
                {
                    var parameters = DefaultParameters(style);
                    parameters.EntranceCount = entrances;
                    parameters.ExitCount = exits;

                    var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));

                    var entrancePoints = layout.Transitions.Where(t => t.Kind == TransitionKind.Entrance).ToList();
                    var exitPoints = layout.Transitions.Where(t => t.Kind == TransitionKind.Exit).ToList();

                    entrancePoints.Should().HaveCount(entrances, $"{style} seed {seed}");
                    exitPoints.Should().HaveCount(exits, $"{style} seed {seed}");

                    // First entrance is the Entrance room's arrival anchor.
                    var entranceRoom = layout.Rooms.Single(r => r.Role == RoomRole.Entrance);
                    entrancePoints[0].Tile.Should().Be(entranceRoom.CenterTile);
                    entrancePoints[0].RoomId.Should().Be(entranceRoom.Id);

                    var roomsById = layout.Rooms.ToDictionary(r => r.Id);
                    foreach (var transition in layout.Transitions)
                    {
                        var room = roomsById[transition.RoomId];
                        room.Tiles.Should().Contain(transition.Tile,
                            $"{style} seed {seed}: transition tiles must be fully-open room tiles");

                        if (transition.Kind == TransitionKind.Entrance)
                            room.Role.Should().NotBe(RoomRole.Boss,
                                $"{style} seed {seed}: boss rooms never host entrances");
                    }

                    // Distinct tiles across all transitions.
                    layout.Transitions.Select(t => t.Tile).Should().OnlyHaveUniqueItems($"{style} seed {seed}");
                }
            }
        }
    }

    [TestCase(DungeonLayoutStyle.RoomsAndCorridors, 1)]
    [TestCase(DungeonLayoutStyle.RoomsAndCorridors, 2)]
    [TestCase(DungeonLayoutStyle.RoomsAndCorridors, 3)]
    [TestCase(DungeonLayoutStyle.RoomsAndCorridors, 4)]
    [TestCase(DungeonLayoutStyle.RoomsAndCorridors, 5)]
    [TestCase(DungeonLayoutStyle.OrganicCave, 1)]
    [TestCase(DungeonLayoutStyle.OrganicCave, 2)]
    [TestCase(DungeonLayoutStyle.OrganicCave, 3)]
    [TestCase(DungeonLayoutStyle.OrganicCave, 4)]
    [TestCase(DungeonLayoutStyle.OrganicCave, 5)]
    [TestCase(DungeonLayoutStyle.Warren, 1)]
    [TestCase(DungeonLayoutStyle.Warren, 2)]
    [TestCase(DungeonLayoutStyle.Warren, 3)]
    [TestCase(DungeonLayoutStyle.Warren, 4)]
    [TestCase(DungeonLayoutStyle.Warren, 5)]
    [TestCase(DungeonLayoutStyle.PackedRooms, 1)]
    [TestCase(DungeonLayoutStyle.PackedRooms, 2)]
    [TestCase(DungeonLayoutStyle.PackedRooms, 3)]
    [TestCase(DungeonLayoutStyle.PackedRooms, 4)]
    [TestCase(DungeonLayoutStyle.PackedRooms, 5)]
    [TestCase(DungeonLayoutStyle.Labyrinth, 1)]
    [TestCase(DungeonLayoutStyle.Labyrinth, 2)]
    [TestCase(DungeonLayoutStyle.Labyrinth, 3)]
    [TestCase(DungeonLayoutStyle.Labyrinth, 4)]
    [TestCase(DungeonLayoutStyle.Labyrinth, 5)]
    public void Generate_SameSeed_ProducesStructurallyIdenticalLayout(DungeonLayoutStyle style, int seed)
    {
        var parametersA = DefaultParameters(style);
        var parametersB = DefaultParameters(style);

        var layoutA = MacroLayoutGenerator.Generate(parametersA, new Random(seed));
        var layoutB = MacroLayoutGenerator.Generate(parametersB, new Random(seed));

        CornersShouldMatch(layoutA.Corners, layoutB.Corners);

        layoutA.Rooms.Should().HaveCount(layoutB.Rooms.Count);
        for (var i = 0; i < layoutA.Rooms.Count; i++)
        {
            var roomA = layoutA.Rooms[i];
            var roomB = layoutB.Rooms[i];

            roomA.Id.Should().Be(roomB.Id);
            roomA.Role.Should().Be(roomB.Role);
            roomA.CenterTile.Should().Be(roomB.CenterTile);
            roomA.Tiles.Should().BeEquivalentTo(roomB.Tiles, opts => opts.WithStrictOrdering());
        }
    }

    private static void CornersShouldMatch(CornerTerrainGrid a, CornerTerrainGrid b)
    {
        a.Width.Should().Be(b.Width);
        a.Height.Should().Be(b.Height);

        for (var x = 0; x <= a.Width; x++)
        {
            for (var y = 0; y <= a.Height; y++)
            {
                a.Labels[x, y].Should().Be(b.Labels[x, y], $"corner ({x},{y}) should match");
            }
        }
    }

    [TestCase(DungeonLayoutStyle.RoomsAndCorridors)]
    [TestCase(DungeonLayoutStyle.OrganicCave)]
    [TestCase(DungeonLayoutStyle.Warren)]
    [TestCase(DungeonLayoutStyle.PackedRooms)]
    [TestCase(DungeonLayoutStyle.Labyrinth)]
    public void Generate_BorderRing_AlwaysRemainsSolid(DungeonLayoutStyle style)
    {
        var parameters = DefaultParameters(style);

        for (var seed = 0; seed < 25; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
            var corners = layout.Corners;

            for (var x = 0; x <= corners.Width; x++)
            {
                corners.Labels[x, 0].Should().Be(parameters.SolidTerrain, $"seed {seed}, corner ({x},0)");
                corners.Labels[x, corners.Height].Should().Be(parameters.SolidTerrain, $"seed {seed}, corner ({x},{corners.Height})");
            }

            for (var y = 0; y <= corners.Height; y++)
            {
                corners.Labels[0, y].Should().Be(parameters.SolidTerrain, $"seed {seed}, corner (0,{y})");
                corners.Labels[corners.Width, y].Should().Be(parameters.SolidTerrain, $"seed {seed}, corner ({corners.Width},{y})");
            }
        }
    }

    [TestCase(DungeonLayoutStyle.RoomsAndCorridors)]
    [TestCase(DungeonLayoutStyle.OrganicCave)]
    [TestCase(DungeonLayoutStyle.Warren)]
    [TestCase(DungeonLayoutStyle.PackedRooms)]
    [TestCase(DungeonLayoutStyle.Labyrinth)]
    public void Generate_OpenCorners_AreFullyConnected(DungeonLayoutStyle style)
    {
        var parameters = DefaultParameters(style);

        for (var seed = 0; seed < 25; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
            var corners = layout.Corners;

            var openCells = new List<(int X, int Y)>();
            for (var x = 0; x <= corners.Width; x++)
            {
                for (var y = 0; y <= corners.Height; y++)
                {
                    if (corners.Labels[x, y] == parameters.OpenTerrain)
                        openCells.Add((x, y));
                }
            }

            openCells.Should().NotBeEmpty($"seed {seed} should have carved some open space");

            var reachable = FloodFill(corners, parameters.OpenTerrain, openCells[0]);

            reachable.Count.Should().Be(openCells.Count, $"seed {seed}: every open corner should be reachable from every other");
        }
    }

    private static HashSet<(int X, int Y)> FloodFill(CornerTerrainGrid corners, string openTerrain, (int X, int Y) start)
    {
        var visited = new HashSet<(int X, int Y)>();
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;
                if (corners.Labels[nx, ny] != openTerrain) continue;
                if (!visited.Add((nx, ny))) continue;

                queue.Enqueue((nx, ny));
            }
        }

        return visited;
    }

    [TestCase(DungeonLayoutStyle.RoomsAndCorridors)]
    [TestCase(DungeonLayoutStyle.OrganicCave)]
    [TestCase(DungeonLayoutStyle.Warren)]
    [TestCase(DungeonLayoutStyle.PackedRooms)]
    [TestCase(DungeonLayoutStyle.Labyrinth)]
    public void Generate_Rooms_CountAndRolesAreValid(DungeonLayoutStyle style)
    {
        var parameters = DefaultParameters(style);

        for (var seed = 0; seed < 25; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));

            layout.Rooms.Count.Should().BeInRange(2, parameters.MaxRooms, $"seed {seed}");

            layout.Rooms.Count(r => r.Role == RoomRole.Entrance).Should().Be(1, $"seed {seed}");
            layout.Rooms.Count(r => r.Role == RoomRole.Boss).Should().Be(1, $"seed {seed}");

            foreach (var room in layout.Rooms)
            {
                room.Tiles.Should().NotBeEmpty($"seed {seed}, room {room.Id}");
                room.Tiles.Should().Contain(room.CenterTile, $"seed {seed}, room {room.Id}");
            }
        }
    }

    [TestCase(DungeonLayoutStyle.RoomsAndCorridors)]
    [TestCase(DungeonLayoutStyle.OrganicCave)]
    [TestCase(DungeonLayoutStyle.Warren)]
    [TestCase(DungeonLayoutStyle.PackedRooms)]
    [TestCase(DungeonLayoutStyle.Labyrinth)]
    public void Generate_RoomCenterTile_IsAlwaysFullyOpen(DungeonLayoutStyle style)
    {
        var parameters = DefaultParameters(style);

        for (var seed = 0; seed < 25; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
            var corners = layout.Corners;

            foreach (var room in layout.Rooms)
            {
                var (cx, cy) = room.CenterTile;

                corners.Labels[cx, cy].Should().Be(parameters.OpenTerrain, $"seed {seed}, room {room.Id} center corner (TL)");
                corners.Labels[cx + 1, cy].Should().Be(parameters.OpenTerrain, $"seed {seed}, room {room.Id} center corner (TR)");
                corners.Labels[cx, cy + 1].Should().Be(parameters.OpenTerrain, $"seed {seed}, room {room.Id} center corner (BL)");
                corners.Labels[cx + 1, cy + 1].Should().Be(parameters.OpenTerrain, $"seed {seed}, room {room.Id} center corner (BR)");
            }
        }
    }

    [Test]
    public void Generate_TooSmallArea_ClampsToSizeFloorInsteadOfThrowing()
    {
        // Generate now normalizes every parameter (including Width/Height) through
        // LayoutParameterConstraints.ClampToValid before dispatching to a style, so a
        // caller-requested area below LayoutStyleSizeFloor no longer throws -- it silently
        // generates at the floor instead (see LayoutParameterConstraints).
        var parameters = DefaultParameters(width: 4, height: 4, minRooms: 4, maxRooms: 8);
        var floor = LayoutStyleSizeFloor.For(parameters.Style);

        var layout = MacroLayoutGenerator.Generate(parameters, new Random(1));

        layout.Rooms.Count.Should().BeGreaterOrEqualTo(2);
        layout.Corners.Width.Should().Be(floor);
        layout.Corners.Height.Should().Be(floor);
        // The caller's own object must never be mutated by Generate.
        parameters.Width.Should().Be(4);
        parameters.Height.Should().Be(4);
    }

    [Test]
    public void Generate_AllStyles_ProduceAtLeastTwoRoomsAcrossManySeeds()
    {
        foreach (var style in AllStyles)
        {
            var parameters = DefaultParameters(style);

            for (var seed = 0; seed < 25; seed++)
            {
                var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
                layout.Rooms.Count.Should().BeGreaterOrEqualTo(2, $"style {style}, seed {seed}");
            }
        }
    }

    [Test]
    public void Generate_OrganicCaveAccents_NeverTouchSolidAndPreserveConnectivityAndRoomCenters()
    {
        var parameters = DefaultParameters(DungeonLayoutStyle.OrganicCave);
        parameters.AccentTerrain = "Water";
        parameters.AccentDensity = 0.08;

        for (var seed = 0; seed < 25; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
            var corners = layout.Corners;

            var accentCount = 0;
            var openCount = 0;

            for (var x = 0; x <= corners.Width; x++)
            {
                for (var y = 0; y <= corners.Height; y++)
                {
                    var label = corners.Labels[x, y];

                    if (label == parameters.AccentTerrain)
                    {
                        accentCount++;

                        for (var dx = -1; dx <= 1; dx++)
                        {
                            for (var dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                var nx = x + dx;
                                var ny = y + dy;
                                if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;

                                corners.Labels[nx, ny].Should().NotBe(parameters.SolidTerrain,
                                    $"seed {seed}: accent corner ({x},{y}) has a solid 8-neighbor at ({nx},{ny})");
                            }
                        }
                    }
                    else if (label == parameters.OpenTerrain)
                    {
                        openCount++;
                    }
                }
            }

            // Open-corner connectivity must survive accent painting.
            var openCells = new List<(int X, int Y)>();
            for (var x = 0; x <= corners.Width; x++)
                for (var y = 0; y <= corners.Height; y++)
                    if (corners.Labels[x, y] == parameters.OpenTerrain)
                        openCells.Add((x, y));

            openCells.Should().NotBeEmpty($"seed {seed}");
            var reachable = FloodFill(corners, parameters.OpenTerrain, openCells[0]);
            reachable.Count.Should().Be(openCells.Count, $"seed {seed}: open corners must remain fully connected after accenting");

            // Room centers must never be painted over.
            foreach (var room in layout.Rooms)
            {
                var (cx, cy) = room.CenterTile;
                corners.Labels[cx, cy].Should().Be(parameters.OpenTerrain, $"seed {seed}, room {room.Id} center");
                corners.Labels[cx + 1, cy].Should().Be(parameters.OpenTerrain, $"seed {seed}, room {room.Id} center");
                corners.Labels[cx, cy + 1].Should().Be(parameters.OpenTerrain, $"seed {seed}, room {room.Id} center");
                corners.Labels[cx + 1, cy + 1].Should().Be(parameters.OpenTerrain, $"seed {seed}, room {room.Id} center");
            }

            var total = accentCount + openCount;
            if (total == 0) continue;

            var fraction = (double)accentCount / total;
            fraction.Should().BeInRange(0.01, 0.2, $"seed {seed}: accent fraction should be roughly the requested density");
        }
    }
}
