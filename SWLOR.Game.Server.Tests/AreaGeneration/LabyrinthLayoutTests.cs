using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Labyrinth-specific coverage. Generic per-style invariants (connectivity, border solidity, room
/// count/role validity, room-center openness, same-seed determinism) already run for Labyrinth via
/// the shared style arrays in <see cref="MacroLayoutGeneratorTests"/>, <see cref="TileDoorPlannerTests"/>,
/// and <see cref="AreaGenerationPipelineTests"/>. This file focuses on what makes Labyrinth a distinct
/// style: a near-perfect maze (few loops, high corridor fraction) with a long, winding solution path
/// and only a handful of small chambers — measurably different from Warren on the same seeds.
/// </summary>
public class LabyrinthLayoutTests
{
    private static MacroLayoutParameters Parameters(DungeonLayoutStyle style, double loopFactor, int minRooms, int maxRooms, int width = 24, int height = 24, int corridorWidth = 1)
    {
        return new MacroLayoutParameters
        {
            Width = width,
            Height = height,
            SolidTerrain = "Wall",
            OpenTerrain = "Floor",
            Style = style,
            CorridorWidth = corridorWidth,
            LoopFactor = loopFactor,
            MinRooms = minRooms,
            MaxRooms = maxRooms
        };
    }

    /// <summary>Independent-cycle count of the open-corner graph: edges - vertices + 1 for a single
    /// connected component. Zero means a perfect tree (no loops); higher means more looping passages.</summary>
    private static int CountLoops(CornerTerrainGrid corners, string openTerrain)
    {
        var vertices = 0;
        var edges = 0;

        for (var x = 0; x <= corners.Width; x++)
        {
            for (var y = 0; y <= corners.Height; y++)
            {
                if (corners.Labels[x, y] != openTerrain) continue;
                vertices++;

                if (x + 1 <= corners.Width && corners.Labels[x + 1, y] == openTerrain) edges++;
                if (y + 1 <= corners.Height && corners.Labels[x, y + 1] == openTerrain) edges++;
            }
        }

        return edges - vertices + 1;
    }

    /// <summary>Fraction of open corners with exactly 2 open orthogonal neighbors (through-corridor
    /// cells, neither dead end nor junction). Mazes with few loops and long straight runs skew high;
    /// densely-looped/chambered networks skew lower (more junctions and open floor).</summary>
    private static double CorridorFraction(CornerTerrainGrid corners, string openTerrain)
    {
        var openCount = 0;
        var corridorCount = 0;

        for (var x = 0; x <= corners.Width; x++)
        {
            for (var y = 0; y <= corners.Height; y++)
            {
                if (corners.Labels[x, y] != openTerrain) continue;
                openCount++;

                var degree = 0;
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;
                    if (corners.Labels[nx, ny] == openTerrain) degree++;
                }

                if (degree == 2) corridorCount++;
            }
        }

        return openCount == 0 ? 0 : (double)corridorCount / openCount;
    }

    private static Dictionary<(int X, int Y), int> BfsDistances(CornerTerrainGrid corners, string openTerrain, (int X, int Y) start)
    {
        var dist = new Dictionary<(int X, int Y), int> { [start] = 0 };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var d = dist[current];

            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var nx = current.X + dx;
                var ny = current.Y + dy;
                if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;
                if (corners.Labels[nx, ny] != openTerrain) continue;

                var key = (nx, ny);
                if (dist.ContainsKey(key)) continue;

                dist[key] = d + 1;
                queue.Enqueue(key);
            }
        }

        return dist;
    }

    private static HashSet<(int X, int Y)> FloodFill(CornerTerrainGrid corners, string openTerrain, (int X, int Y) start)
    {
        var visited = new HashSet<(int X, int Y)> { start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || nx > corners.Width || ny < 0 || ny > corners.Height) continue;
                if (corners.Labels[nx, ny] != openTerrain) continue;
                if (!visited.Add((nx, ny))) continue;
                queue.Enqueue((nx, ny));
            }
        }

        return visited;
    }

    [Test]
    public void Labyrinth_HasFewerLoopsAndHigherCorridorFractionThanWarren()
    {
        // Same width/height/corridor width on both, but each style's own profile-representative
        // LoopFactor (Warren 0.3, Labyrinth 0.05 — see StandardLayoutProfiles), matching how the two
        // profiles are actually tuned in production.
        var labyrinthLoops = new List<int>();
        var warrenLoops = new List<int>();
        var labyrinthCorridorFraction = new List<double>();
        var warrenCorridorFraction = new List<double>();

        for (var seed = 0; seed < 20; seed++)
        {
            var labyrinth = MacroLayoutGenerator.Generate(
                Parameters(DungeonLayoutStyle.Labyrinth, loopFactor: 0.05, minRooms: 3, maxRooms: 4), new Random(seed));
            var warren = MacroLayoutGenerator.Generate(
                Parameters(DungeonLayoutStyle.Warren, loopFactor: 0.3, minRooms: 3, maxRooms: 5), new Random(seed));

            labyrinthLoops.Add(CountLoops(labyrinth.Corners, "Floor"));
            warrenLoops.Add(CountLoops(warren.Corners, "Floor"));
            labyrinthCorridorFraction.Add(CorridorFraction(labyrinth.Corners, "Floor"));
            warrenCorridorFraction.Add(CorridorFraction(warren.Corners, "Floor"));
        }

        labyrinthLoops.Average().Should().BeLessThan(warrenLoops.Average(),
            "Labyrinth's low default LoopFactor should leave a near-perfect maze with far fewer loops than Warren's");

        labyrinthCorridorFraction.Average().Should().BeGreaterThan(warrenCorridorFraction.Average(),
            "Labyrinth's directionally-biased carving should produce more long, un-branching corridor cells than Warren");
    }

    [Test]
    public void Labyrinth_SolutionPath_WindsSignificantlyBeyondStraightLineDistance()
    {
        var ratios = new List<double>();

        for (var seed = 0; seed < 20; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(
                Parameters(DungeonLayoutStyle.Labyrinth, loopFactor: 0.05, minRooms: 3, maxRooms: 4), new Random(seed));

            var entrance = layout.Rooms.Single(r => r.Role == RoomRole.Entrance);
            var boss = layout.Rooms.Single(r => r.Role == RoomRole.Boss);

            var manhattan = Math.Abs(entrance.CenterTile.X - boss.CenterTile.X) + Math.Abs(entrance.CenterTile.Y - boss.CenterTile.Y);
            if (manhattan == 0) continue;

            var distances = BfsDistances(layout.Corners, "Floor", entrance.CenterTile);
            var geodesic = distances[boss.CenterTile];

            ratios.Add((double)geodesic / manhattan);
        }

        ratios.Should().NotBeEmpty();
        // A perfectly straight corridor gives ratio ~1; a winding maze inflates this substantially.
        ratios.Average().Should().BeGreaterThan(1.5,
            "the entrance-to-boss path should wind through a large fraction of the labyrinth, not run near-straight");
    }

    [Test]
    public void Labyrinth_RoomCount_StaysSparse()
    {
        for (var seed = 0; seed < 25; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(
                Parameters(DungeonLayoutStyle.Labyrinth, loopFactor: 0.05, minRooms: 3, maxRooms: 4), new Random(seed));

            layout.Rooms.Count.Should().BeInRange(2, 4, $"seed {seed}: labyrinth chambers should stay sparse");
        }
    }

    [Test]
    public void Labyrinth_CorridorWidthTwo_StillHonorsWidthAndInvariants()
    {
        // Mirrors the zsf01/Facility composition, which raises CorridorWidth to its
        // MinimumOpeningWidth (2) regardless of the layout profile's own default. A width-1 maze can
        // never produce a fully-open tile outside a room (corridors are single lattice points wide,
        // even at junctions); a width-2 maze does, wherever a corridor runs — so finding one away
        // from every registered room is direct proof the parameter widened the passages.
        var foundWideCorridorSeeds = 0;

        for (var seed = 0; seed < 15; seed++)
        {
            var parameters = Parameters(DungeonLayoutStyle.Labyrinth, loopFactor: 0.05, minRooms: 3, maxRooms: 4, width: 28, height: 28, corridorWidth: 2);
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed));
            var corners = layout.Corners;

            // Border solid.
            for (var x = 0; x <= corners.Width; x++)
            {
                corners.Labels[x, 0].Should().Be("Wall", $"seed {seed}");
                corners.Labels[x, corners.Height].Should().Be("Wall", $"seed {seed}");
            }

            // Connectivity.
            var openCells = new List<(int X, int Y)>();
            for (var x = 0; x <= corners.Width; x++)
                for (var y = 0; y <= corners.Height; y++)
                    if (corners.Labels[x, y] == "Floor")
                        openCells.Add((x, y));

            openCells.Should().NotBeEmpty($"seed {seed}");
            FloodFill(corners, "Floor", openCells[0]).Count.Should().Be(openCells.Count, $"seed {seed}");

            var roomTiles = new HashSet<(int X, int Y)>(layout.Rooms.SelectMany(r => r.Tiles));

            for (var x = 0; x < corners.Width; x++)
            {
                for (var y = 0; y < corners.Height; y++)
                {
                    if (roomTiles.Contains((x, y))) continue;

                    if (corners.Labels[x, y] == "Floor" && corners.Labels[x + 1, y] == "Floor" &&
                        corners.Labels[x, y + 1] == "Floor" && corners.Labels[x + 1, y + 1] == "Floor")
                    {
                        foundWideCorridorSeeds++;
                        goto nextSeed;
                    }
                }
            }

            nextSeed: ;
        }

        foundWideCorridorSeeds.Should().BeGreaterThan(0,
            "at least some seeds should show a fully-open corridor tile outside any room, proving CorridorWidth=2 is honored");
    }

    [Test]
    public void Labyrinth_ProfileRegistration_ComposesLowLoopFactorAndSparseRooms()
    {
        var profiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        profiles.Should().ContainKey(StandardLayoutProfiles.Labyrinth);

        var profile = profiles[StandardLayoutProfiles.Labyrinth];
        profile.Template.Style.Should().Be(DungeonLayoutStyle.Labyrinth);
        profile.Template.LoopFactor.Should().BeLessThan(0.1, "the labyrinth profile should default to a near-perfect maze");
        profile.Template.MinRooms.Should().BeLessOrEqualTo(profile.Template.MaxRooms);
        profile.Template.MaxRooms.Should().BeLessOrEqualTo(4, "labyrinth chambers should default sparse");

        for (var seed = 0; seed < 10; seed++)
        {
            var parameters = profile.Template.Clone();
            parameters.Width = 24;
            parameters.Height = 24;
            parameters.SolidTerrain = "Wall";
            parameters.OpenTerrain = "Floor";

            var layoutA = MacroLayoutGenerator.Generate(parameters.Clone(), new Random(seed));
            var layoutB = MacroLayoutGenerator.Generate(parameters.Clone(), new Random(seed));

            layoutA.Rooms.Count.Should().Be(layoutB.Rooms.Count, $"seed {seed}: same seed should be deterministic");
            for (var i = 0; i < layoutA.Rooms.Count; i++)
            {
                layoutA.Rooms[i].CenterTile.Should().Be(layoutB.Rooms[i].CenterTile, $"seed {seed}, room {i}");
                layoutA.Rooms[i].Tiles.Should().BeEquivalentTo(layoutB.Rooms[i].Tiles, opts => opts.WithStrictOrdering(), $"seed {seed}, room {i}");
            }
        }
    }
}
