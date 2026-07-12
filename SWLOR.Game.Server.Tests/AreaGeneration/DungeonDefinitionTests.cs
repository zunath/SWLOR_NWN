using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

public class DungeonDefinitionTests
{

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
        var tilesetProfiles = BuildAllTilesetProfiles();
        var layoutProfiles = BuildAllLayoutProfiles();

        foreach (var (themeKey, tilesetResref) in ExpectedThemeTilesets)
        {
            dungeons.Should().ContainKey(themeKey);
            var detail = dungeons[themeKey];
            detail.DisplayName.Should().NotBeNullOrWhiteSpace();

            tilesetProfiles.Should().ContainKey(detail.TilesetProfileKey,
                $"theme '{themeKey}' must reference a registered tileset profile");
            layoutProfiles.Should().ContainKey(detail.LayoutProfileKey,
                $"theme '{themeKey}' must reference a registered layout profile");

            tilesetProfiles[detail.TilesetProfileKey].TilesetResref.Should().Be(tilesetResref);
        }
    }

    [Test]
    public void AllLayoutProfiles_LeaveAccentTerrainToTilesetProfiles()
    {
        foreach (var (key, profile) in BuildAllLayoutProfiles())
        {
            profile.Template.AccentTerrain.Should().BeEmpty(
                $"layout profile '{key}' must stay tileset-independent — the accent terrain name comes from the tileset profile at composition time");
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
    public void AllDungeonDefinitions_TreasureAndExitPlaceablesHaveModuleBlueprints()
    {
        var root = FindRepositoryRoot();
        var placeableResrefs = ReadModuleTemplateResrefs(root, "utp", "utp.json");
        var failures = new List<string>();

        foreach (var (themeKey, detail) in BuildAllDungeons())
        {
            if (!placeableResrefs.Contains(detail.TreasurePlaceableResref))
                failures.Add($"{themeKey}: treasure placeable '{detail.TreasurePlaceableResref}' has no utp blueprint.");
            if (!placeableResrefs.Contains(detail.ExitPlaceableResref))
                failures.Add($"{themeKey}: exit placeable '{detail.ExitPlaceableResref}' has no utp blueprint.");
            if (string.IsNullOrWhiteSpace(detail.ExitDisplayName))
                failures.Add($"{themeKey}: exit display name is empty.");
            if (string.IsNullOrWhiteSpace(detail.TreasureDisplayName))
                failures.Add($"{themeKey}: treasure display name is empty.");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    // These blueprints looked plausible but render invisibly or with the wrong model —
    // discovered live. Guard against regressions to any of them.
    private static readonly string[] KnownBadPlaceableResrefs =
    {
        "building_exit",     // door model meant to sit flush against a wall; floats mid-room
        "zep_chest_dag",     // appearance row 4245 is blank in placeables.2da (invisible)
        "_mdrn_pl_crate01",  // HasInventory=0; item creation silently fails
        "_mdrn_placedoorb",  // "Concealed Tunnel" = invisible object appearance
        "_mdrn_placedoora",  // "Concealed Entrance" = invisible barrier appearance
        "zep_doorway_d001",  // "Portal. Blue" appearance row is a carpet
        "zep_doorway_d003",  // "Portal. Pyramid" appearance row is skeleton bones
    };

    [Test]
    public void AllDungeonDefinitions_AvoidKnownBadPlaceables()
    {
        foreach (var (themeKey, detail) in BuildAllDungeons())
        {
            KnownBadPlaceableResrefs.Should().NotContain(detail.TreasurePlaceableResref,
                $"{themeKey}'s treasure placeable renders incorrectly");
            KnownBadPlaceableResrefs.Should().NotContain(detail.ExitPlaceableResref,
                $"{themeKey}'s exit placeable renders incorrectly");
        }
    }

    [Test]
    public void AllTilesetProfiles_PlaceholdersExistAndMatchTheirTileset()
    {
        var root = FindRepositoryRoot();
        var moduleAreaResrefs = ReadModuleAreaListResrefs(root);
        var failures = new List<string>();

        foreach (var (key, profile) in BuildAllTilesetProfiles())
        {
            if (string.IsNullOrWhiteSpace(profile.PlaceholderResref))
            {
                failures.Add($"{key}: no placeholder resref configured.");
                continue;
            }

            var arePath = Path.Combine(root.FullName, "Module", "are", $"{profile.PlaceholderResref}.are.json");
            if (!File.Exists(arePath))
            {
                failures.Add($"{key}: placeholder '{profile.PlaceholderResref}' has no Module/are/{profile.PlaceholderResref}.are.json.");
                continue;
            }

            if (!moduleAreaResrefs.Contains(profile.PlaceholderResref))
            {
                failures.Add($"{key}: placeholder '{profile.PlaceholderResref}' is not listed in Module/ifo/module.ifo.json Mod_Area_list.");
            }

            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(arePath));
            if (!document.RootElement.TryGetProperty("Tileset", out var tileset) ||
                !tileset.TryGetProperty("value", out var tilesetValue) ||
                !string.Equals(tilesetValue.GetString(), profile.TilesetResref, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{key}: placeholder '{profile.PlaceholderResref}' area Tileset does not match profile TilesetResref '{profile.TilesetResref}'.");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Validates every theme's exit/treasure placeable renders and behaves at the DATA level:
    /// the blueprint must be useable, non-static, point at a placeables.2da row whose ModelName
    /// is not blank (blank rows render invisible — this happened with zep_chest_dag), and
    /// treasure containers must have an inventory (or item creation silently fails).
    /// This is the automated stand-in for eyeballing placeables in the client.
    /// </summary>
    [Test]
    public void AllDungeonDefinitions_PlaceablesAreVisibleUseableAndCorrectlyFlagged()
    {
        var root = FindRepositoryRoot();
        var modelNamesByRow = ReadPlaceableAppearanceModelNames(root);
        var failures = new List<string>();

        void CheckPlaceable(string themeKey, string role, string resref, bool requireInventory)
        {
            var utpPath = Path.Combine(root.FullName, "Module", "utp", $"{resref}.utp.json");
            if (!File.Exists(utpPath))
            {
                failures.Add($"{themeKey} {role}: '{resref}' has no utp blueprint.");
                return;
            }

            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(utpPath));
            var rootElement = document.RootElement;

            int GetIntField(string field) =>
                rootElement.TryGetProperty(field, out var f) && f.TryGetProperty("value", out var v)
                    ? v.GetInt32()
                    : 0;

            if (GetIntField("Useable") != 1)
                failures.Add($"{themeKey} {role}: '{resref}' is not Useable.");
            if (GetIntField("Static") != 0)
                failures.Add($"{themeKey} {role}: '{resref}' is Static (non-interactive).");
            if (requireInventory && GetIntField("HasInventory") != 1)
                failures.Add($"{themeKey} {role}: '{resref}' has no inventory; item creation will silently fail.");

            var appearance = GetIntField("Appearance");
            if (!modelNamesByRow.TryGetValue(appearance, out var modelName))
                failures.Add($"{themeKey} {role}: '{resref}' appearance row {appearance} does not exist in placeables.2da.");
            else if (string.IsNullOrEmpty(modelName) || modelName == "****")
                failures.Add($"{themeKey} {role}: '{resref}' appearance row {appearance} has a blank ModelName — it renders invisible.");
        }

        foreach (var (themeKey, detail) in BuildAllDungeons())
        {
            CheckPlaceable(themeKey, "exit", detail.ExitPlaceableResref, requireInventory: false);
            CheckPlaceable(themeKey, "treasure", detail.TreasurePlaceableResref, requireInventory: true);
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    /// <summary>Parses placeables.2da into row index -> ModelName (third column; "****" = blank).</summary>
    private static Dictionary<int, string> ReadPlaceableAppearanceModelNames(DirectoryInfo root)
    {
        var result = new Dictionary<int, string>();
        var path = Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "placeables.2da");

        foreach (var line in File.ReadLines(path).Skip(3))
        {
            var parts = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || !int.TryParse(parts[0], out var row))
                continue;

            // Column layout: RowIndex Label StrRef ModelName ... but Label may be a quoted string
            // with spaces. ModelName is the token after StrRef; find it by walking past the label:
            // simplest robust heuristic — quoted labels start with '"'; rejoin and skip to the
            // closing quote, then StrRef, then ModelName.
            string modelName;
            if (parts[1].StartsWith('"') && !parts[1].EndsWith('"'))
            {
                var idx = 2;
                while (idx < parts.Length && !parts[idx].EndsWith('"'))
                    idx++;
                modelName = idx + 2 < parts.Length ? parts[idx + 2] : "****";
            }
            else
            {
                modelName = parts.Length > 3 ? parts[3] : "****";
            }

            result[row] = modelName;
        }

        return result;
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

    private static Dictionary<string, DungeonTilesetProfile> BuildAllTilesetProfiles()
    {
        var profiles = new Dictionary<string, DungeonTilesetProfile>();

        foreach (var type in GetTypesImplementing<IDungeonTilesetProfileListDefinition>())
        {
            var definition = (IDungeonTilesetProfileListDefinition)Activator.CreateInstance(type)!;
            foreach (var (key, profile) in definition.BuildTilesetProfiles())
                profiles[key] = profile;
        }

        return profiles;
    }

    private static Dictionary<string, DungeonLayoutProfile> BuildAllLayoutProfiles()
    {
        var profiles = new Dictionary<string, DungeonLayoutProfile>();

        foreach (var type in GetTypesImplementing<IDungeonLayoutProfileListDefinition>())
        {
            var definition = (IDungeonLayoutProfileListDefinition)Activator.CreateInstance(type)!;
            foreach (var (key, profile) in definition.BuildLayoutProfiles())
                profiles[key] = profile;
        }

        return profiles;
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
