using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

public class MacroLayoutGeneratorTests
{
    private static MacroLayoutParameters DefaultParameters(int width = 20, int height = 20, int minRooms = 4, int maxRooms = 6)
    {
        return new MacroLayoutParameters
        {
            Width = width,
            Height = height,
            SolidTerrain = "Wall",
            OpenTerrain = "Floor",
            MinRooms = minRooms,
            MaxRooms = maxRooms
        };
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Generate_SameSeed_ProducesStructurallyIdenticalLayout(int seed)
    {
        var parametersA = DefaultParameters();
        var parametersB = DefaultParameters();

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

    [Test]
    public void Generate_BorderRing_AlwaysRemainsSolid()
    {
        var parameters = DefaultParameters();

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

    [Test]
    public void Generate_OpenCorners_AreFullyConnected()
    {
        var parameters = DefaultParameters();

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

    [Test]
    public void Generate_Rooms_CountAndRolesAreValid()
    {
        var parameters = DefaultParameters();

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

    [Test]
    public void Generate_TooSmallArea_Throws()
    {
        var parameters = DefaultParameters(width: 4, height: 4, minRooms: 4, maxRooms: 8);

        Action act = () => MacroLayoutGenerator.Generate(parameters, new Random(1));

        act.Should().Throw<InvalidOperationException>();
    }
}
