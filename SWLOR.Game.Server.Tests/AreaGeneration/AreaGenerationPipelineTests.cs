using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Runs the full offline pipeline (parse real .set -> macro layout -> tile resolution)
/// against the actual first-target tileset. This is the test that proves tdt01's tile
/// inventory can satisfy every corner combination the macro generator produces —
/// a gap synthetic-fixture tests cannot cover.
/// </summary>
public class AreaGenerationPipelineTests
{
    private static TilesetModel LoadTdt01()
    {
        var root = FindRepositoryRoot();
        var contents = File.ReadAllText(Path.Combine(root.FullName, "SWLOR_Haks", "sw_t_minecave", "tdt01.set"));
        return TilesetSetParser.Parse("tdt01", contents);
    }

    [Test]
    public void Tdt01_HasSimpleTilesForEveryWallFloorCornerCombination()
    {
        var model = LoadTdt01();

        // Simple = usable by the v1 resolver: ungrouped, crosser-free, doorless, flat.
        var simpleTiles = model.Tiles
            .Where(t => t.GroupIndex == -1 &&
                        !t.HasAnyCrosser &&
                        t.Doors.Count == 0 &&
                        t.CornerHeights.All(h => h == 0))
            .ToList();

        var covered = new HashSet<string>();
        foreach (var tile in simpleTiles)
        {
            for (var orientation = 0; orientation < 4; orientation++)
            {
                var key = string.Join("|",
                    tile.GetCornerAt(orientation, CornerSlot.TopLeft),
                    tile.GetCornerAt(orientation, CornerSlot.TopRight),
                    tile.GetCornerAt(orientation, CornerSlot.BottomRight),
                    tile.GetCornerAt(orientation, CornerSlot.BottomLeft)).ToLowerInvariant();
                covered.Add(key);
            }
        }

        var missing = new List<string>();
        var labels = new[] { "wall", "floor" };
        foreach (var tl in labels)
        foreach (var tr in labels)
        foreach (var br in labels)
        foreach (var bl in labels)
        {
            var key = $"{tl}|{tr}|{br}|{bl}";
            if (!covered.Contains(key))
                missing.Add(key);
        }

        missing.Should().BeEmpty(
            "the macro generator can produce any Wall/Floor corner combination, so tdt01 must offer a simple tile for each (TL|TR|BR|BL)");
    }

    [Test]
    public void Tdt01_FullPipelineSucceedsAcrossManySeeds()
    {
        var model = LoadTdt01();
        var failures = new List<string>();

        for (var seed = 1; seed <= 100; seed++)
        {
            var rng = new Random(seed);
            var macro = MacroLayoutGenerator.Generate(new MacroLayoutParameters
            {
                Width = 16,
                Height = 16,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = model.FloorTerrain,
                MinRooms = 4,
                MaxRooms = 8
            }, rng);
            macro.Seed = seed;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: {reason}");
                continue;
            }

            resolved.Tiles.Should().HaveCount(16 * 16);
            resolved.Rooms.Should().NotBeEmpty();
        }

        failures.Should().BeEmpty("every macro layout must resolve against the real tdt01 inventory");
    }

    [Test]
    public void Tdt01_PipelineIsDeterministicPerSeed()
    {
        var model = LoadTdt01();

        ResolvedLayout Run(int seed)
        {
            var rng = new Random(seed);
            // 12x12 with default room parameters cannot always fit MinRooms — 16x16 is the
            // practical minimum for the defaults (the facade's request default matches).
            var macro = MacroLayoutGenerator.Generate(new MacroLayoutParameters
            {
                Width = 16,
                Height = 16,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = model.FloorTerrain
            }, rng);
            macro.Seed = seed;

            TileResolver.TryResolve(model, macro, rng, out var resolved, out _).Should().BeTrue();
            return resolved;
        }

        var first = Run(1234);
        var second = Run(1234);

        for (var i = 0; i < first.Tiles.Length; i++)
        {
            second.Tiles[i].TileId.Should().Be(first.Tiles[i].TileId);
            second.Tiles[i].Orientation.Should().Be(first.Tiles[i].Orientation);
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
