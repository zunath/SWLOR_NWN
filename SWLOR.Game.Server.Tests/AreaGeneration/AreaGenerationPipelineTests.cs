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
/// against every registered dungeon tileset. This is the test that proves each tileset's
/// tile inventory can satisfy every corner combination the macro generator produces —
/// a gap synthetic-fixture tests cannot cover.
/// </summary>
public class AreaGenerationPipelineTests
{
    // haks subdirectory each tileset's .set file lives under.
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

    [TestCase("tdt01")]
    [TestCase("zsf01")]
    [TestCase("tds01")]
    [TestCase("vmr01")]
    public void Tileset_HasSimpleTilesForEveryWallFloorCornerCombination(string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);

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
        var labels = new[] { model.DefaultTerrain.ToLowerInvariant(), model.FloorTerrain.ToLowerInvariant() };
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
            $"the macro generator can produce any Wall/Floor corner combination, so {tilesetResref} must offer a simple tile for each (TL|TR|BR|BL)");
    }

    [TestCase("tdt01", 100)]
    [TestCase("zsf01", 25)]
    [TestCase("tds01", 25)]
    [TestCase("vmr01", 25)]
    public void Tileset_FullPipelineSucceedsAcrossManySeeds(string tilesetResref, int seedCount)
    {
        var model = LoadTileset(tilesetResref);
        var failures = new List<string>();

        for (var seed = 1; seed <= seedCount; seed++)
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

        failures.Should().BeEmpty($"every macro layout must resolve against the real {tilesetResref} inventory");
    }

    [TestCase("tdt01")]
    [TestCase("zsf01")]
    [TestCase("tds01")]
    [TestCase("vmr01")]
    public void Tileset_PipelineIsDeterministicPerSeed(string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);

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
