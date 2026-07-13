using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Feature tile sprinkling: rare decorative 1x1 "group" tiles (treasure mounds, pillars, hot
/// springs, ...) mixed into open room space at low density. Runs the full pipeline against every
/// generation tileset's real .set data, using the same (name -> weight) configuration
/// StandardTilesetProfiles declares, so this proves the curated lists actually resolve and place.
/// </summary>
public class FeatureTileTests
{
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

    private static Dictionary<string, DungeonTilesetProfile> LoadProfiles()
    {
        return new StandardTilesetProfiles().BuildTilesetProfiles();
    }

    private static MacroLayoutParameters FeatureParameters(
        TilesetModel model, string openTerrainOverride, Dictionary<string, int> featureTiles,
        double featureDensity, int size = 20)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            MinRooms = 6,
            MaxRooms = 9,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            LoopFactor = 0.3,
            Width = size,
            Height = size,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = string.IsNullOrEmpty(openTerrainOverride) ? model.FloorTerrain : openTerrainOverride,
            FeatureDensity = featureDensity,
            FeatureTiles = featureTiles,
        };
    }

    private static bool CellFullyOpen(MacroLayout macro, int x, int y)
    {
        var openTerrain = macro.OpenTerrain;
        return string.Equals(macro.Corners.Labels[x, y + 1], openTerrain, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(macro.Corners.Labels[x + 1, y + 1], openTerrain, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(macro.Corners.Labels[x + 1, y], openTerrain, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(macro.Corners.Labels[x, y], openTerrain, StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<TestCaseData> CuratedTilesetCases()
    {
        var profiles = LoadProfiles();
        yield return new TestCaseData("tdt01", "", profiles[StandardTilesetProfiles.Cavern].FeatureTiles)
            .SetName("FeatureSprinkling_{m}(cavern/tdt01)");
        yield return new TestCaseData("tds01", "", profiles[StandardTilesetProfiles.Sewers].FeatureTiles)
            .SetName("FeatureSprinkling_{m}(sewers/tds01)");
        yield return new TestCaseData("vmr01", "Plaza", profiles[StandardTilesetProfiles.AncientRuin].FeatureTiles)
            .SetName("FeatureSprinkling_{m}(ancientruin/vmr01)");
    }

    [TestCaseSource(nameof(CuratedTilesetCases))]
    public void FeatureSprinkling_PlacesValidSpacedFeaturesAcrossSeeds(
        string tilesetResref, string openTerrainOverride, Dictionary<string, int> featureTiles)
    {
        var model = LoadTileset(tilesetResref);
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);
        var totalFeatures = 0;

        for (var seed = 9000; seed < 9015; seed++)
        {
            var rng = new Random(seed);
            var parameters = FeatureParameters(model, openTerrainOverride, featureTiles, featureDensity: 0.08);

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            macro.Seed = seed;
            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);

            var transitionCells = new HashSet<(int X, int Y)>(resolved.Transitions.Select(t => t.Tile));

            // TileDoorPlanner (DoorTransitions defaults true and isn't disabled by this test) may
            // substitute a solid-side terminator tile at a transition's DoorCell -- since Fix A, a
            // terminator candidate may be a grouped tile (e.g. vmr01/tds01 "Door_Trans"), so a
            // GroupIndex != -1 tile can legitimately sit on solid ground there. Exclude those cells
            // from feature classification below; they are door substitutions, not sprinkled features.
            var doorSubstitutionCells = new HashSet<(int X, int Y)>(
                resolved.Transitions.Where(t => t.Style != TransitionStyle.Placeable).Select(t => t.DoorCell));
            var featureCells = new List<(int X, int Y)>();

            for (var y = 0; y < resolved.Height; y++)
            {
                for (var x = 0; x < resolved.Width; x++)
                {
                    var tile = resolved.GetTile(x, y);
                    var record = tilesById[tile.TileId];

                    // Only feature-configured groups ever register with the resolver, so any group
                    // tile (GroupIndex != -1) showing up in the resolved output is a placed feature --
                    // except a TileDoorPlanner terminator substitution, excluded above.
                    if (record.GroupIndex == -1) continue;
                    if (doorSubstitutionCells.Contains((x, y))) continue;

                    featureCells.Add((x, y));
                    CellFullyOpen(macro, x, y).Should().BeTrue(
                        $"seed {seed}: feature at ({x},{y}) must sit on a fully-open cell");
                    transitionCells.Should().NotContain((x, y),
                        $"seed {seed}: feature at ({x},{y}) must not land on a transition anchor");
                }
            }

            for (var i = 0; i < featureCells.Count; i++)
            for (var j = i + 1; j < featureCells.Count; j++)
            {
                var dx = Math.Abs(featureCells[i].X - featureCells[j].X);
                var dy = Math.Abs(featureCells[i].Y - featureCells[j].Y);
                Math.Max(dx, dy).Should().BeGreaterThan(2,
                    $"seed {seed}: features at {featureCells[i]} and {featureCells[j]} are too close together");
            }

            totalFeatures += featureCells.Count;
        }

        totalFeatures.Should().BeGreaterThan(0,
            "at least one feature tile should appear across 15 seeds at density 0.08");
    }

    [Test]
    public void FeatureSprinkling_InvalidGroupNameIsSilentlyIgnored()
    {
        var model = LoadTileset("tdt01");
        var featureTiles = new Dictionary<string, int> { ["NoSuchGroupWhatsoever"] = 5 };

        for (var seed = 9100; seed < 9105; seed++)
        {
            var rng = new Random(seed);
            // Density 1.0 so every eligible cell would roll a feature if the (invalid) name resolved.
            var parameters = FeatureParameters(model, "", featureTiles, featureDensity: 1.0);
            var macro = MacroLayoutGenerator.Generate(parameters, rng);
            macro.Seed = seed;

            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);

            for (var y = 0; y < resolved.Height; y++)
            for (var x = 0; x < resolved.Width; x++)
            {
                var record = model.Tiles[resolved.GetTile(x, y).TileId];
                record.GroupIndex.Should().Be(-1, $"seed {seed}: an unresolvable feature name must never place a group tile");
            }
        }
    }

    [Test]
    public void FeatureSprinkling_ZsfHasNoQualifyingFeaturesEvenWhenConfigured()
    {
        var model = LoadTileset("zsf01");
        var profiles = LoadProfiles();
        // zsf01 has no treasure/pillar/hot-spring groups at all; reusing the cavern's curated set
        // proves the structural/name check drops every one of them rather than trusting the profile.
        var featureTiles = profiles[StandardTilesetProfiles.Cavern].FeatureTiles;

        for (var seed = 9200; seed < 9205; seed++)
        {
            var rng = new Random(seed);
            var parameters = FeatureParameters(model, "floor", featureTiles, featureDensity: 1.0);
            var macro = MacroLayoutGenerator.Generate(parameters, rng);
            macro.Seed = seed;

            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);

            for (var y = 0; y < resolved.Height; y++)
            for (var x = 0; x < resolved.Width; x++)
            {
                var record = model.Tiles[resolved.GetTile(x, y).TileId];
                record.GroupIndex.Should().Be(-1, $"seed {seed}: zsf01 has no qualifying feature groups");
            }
        }
    }

    [Test]
    public void FeatureSprinkling_IsDeterministicPerSeed()
    {
        var model = LoadTileset("tdt01");
        var profiles = LoadProfiles();
        var featureTiles = profiles[StandardTilesetProfiles.Cavern].FeatureTiles;

        ResolvedLayout Resolve()
        {
            var rng = new Random(9300);
            var parameters = FeatureParameters(model, "", featureTiles, featureDensity: 0.08);
            var macro = MacroLayoutGenerator.Generate(parameters, rng);
            macro.Seed = 9300;
            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);
            return resolved;
        }

        var first = Resolve();
        var second = Resolve();

        first.Width.Should().Be(second.Width);
        first.Height.Should().Be(second.Height);
        for (var i = 0; i < first.Tiles.Length; i++)
        {
            first.Tiles[i].TileId.Should().Be(second.Tiles[i].TileId, $"cell index {i}");
            first.Tiles[i].Orientation.Should().Be(second.Tiles[i].Orientation, $"cell index {i}");
        }
    }

    [Test]
    public void FeatureSprinkling_EmptyConfigResolvesDeterministicallyWithNoFeatureTiles()
    {
        var model = LoadTileset("tdt01");
        var emptyFeatureTiles = new Dictionary<string, int>();

        ResolvedLayout Resolve()
        {
            var rng = new Random(9400);
            var parameters = FeatureParameters(model, "", emptyFeatureTiles, featureDensity: 0.08);
            var macro = MacroLayoutGenerator.Generate(parameters, rng);
            macro.Seed = 9400;
            TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason).Should().BeTrue(reason);
            return resolved;
        }

        var first = Resolve();
        var second = Resolve();

        for (var i = 0; i < first.Tiles.Length; i++)
        {
            first.Tiles[i].TileId.Should().Be(second.Tiles[i].TileId, $"cell index {i}");
            first.Tiles[i].Orientation.Should().Be(second.Tiles[i].Orientation, $"cell index {i}");
        }

        foreach (var tile in first.Tiles)
        {
            model.Tiles[tile.TileId].GroupIndex.Should().Be(-1, "empty FeatureTiles must never place a group tile");
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SWLOR.Game.Server.sln")))
                return current;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (SWLOR.Game.Server.sln).");
    }
}
