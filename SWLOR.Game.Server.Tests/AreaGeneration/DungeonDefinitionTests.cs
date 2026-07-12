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

    [Test]
    public void MineCaveDungeonDefinition_RegistersUnderExpectedThemeKey()
    {
        var dungeons = BuildAllDungeons();

        dungeons.Should().ContainKey(MineCaveDungeonDefinition.ThemeKey);
        dungeons[MineCaveDungeonDefinition.ThemeKey].DisplayName.Should().NotBeNullOrWhiteSpace();
        dungeons[MineCaveDungeonDefinition.ThemeKey].TilesetResref.Should().Be("tdt01");
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
