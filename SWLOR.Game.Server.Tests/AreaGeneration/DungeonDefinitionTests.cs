using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

public class DungeonDefinitionTests
{
    // Hard-coded in DungeonContentPlacer; verified here so a resref rename/typo fails loudly
    // instead of silently spawning nothing at runtime.
    private const string TreasurePlaceableResref = "_mdrn_pl_crate01";
    private const string ExitPlaceableResref = "building_exit";

    // Fixed theme-key -> tileset mapping (see design/ProceduralAreaGeneration.md). Verified here so
    // a resref rename/typo, or a theme silently failing to register, fails loudly instead of
    // producing a subtly broken generated area at runtime.
    private static readonly (string ThemeKey, string TilesetResref)[] ExpectedThemeTilesets =
    {
        (MineCaveDungeonDefinition.ThemeKey, "tdt01"),
        (SciFiBaseDungeonDefinition.ThemeKey, "zsf01"),
        (SewerDungeonDefinition.ThemeKey, "tds01"),
        (AlienRuinDungeonDefinition.ThemeKey, "vmr01"),
    };

    [Test]
    public void AllDungeonDefinitions_RegisterUnderExpectedThemeKeysAndTilesets()
    {
        var dungeons = BuildAllDungeons();

        foreach (var (themeKey, tilesetResref) in ExpectedThemeTilesets)
        {
            dungeons.Should().ContainKey(themeKey);
            dungeons[themeKey].DisplayName.Should().NotBeNullOrWhiteSpace();
            dungeons[themeKey].TilesetResref.Should().Be(tilesetResref);
        }
    }

    [Test]
    public void AllDungeonDefinitions_TiersAreContiguousStartingAtOne()
    {
        var failures = new List<string>();

        foreach (var (themeKey, detail) in BuildAllDungeons())
        {
            var tiers = detail.Tiers.Keys.OrderBy(k => k).ToList();

            if (tiers.Count == 0)
            {
                failures.Add($"{themeKey}: has no tiers defined.");
                continue;
            }

            for (var i = 0; i < tiers.Count; i++)
            {
                var expected = i + 1;
                if (tiers[i] != expected)
                {
                    failures.Add($"{themeKey}: tiers must be contiguous starting at 1, found {string.Join(",", tiers)}.");
                    break;
                }
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void AllDungeonDefinitions_CreatureAndBossResrefsHaveModuleBlueprints()
    {
        var root = FindRepositoryRoot();
        var creatureResrefs = ReadModuleTemplateResrefs(root, "utc", "utc.json");
        var failures = new List<string>();

        foreach (var (themeKey, detail) in BuildAllDungeons())
        {
            foreach (var (tierNumber, tier) in detail.Tiers)
            {
                foreach (var creature in tier.Creatures)
                {
                    if (!creatureResrefs.Contains(creature.Resref))
                        failures.Add($"{themeKey} tier {tierNumber}: ambient creature '{creature.Resref}' has no Module/utc blueprint.");
                }

                if (string.IsNullOrWhiteSpace(tier.BossResref))
                {
                    failures.Add($"{themeKey} tier {tierNumber}: no boss resref configured.");
                }
                else if (!creatureResrefs.Contains(tier.BossResref))
                {
                    failures.Add($"{themeKey} tier {tierNumber}: boss '{tier.BossResref}' has no Module/utc blueprint.");
                }
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void AllDungeonDefinitions_TreasureLootTablesAreRegisteredAndNonEmpty()
    {
        var lootTables = BuildAllLootTables();
        var failures = new List<string>();

        foreach (var (themeKey, detail) in BuildAllDungeons())
        {
            foreach (var (tierNumber, tier) in detail.Tiers)
            {
                if (string.IsNullOrWhiteSpace(tier.TreasureLootTableId))
                {
                    failures.Add($"{themeKey} tier {tierNumber}: no treasure loot table configured.");
                    continue;
                }

                if (!lootTables.TryGetValue(tier.TreasureLootTableId, out var table))
                {
                    failures.Add($"{themeKey} tier {tierNumber}: treasure loot table '{tier.TreasureLootTableId}' is not registered by any ILootTableDefinition.");
                    continue;
                }

                if (table.Count == 0)
                    failures.Add($"{themeKey} tier {tierNumber}: treasure loot table '{tier.TreasureLootTableId}' has no items.");

                if (tier.TreasureItemCount < 1)
                    failures.Add($"{themeKey} tier {tierNumber}: treasure item count must be at least 1.");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void TreasureAndExitPlaceableResrefs_HaveModuleBlueprints()
    {
        var root = FindRepositoryRoot();
        var placeableResrefs = ReadModuleTemplateResrefs(root, "utp", "utp.json");

        placeableResrefs.Should().Contain(TreasurePlaceableResref);
        placeableResrefs.Should().Contain(ExitPlaceableResref);
    }

    [Test]
    public void AllDungeonDefinitions_PlaceholdersExistAndMatchTheirTileset()
    {
        var root = FindRepositoryRoot();
        var moduleAreaResrefs = ReadModuleAreaListResrefs(root);
        var failures = new List<string>();

        foreach (var (themeKey, detail) in BuildAllDungeons())
        {
            if (string.IsNullOrWhiteSpace(detail.PlaceholderResref))
            {
                failures.Add($"{themeKey}: no placeholder resref configured.");
                continue;
            }

            var arePath = Path.Combine(root.FullName, "Module", "are", $"{detail.PlaceholderResref}.are.json");
            if (!File.Exists(arePath))
            {
                failures.Add($"{themeKey}: placeholder '{detail.PlaceholderResref}' has no Module/are/{detail.PlaceholderResref}.are.json.");
                continue;
            }

            if (!moduleAreaResrefs.Contains(detail.PlaceholderResref))
            {
                failures.Add($"{themeKey}: placeholder '{detail.PlaceholderResref}' is not listed in Module/ifo/module.ifo.json Mod_Area_list.");
            }

            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(arePath));
            if (!document.RootElement.TryGetProperty("Tileset", out var tileset) ||
                !tileset.TryGetProperty("value", out var tilesetValue) ||
                !string.Equals(tilesetValue.GetString(), detail.TilesetResref, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{themeKey}: placeholder '{detail.PlaceholderResref}' area Tileset does not match theme TilesetResref '{detail.TilesetResref}'.");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static Dictionary<string, DungeonDetail> BuildAllDungeons()
    {
        var dungeons = new Dictionary<string, DungeonDetail>();

        foreach (var type in GetTypesImplementing<IDungeonListDefinition>())
        {
            var definition = (IDungeonListDefinition)Activator.CreateInstance(type)!;
            foreach (var (key, detail) in definition.BuildDungeons())
                dungeons[key] = detail;
        }

        return dungeons;
    }

    private static Dictionary<string, SWLOR.Game.Server.Service.LootService.LootTable> BuildAllLootTables()
    {
        var tables = new Dictionary<string, SWLOR.Game.Server.Service.LootService.LootTable>();

        foreach (var type in GetTypesImplementing<SWLOR.Game.Server.Service.LootService.ILootTableDefinition>())
        {
            var definition = (SWLOR.Game.Server.Service.LootService.ILootTableDefinition)Activator.CreateInstance(type)!;
            foreach (var (key, table) in definition.BuildLootTables())
                tables[key] = table;
        }

        return tables;
    }

    private static IEnumerable<Type> GetTypesImplementing<TInterface>()
    {
        return typeof(TInterface)
            .Assembly
            .GetTypes()
            .Where(type =>
                typeof(TInterface).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                !type.IsInterface)
            .OrderBy(type => type.Name);
    }

    private static HashSet<string> ReadModuleTemplateResrefs(DirectoryInfo root, string folder, string extension)
    {
        var resrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", folder), $"*.{extension}"))
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
            if (document.RootElement.TryGetProperty("TemplateResRef", out var templateResRef) &&
                templateResRef.TryGetProperty("value", out var value))
            {
                var resref = value.GetString();
                if (!string.IsNullOrWhiteSpace(resref))
                    resrefs.Add(resref);
            }
        }

        return resrefs;
    }

    private static HashSet<string> ReadModuleAreaListResrefs(DirectoryInfo root)
    {
        var resrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ifoPath = Path.Combine(root.FullName, "Module", "ifo", "module.ifo.json");
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ifoPath));

        if (document.RootElement.TryGetProperty("Mod_Area_list", out var areaList) &&
            areaList.TryGetProperty("value", out var areaListValue))
        {
            foreach (var area in areaListValue.EnumerateArray())
            {
                if (area.TryGetProperty("Area_Name", out var areaName) &&
                    areaName.TryGetProperty("value", out var value))
                {
                    var resref = value.GetString();
                    if (!string.IsNullOrWhiteSpace(resref))
                        resrefs.Add(resref);
                }
            }
        }

        return resrefs;
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
