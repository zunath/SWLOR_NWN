using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

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
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

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

    private static readonly DungeonLayoutStyle[] AllStyles =
    {
        DungeonLayoutStyle.RoomsAndCorridors,
        DungeonLayoutStyle.OrganicCave,
        DungeonLayoutStyle.Warren,
        DungeonLayoutStyle.PackedRooms,
        DungeonLayoutStyle.Labyrinth
    };

    /// <summary>
    /// Runs every registered tileset against every layout style, proving each tileset's tile inventory
    /// satisfies whatever corner combinations that style produces — not just the RoomsAndCorridors shape
    /// the other tests exercise. tdt01/OrganicCave and tds01/Warren additionally enable an accent terrain
    /// (Water, Pit respectively) verified to have full (open, accent) tile coverage in those tilesets.
    /// </summary>
    [TestCaseSource(nameof(StyleMatrixCases))]
    public void Tileset_StyleMatrix_ResolvesAcrossSeeds(string tilesetResref, DungeonLayoutStyle style)
    {
        var model = LoadTileset(tilesetResref);
        var failures = new List<string>();

        for (var seed = 1; seed <= 10; seed++)
        {
            var rng = new Random(seed);
            var parameters = new MacroLayoutParameters
            {
                Width = 16,
                Height = 16,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = model.FloorTerrain,
                Style = style
            };

            if (tilesetResref == "tdt01" && style == DungeonLayoutStyle.OrganicCave)
            {
                parameters.AccentTerrain = "Water";
                parameters.AccentDensity = 0.06;
            }
            else if (tilesetResref == "tds01" && style == DungeonLayoutStyle.Warren)
            {
                parameters.AccentTerrain = "Pit";
                parameters.AccentDensity = 0.05;
            }

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: macro layout generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: {reason}");
                continue;
            }

            resolved.Tiles.Should().HaveCount(16 * 16);
            resolved.Rooms.Should().NotBeEmpty();
        }

        failures.Should().BeEmpty($"every {style} macro layout must resolve against the real {tilesetResref} inventory");
    }

    // The four original generation tilesets (matches the former TilesetHakDirectories.Keys set).
    private static readonly string[] KnownTilesetResrefs = { "tdt01", "zsf01", "tds01", "vmr01" };

    private static IEnumerable<TestCaseData> StyleMatrixCases()
    {
        foreach (var tileset in KnownTilesetResrefs)
        {
            foreach (var style in AllStyles)
            {
                yield return new TestCaseData(tileset, style).SetName($"Tileset_StyleMatrix_ResolvesAcrossSeeds({tileset},{style})");
            }
        }
    }

}
