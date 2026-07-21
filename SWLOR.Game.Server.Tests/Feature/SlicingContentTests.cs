using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EspionageRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Tests.Feature;

public class SlicingContentTests
{
    private static readonly Dictionary<string, int> TerminalAreas = new()
    {
        ["czs220_maintlvl"] = 1, ["nanostation015"] = 1, ["viscarawildlands"] = 1,
        ["viscara_wwnorth"] = 2, ["viscaradeepmount"] = 2, ["v_cox_base"] = 2,
        ["korr_ravine"] = 3, ["korr_cavern"] = 3, ["korr_crypt_zil"] = 3,
        ["hutlar_qion"] = 4, ["pw_ar_narslum"] = 4, ["tat_anc_hillydes"] = 4,
        ["dan_jantacaves"] = 5, ["dath_mountains"] = 5, ["tat_wormden"] = 5,
    };

    [Test]
    public void DirectRewardCatalog_BlueprintsExistAndUseArmorRequirementsWithoutRawAttributes()
    {
        var root = FindRepositoryRoot();
        foreach (var reward in SlicingRewardCatalog.Entries)
        {
            var path = Path.Combine(root, "Module", "uti", reward.Resref + ".uti.json");
            File.Exists(path).Should().BeTrue($"{reward.Resref} is a direct slicing reward");

            if (reward.Category != SlicingRewardCategory.NamedItem)
                continue;

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var properties = document.RootElement.GetProperty("PropertiesList").GetProperty("value");
            properties.EnumerateArray().Should().NotContain(property => GetInt(property, "PropertyName") == 0,
                $"{reward.Name} must not grant raw Might, Social, Vitality, Willpower, Perception, or Agility");

            var armorRequirement = properties.EnumerateArray().Single(property =>
                GetInt(property, "PropertyName") == 131 && GetInt(property, "Subtype") == 6);
            var expectedLevel = (reward.Tier - 1) * 10 + (reward.IsExceptional ? 5 : 0);
            GetInt(armorRequirement, "CostValue").Should().Be(expectedLevel,
                $"{reward.Name} scales from Armor rather than Espionage");
        }
    }

    [Test]
    public void TerminalAreas_HaveOneSharedTieredSpawnNode()
    {
        var root = FindRepositoryRoot();
        foreach (var (areaResref, tier) in TerminalAreas)
        {
            var path = Path.Combine(root, "Module", "git", areaResref + ".git.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var locals = document.RootElement.GetProperty("VarTable").GetProperty("value")
                .EnumerateArray()
                .ToDictionary(entry => GetString(entry, "Name"), entry => entry.GetProperty("Value").GetProperty("value"));

            locals["SLICING_TERMINAL_SPAWN_TABLE_ID"].GetString().Should().Be($"SLICING_TERMINAL_T{tier}");
            locals["SLICING_TERMINAL_SPAWN_COUNT"].GetInt32().Should().Be(1);
        }
    }

    [Test]
    public void TerminalSpawnTables_UseTierBlueprintsAndFortyFiveToSeventyFiveMinuteRespawns()
    {
        var tables = new SlicingTerminalSpawnDefinition().BuildSpawnTables();
        tables.Should().HaveCount(5);
        for (var tier = 1; tier <= 5; tier++)
        {
            var table = tables[$"SLICING_TERMINAL_T{tier}"];
            table.RespawnDelayMinutes.Should().Be(45);
            table.RespawnDelayMaximumMinutes.Should().Be(75);
            table.Spawns.Should().ContainSingle(spawn => spawn.Resref == $"slice_term_{tier}");
        }
    }

    [Test]
    public void SlicingRecipes_CoverAllCraftsAndPreserveAgricultureAsAPoisonComponentSource()
    {
        AssertRecipeSet(new SlicingCacheSmitheryRecipes().BuildRecipes(), SkillType.Smithery, 5, RecipeEnhancementType.Armor);
        AssertRecipeSet(new SlicingCacheCookingRecipes().BuildRecipes(), SkillType.Agriculture, 5, RecipeEnhancementType.Food);
        AssertRecipeSet(new TraceFuseRecipes().BuildRecipes(), SkillType.Engineering, 5, RecipeEnhancementType.None);
        AssertRecipeSet(new SlicingTerminalFurnitureRecipes().BuildRecipes(), SkillType.Fabrication, 5, RecipeEnhancementType.Structure);

        var concentrates = new ConcentratedVenomRecipes().BuildRecipes().Values.ToList();
        concentrates.Should().HaveCount(5);
        concentrates.Should().OnlyContain(recipe =>
            recipe.Skill == SkillType.Espionage &&
            recipe.Quantity == 1 &&
            recipe.Components.Keys.Any(component => component.StartsWith("herb_")));
    }

    [Test]
    public void SlicingStructures_HaveOneStorageAndUnusedAppearances()
    {
        var root = FindRepositoryRoot();
        var expected = new Dictionary<StructureType, (string Resref, int Appearance)>
        {
            [StructureType.RustlineDataTerminal] = ("slc_rustterm", 6031),
            [StructureType.CipherfileCabinet] = ("slc_ciphcab", 30935),
            [StructureType.ListeningPostMonitor] = ("slc_listmon", 7201),
            [StructureType.GhostChannelConsole] = ("slc_ghostcon", 21450),
            [StructureType.BlacksiteAnalysisStation] = ("slc_blackstat", 30611),
        };

        var appearances = Directory.EnumerateFiles(Path.Combine(root, "Module", "utp"), "*.utp.json")
            .Select(path => JsonDocument.Parse(File.ReadAllText(path)))
            .ToList();
        try
        {
            foreach (var (type, detail) in expected)
            {
                var attribute = typeof(StructureType).GetField(type.ToString())!.GetCustomAttribute<StructureAttribute>();
                attribute.Should().NotBeNull();
                attribute!.Resref.Should().Be(detail.Resref);
                attribute.ItemStorage.Should().Be(1);
                appearances.Count(document => GetInt(document.RootElement, "Appearance") == detail.Appearance).Should().Be(1,
                    $"appearance {detail.Appearance} was selected because it had no existing module use");
            }
        }
        finally
        {
            foreach (var document in appearances)
                document.Dispose();
        }
    }

    [Test]
    public void SlicingNuiArt_HasEveryThemeTypeOrientationAndState()
    {
        var ui = Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_ui");
        foreach (var theme in new[] { 'l', 't' })
        {
            AssertTga(Path.Combine(ui, $"slc_bg_{theme}.tga"), 640, 96);
            foreach (var type in new[] { 's', 'c', 'j', 'x', 'e', 'o', 'b', 'q' })
            foreach (var orientation in Enumerable.Range(0, 4))
            foreach (var state in new[] { 'u', 'p', 's', 'd' })
            {
                var resref = $"slc{theme}{type}{orientation}{state}";
                resref.Length.Should().BeLessThanOrEqualTo(16);
                AssertTga(Path.Combine(ui, resref + ".tga"), 72, 72);
            }
        }
    }

    private static void AssertRecipeSet(
        Dictionary<RecipeType, RecipeDetail> recipes,
        SkillType skill,
        int count,
        RecipeEnhancementType enhancementType)
    {
        recipes.Should().HaveCount(count);
        recipes.Values.Should().OnlyContain(recipe => recipe.Skill == skill);
        if (enhancementType == RecipeEnhancementType.None)
            recipes.Values.Should().OnlyContain(recipe => recipe.EnhancementSlots == 0);
        else
            recipes.Values.Should().OnlyContain(recipe => recipe.EnhancementType == enhancementType && recipe.EnhancementSlots == 1);
    }

    private static void AssertTga(string path, int width, int height)
    {
        File.Exists(path).Should().BeTrue(path);
        var header = File.ReadAllBytes(path).Take(18).ToArray();
        header.Should().HaveCount(18);
        (header[12] | header[13] << 8).Should().Be(width);
        (header[14] | header[15] << 8).Should().Be(height);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static int GetInt(JsonElement element, string property) =>
        element.GetProperty(property).GetProperty("value").GetInt32();

    private static string GetString(JsonElement element, string property) =>
        element.GetProperty(property).GetProperty("value").GetString() ?? string.Empty;
}
