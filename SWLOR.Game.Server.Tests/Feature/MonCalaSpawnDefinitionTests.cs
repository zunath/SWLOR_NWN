using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class MonCalaSpawnDefinitionTests
{
    private static readonly NamedRareEliteSpec[] NamedRareEliteSpecs =
    {
        new("reefmaw", "MONCALA_CORAL_ISLES_REEFMAW", "MONCALA_REEFMAW_RARES", "MONCALA_REEFMAW_SCALE", "tide_scale", "moncalacoralisle.git.json"),
        new("sable_quarr", "MONCALA_ECOTERRORISTS_SABLE_QUARR", "MONCALA_SABLE_QUARR_RARES", "MONCALA_SABLE_QUARR_CHIP", "field_chip", "moncalawildjungl.git.json"),
        new("kael_drox", "MONCALA_ECOTERRORIST_LEADER_KAEL_DROX", "MONCALA_KAEL_DROX_RARES", "MONCALA_KAEL_DROX_KEY", "command_key", "moncalacifacilit.git.json"),
        new("inkveil", "MONCALA_SUNKENHEAD_SWAMPS_INKVEIL", "MONCALA_INKVEIL_RARES", "MONCALA_INKVEIL_SAC", "midink_sac", "moncala_swamp.git.json"),
        new("glassjaw", "MONCALA_SHARPTOOTH_CAVERNS_GLASSJAW", "MONCALA_GLASSJAW_RARES", "MONCALA_GLASSJAW_CHITIN", "sh_chitin", "moncaladungeon1.git.json"),
    };

    private static readonly string[] RareEliteSourceNameFragments =
    {
        "reefmaw",
        "sable",
        "quarr",
        "kael",
        "drox",
        "inkveil",
        "glassjaw",
        "matron",
        "stalker",
        "tidebreaker",
    };

    [Test]
    public void MonCalaNamedRareElites_UseWeightedRareEntriesInDedicatedSpawnTables()
    {
        var tables = new MonCalaSpawnDefinition().BuildSpawnTables();
        var spawnEntries = tables
            .SelectMany(table => table.Value.Spawns.Select(spawn => new { TableId = table.Key, Spawn = spawn }))
            .ToArray();

        foreach (var spec in NamedRareEliteSpecs)
        {
            var entry = spawnEntries
                .Where(candidate => candidate.Spawn.Resref == spec.Resref)
                .Should()
                .ContainSingle($"{spec.Resref} should be wired to one spawn table only")
                .Subject;

            entry.TableId.Should().Be(spec.SpawnTableId);
            entry.Spawn.Type.Should().Be(ObjectType.Creature);
            entry.Spawn.Weight.Should().Be(1, "named rare elites should stay on the normal weighted frequency model");
            entry.Spawn.IsRare.Should().BeTrue();
        }
    }

    [Test]
    public void MonCalaNamedRareEliteSpawnTables_ArePlacedInOneAreaFile()
    {
        var root = FindRepositoryRoot();
        var gitFiles = Directory.GetFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json");

        foreach (var spec in NamedRareEliteSpecs)
        {
            var filesWithTable = gitFiles
                .Where(file => File.ReadAllText(file).Contains($"\"value\": \"{spec.SpawnTableId}\"", StringComparison.Ordinal))
                .Select(Path.GetFileName)
                .Distinct()
                .ToArray();

            filesWithTable.Should().BeEquivalentTo(
                new[] { spec.ModuleFile },
                $"{spec.Resref} should not appear through a spawn table reused by multiple Mon Cala area files");
        }
    }

    [Test]
    public void MonCalaNamedRareEliteLoot_DropsOneGuaranteedUniqueRecipeWithLowChanceSecondRoll()
    {
        var root = FindRepositoryRoot();
        var tables = new MonCalaLootTableDefinition().BuildLootTables();

        foreach (var spec in NamedRareEliteSpecs)
        {
            var expectedRecipeResrefs = GetNamedRareEliteRecipeEntries()
                .Where(entry => entry.RareComponent == spec.ComponentResref)
                .Select(GetNamedRareEliteRecipeResref)
                .ToArray();

            expectedRecipeResrefs.Length.Should().BeInRange(10, 20, $"{spec.Resref} should expose a 10-20 recipe named rare pool");

            var rareTable = tables[spec.RareLootTableId];
            rareTable.IsRare.Should().BeTrue();

            // Incubation field notes ride along in world-boss rare tables; the recipe pool contract below ignores them.
            var recipeItems = rareTable.Where(item => !item.Resref.StartsWith("fnote_")).ToArray();
            recipeItems.Should().HaveCount(expectedRecipeResrefs.Length);
            recipeItems.Should().OnlyContain(item => item.IsRare && item.MaxQuantity == 1 && item.Weight == 1);
            recipeItems.Select(item => item.Resref).Should().BeEquivalentTo(expectedRecipeResrefs);

            var componentTable = tables[spec.ComponentLootTableId];
            componentTable.IsRare.Should().BeFalse();
            componentTable.Should().ContainSingle(item =>
                item.Resref == spec.ComponentResref &&
                item.MaxQuantity == 1 &&
                item.Weight == 1);

            using var utc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "utc",
                $"{spec.Resref}.utc.json")));
            var lootLocals = GetLocalStringsWithPrefix(utc.RootElement, "LOOT_TABLE_").ToArray();

            lootLocals.Should().Contain($"{spec.RareLootTableId},100,1");
            lootLocals.Should().Contain($"{spec.RareLootTableId},10,1");
            lootLocals.Should().Contain($"{spec.ComponentLootTableId},100,1");
        }
    }

    [Test]
    public void MonCalaNamedRareEliteRecipeItems_LearnRegisteredRecipes()
    {
        var root = FindRepositoryRoot();

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            var recipeResref = GetNamedRareEliteRecipeResref(entry);
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{recipeResref}.uti.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTI ");
            GetLocalizedName(json).Should().StartWith("Blueprint: ");
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("RECIPE");
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(recipeResref);
            GetLocalString(json, "RECIPES").Should().Be(((int)entry.Recipe).ToString());
        }
    }

    [Test]
    public void MonCalaNamedRareEliteRecipes_CreateUnlockedOutputs()
    {
        var recipes = BuildNamedRareEliteRecipes();

        recipes.Should().HaveCount(GetNamedRareEliteRecipeEntries().Count());

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            var recipe = recipes[entry.Recipe];
            recipe.Skill.Should().Be(entry.Skill);
            recipe.Category.Should().Be(entry.Category);
            recipe.Resref.Should().Be(entry.CraftedResref);
            recipe.Level.Should().Be(entry.Level);
            recipe.Quantity.Should().Be(1);
            recipe.EnhancementType.Should().Be(entry.EnhancementType);
            recipe.EnhancementSlots.Should().Be(entry.EnhancementType == RecipeEnhancementType.None ? 0 : 1);
            recipe.Requirements.Count(requirement => requirement.GetType() == typeof(RecipeUnlockRequirement)).Should().Be(1);
            recipe.Components[entry.PrimaryComponent].Should().Be(entry.PrimaryQuantity);
            recipe.Components[entry.SecondaryComponent].Should().Be(entry.SecondaryQuantity);
            recipe.Components[entry.RareComponent].Should().Be(1);
        }
    }

    [Test]
    public void MonCalaNamedRareEliteOutputs_AreNewRecipeCraftedAssets()
    {
        var root = FindRepositoryRoot();

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{entry.CraftedResref}.uti.json")));

            var json = blueprint.RootElement;
            json.GetProperty("__data_type").GetString().Should().Be("UTI ");
            GetLocalizedName(json).Should().NotBeNullOrWhiteSpace();
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be(entry.CraftedResref);
            json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(entry.CraftedResref);

            if (entry.Skill == SkillType.Smithery)
            {
                var requiresSkill = GetItemProperty(json, ItemPropertyType.RequiresSkill);
                requiresSkill.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be((int)SkillType.Armor);

                GetItemPropertyCount(json, ItemPropertyType.DMG).Should().Be(0);
                GetItemPropertyCount(json, ItemPropertyType.Delay).Should().Be(0);
                GetItemPropertyCount(json, ItemPropertyType.UnlimitedAmmunition).Should().Be(0);
            }
            else if (entry.Skill == SkillType.Fabrication)
            {
                json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(29);
                AssertRegisteredPropertyStructure(entry.CraftedResref);
            }
            else if (entry.Skill == SkillType.Agriculture)
            {
                json.GetProperty("Charges").GetProperty("value").GetInt32().Should().Be(1);
                json.GetProperty("PaletteID").GetProperty("value").GetInt32().Should().Be(23);
                json.GetProperty("PropertiesList").GetProperty("value").GetArrayLength().Should().BeGreaterThan(0);
            }
            else
            {
                json.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(77);
                json.GetProperty("Charges").GetProperty("value").GetInt32().Should().Be(1);
                json.GetProperty("PaletteID").GetProperty("value").GetInt32().Should().Be(54);

                var activationProperty = GetItemProperty(json, ItemPropertyType.CastSpell);
                activationProperty.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be(335);
            }
        }
    }

    [Test]
    public void MonCalaNamedRareEliteEngineeringOutputs_AreRegisteredUsableItems()
    {
        var items = new Dictionary<string, ItemDetail>();
        var itemDefinitions = new IItemListDefinition[]
        {
            new TidecallBeaconItemDefinition(),
            new FluxDiverterItemDefinition(),
        };

        foreach (var itemDefinition in itemDefinitions)
        {
            foreach (var item in itemDefinition.BuildItems())
            {
                items[item.Key] = item.Value;
            }
        }

        var engineeringResrefs = BuildNamedRareEliteEngineeringRecipes()
            .Values
            .Select(recipe => recipe.Resref)
            .ToArray();

        items.Keys.Should().BeEquivalentTo(engineeringResrefs);

        foreach (var resref in engineeringResrefs)
        {
            var item = items[resref];
            item.ApplyAction.Should().NotBeNull();
            item.ReducesItemChargeAction.Should().NotBeNull();
            item.ActivationAnimation.Should().Be(Animation.LoopingGetMid);
            item.RecastGroup.Should().Be(RecastGroup.FieldTool);
            item.RecastCooldown.Should().Be(300f);
        }
    }

    [Test]
    public void MonCalaRareEliteLootItems_UseReusableNonEliteNaming()
    {
        var root = FindRepositoryRoot();
        var visibleTexts = new List<(string Source, string Text)>();

        void AddItemText(string resref)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                root.FullName,
                "Module",
                "uti",
                $"{resref}.uti.json")));
            var json = blueprint.RootElement;

            visibleTexts.Add((
                resref,
                string.Join(
                    " ",
                    GetLocalizedName(json),
                    GetLocalizedString(json, "Description"),
                    GetLocalizedString(json, "DescIdentified"))));
        }

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            AddItemText(GetNamedRareEliteRecipeResref(entry));
            AddItemText(entry.CraftedResref);
            visibleTexts.Add((entry.Recipe.ToString(), entry.Recipe.ToString()));
        }

        foreach (var spec in NamedRareEliteSpecs)
        {
            AddItemText(spec.ComponentResref);
        }

        foreach (var (source, text) in visibleTexts)
        {
            foreach (var fragment in RareEliteSourceNameFragments)
            {
                text.Should().NotContain(
                    fragment,
                    $"{source} should use reusable item naming instead of the source elite name");
            }
        }
    }

    [Test]
    public void MonCalaNamedRareEliteRecipes_AreDocumentedInRecipeBible()
    {
        var root = FindRepositoryRoot();
        using var archive = ZipFile.OpenRead(Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx"));
        var sharedStrings = ReadSharedStrings(archive);
        var smitheryWorksheet = ReadWorksheetByName(archive, "Smithery Recipes");
        var fabricationWorksheet = ReadWorksheetByName(archive, "Fabrication Recipes");
        var cookingWorksheet = ReadWorksheetByName(archive, "Cooking Recipes");
        var engineeringWorksheet = ReadWorksheetByName(archive, "Engineering Recipes");

        foreach (var entry in GetNamedRareEliteRecipeEntries())
        {
            var worksheet = entry.Skill switch
            {
                SkillType.Smithery => smitheryWorksheet,
                SkillType.Fabrication => fabricationWorksheet,
                SkillType.Agriculture => cookingWorksheet,
                SkillType.Engineering => engineeringWorksheet,
                _ => throw new InvalidOperationException($"Unsupported named rare elite recipe skill: {entry.Skill}")
            };
            var recipeResref = GetNamedRareEliteRecipeResref(entry);
            var craftedName = GetItemLocalizedName(root, entry.CraftedResref);
            var row = FindWorkbookRowByCellText(worksheet, sharedStrings, "B", recipeResref);

            GetWorkbookCellText(worksheet, sharedStrings, $"A{row}").Should().Be(entry.Skill.ToString());
            GetWorkbookCellText(worksheet, sharedStrings, $"B{row}").Should().Be(recipeResref);
            GetWorkbookCellText(worksheet, sharedStrings, $"D{row}").Should().Be(entry.Recipe.ToString());
            GetWorkbookCellText(worksheet, sharedStrings, $"E{row}").Should().Be(GetRecipeCategoryBibleName(entry));
            GetWorkbookCellText(worksheet, sharedStrings, $"G{row}").Should().Be(craftedName);
            GetWorkbookCellNumber(worksheet, sharedStrings, $"H{row}").Should().Be(entry.Level);
            GetWorkbookCellNumber(worksheet, sharedStrings, $"I{row}").Should().Be(1m);
            GetWorkbookCellText(worksheet, sharedStrings, $"J{row}").Should().Be(entry.CraftedResref);

            if (entry.Skill == SkillType.Smithery)
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be(GetSmitheryBlueprintCategory(entry.Category));
                GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"K{row}").Should().Be(RecipeEnhancementType.Armor.ToString());
                GetWorkbookCellNumber(worksheet, sharedStrings, $"L{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"M{row}").Should().Be(entry.PrimaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"N{row}").Should().Be(entry.PrimaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"O{row}").Should().Be(entry.SecondaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"P{row}").Should().Be(entry.SecondaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"Q{row}").Should().Be(entry.RareComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"R{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"AC{row}").Should().Be(0m);
            }
            else if (entry.Skill == SkillType.Fabrication)
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be("Furniture Blueprints");
                GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"K{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"L{row}").Should().Be(RecipeEnhancementType.Structure.ToString());
                GetWorkbookCellNumber(worksheet, sharedStrings, $"M{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"N{row}").Should().Be(entry.PrimaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"O{row}").Should().Be(entry.PrimaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"P{row}").Should().Be(entry.SecondaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"Q{row}").Should().Be(entry.SecondaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"R{row}").Should().Be(entry.RareComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"S{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"AD{row}").Should().Be(0m);
            }
            else if (entry.Skill == SkillType.Agriculture)
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be("Cooking Recipes");
                GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(GetRecipeBibleTier(entry.Level));
                GetWorkbookCellText(worksheet, sharedStrings, $"K{row}").Should().Be(RecipeEnhancementType.Food.ToString());
                GetWorkbookCellNumber(worksheet, sharedStrings, $"L{row}").Should().Be(1m);
                GetWorkbookCellText(worksheet, sharedStrings, $"M{row}").Should().Be(entry.PrimaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"N{row}").Should().Be(entry.PrimaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"O{row}").Should().Be(entry.SecondaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"P{row}").Should().Be(entry.SecondaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"Q{row}").Should().Be(entry.RareComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"R{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"AD{row}").Should().Be(0m);
            }
            else
            {
                GetWorkbookCellText(worksheet, sharedStrings, $"C{row}").Should().Be("Tool Blueprints");
                GetWorkbookCellNumber(worksheet, sharedStrings, $"F{row}").Should().Be(GetRecipeBibleTier(entry.Level));
                GetWorkbookCellText(worksheet, sharedStrings, $"K{row}").Should().Be("N/A");
                GetWorkbookCellNumber(worksheet, sharedStrings, $"L{row}").Should().Be(0m);
                GetWorkbookCellText(worksheet, sharedStrings, $"M{row}").Should().Be(entry.PrimaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"N{row}").Should().Be(entry.PrimaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"O{row}").Should().Be(entry.SecondaryComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"P{row}").Should().Be(entry.SecondaryQuantity);
                GetWorkbookCellText(worksheet, sharedStrings, $"Q{row}").Should().Be(entry.RareComponent);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"R{row}").Should().Be(1m);
                GetWorkbookCellNumber(worksheet, sharedStrings, $"AC{row}").Should().Be(0m);
            }
        }
    }

    private static NamedRareEliteRecipeSpec[] GetNamedRareEliteRecipeEntries()
    {
        return BuildNamedRareEliteRecipes()
            .Select(recipe => CreateNamedRareEliteRecipeSpec(recipe.Key, recipe.Value))
            .ToArray();
    }

    private static NamedRareEliteRecipeSpec CreateNamedRareEliteRecipeSpec(RecipeType recipe, RecipeDetail detail)
    {
        var rareComponents = new HashSet<string>(StringComparer.Ordinal)
        {
            "tide_scale",
            "field_chip",
            "command_key",
            "midink_sac",
            "sh_chitin",
        };
        var rareComponent = detail.Components.Keys.Single(rareComponents.Contains);
        var regularComponents = detail.Components
            .Where(component => component.Key != rareComponent)
            .ToArray();

        regularComponents.Should().HaveCount(2, $"{recipe} should use two regular components and one rare component");

        return new NamedRareEliteRecipeSpec(
            recipe,
            detail.Skill,
            detail.EnhancementType,
            detail.Category,
            detail.Resref,
            detail.Level,
            regularComponents[0].Key,
            regularComponents[0].Value,
            regularComponents[1].Key,
            regularComponents[1].Value,
            rareComponent);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteRecipes()
    {
        return BuildNamedRareEliteSmitheryRecipes()
            .Concat(BuildNamedRareEliteFabricationRecipes())
            .Concat(BuildNamedRareEliteCookingRecipes())
            .Concat(BuildNamedRareEliteEngineeringRecipes())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteSmitheryRecipes()
    {
        return new TideguardRecipes()
            .BuildRecipes()
            .Concat(new InkguardRecipes().BuildRecipes())
            .Concat(new ShatterhideRecipes().BuildRecipes())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteFabricationRecipes()
    {
        return new FieldlineFurnitureRecipes()
            .BuildRecipes()
            .Concat(new CommandlineFurnitureRecipes().BuildRecipes())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteCookingRecipes()
    {
        return new FieldProvisionRecipes()
            .BuildRecipes()
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static Dictionary<RecipeType, RecipeDetail> BuildNamedRareEliteEngineeringRecipes()
    {
        return new FieldToolRecipes()
            .BuildRecipes()
            .Where(pair => pair.Value.Components.Keys.Any(component => component is "field_chip" or "command_key"))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static string GetNamedRareEliteRecipeResref(NamedRareEliteRecipeSpec entry)
    {
        return "bp" + entry.CraftedResref.Replace("_", string.Empty);
    }

    private static void AssertRegisteredPropertyStructure(string resref)
    {
        resref.Should().StartWith("structure_");
        int.TryParse(resref["structure_".Length..], out var structureId).Should().BeTrue();
        Enum.IsDefined(typeof(StructureType), structureId).Should().BeTrue();

        var structure = (StructureType)structureId;
        var detail = structure.GetAttribute<StructureType, StructureAttribute>();
        detail.IsActive.Should().BeTrue();
        detail.Resref.Should().NotBeNullOrWhiteSpace();
        detail.LayoutType.Should().Be(PropertyLayoutType.Invalid);
    }

    private static IEnumerable<string> GetLocalStringsWithPrefix(JsonElement json, string prefix)
    {
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var entries))
        {
            yield break;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            var name = entry.GetProperty("Name").GetProperty("value").GetString();

            if (name != null && name.StartsWith(prefix, StringComparison.Ordinal))
            {
                yield return entry.GetProperty("Value").GetProperty("value").GetString()!;
            }
        }
    }

    private static string GetItemLocalizedName(DirectoryInfo root, string resref)
    {
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "uti",
            $"{resref}.uti.json")));

        return GetLocalizedName(blueprint.RootElement);
    }

    private static string GetLocalizedName(JsonElement json)
    {
        return json
            .GetProperty("LocalizedName")
            .GetProperty("value")
            .GetProperty("0")
            .GetString() ?? string.Empty;
    }

    private static string GetLocalizedString(JsonElement json, string property)
    {
        if (!json.TryGetProperty(property, out var locString) ||
            !locString.TryGetProperty("value", out var value) ||
            !value.TryGetProperty("0", out var text))
        {
            return string.Empty;
        }

        return text.GetString() ?? string.Empty;
    }

    private static string GetLocalString(JsonElement json, string variableName)
    {
        return json
            .GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Where(entry => entry.GetProperty("Name").GetProperty("value").GetString() == variableName)
            .Select(entry => entry.GetProperty("Value").GetProperty("value").GetString())
            .Single() ?? string.Empty;
    }

    private static int GetItemPropertyCount(JsonElement json, ItemPropertyType propertyType)
    {
        return json
            .GetProperty("PropertiesList")
            .GetProperty("value")
            .EnumerateArray()
            .Count(property => property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)propertyType);
    }

    private static JsonElement GetItemProperty(JsonElement json, ItemPropertyType propertyType)
    {
        return json
            .GetProperty("PropertiesList")
            .GetProperty("value")
            .EnumerateArray()
            .Single(property => property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)propertyType);
    }

    private static XDocument ReadWorkbookXml(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull($"{entryName} should exist in the combat Bible workbook");

        using var stream = entry!.Open();
        return XDocument.Load(stream);
    }

    private static XDocument ReadWorksheetByName(ZipArchive archive, string sheetName)
    {
        var workbook = ReadWorkbookXml(archive, "xl/workbook.xml");
        var relationships = ReadWorkbookXml(archive, "xl/_rels/workbook.xml.rels");
        XNamespace workbookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        var sheet = workbook
            .Descendants(workbookNs + "sheet")
            .Single(candidate => candidate.Attribute("name")?.Value == sheetName);
        var relationshipId = sheet.Attribute(relationshipNs + "id")?.Value;
        relationshipId.Should().NotBeNullOrWhiteSpace($"{sheetName} should have a workbook relationship id");

        var target = relationships
            .Descendants(packageRelationshipNs + "Relationship")
            .Single(candidate => candidate.Attribute("Id")?.Value == relationshipId)
            .Attribute("Target")?
            .Value
            .Replace('\\', '/');
        target.Should().NotBeNullOrWhiteSpace($"{sheetName} should resolve to a worksheet XML target");

        var entryName = target!.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : $"xl/{target}";
        return ReadWorkbookXml(archive, entryName);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return Array.Empty<string>();

        var sharedStrings = ReadWorkbookXml(archive, "xl/sharedStrings.xml");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return sharedStrings
            .Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static int FindWorkbookRowByCellText(XDocument worksheet, IReadOnlyList<string> sharedStrings, string column, string text)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return worksheet
            .Descendants(ns + "row")
            .Select(row => int.Parse(row.Attribute("r")?.Value ?? "0", CultureInfo.InvariantCulture))
            .Single(row => GetWorkbookCellText(worksheet, sharedStrings, $"{column}{row}") == text);
    }

    private static string GetWorkbookCellText(XDocument worksheet, IReadOnlyList<string> sharedStrings, string address)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var cell = worksheet
            .Descendants(ns + "c")
            .SingleOrDefault(candidate => candidate.Attribute("r")?.Value == address);

        if (cell == null)
            return string.Empty;

        var type = cell.Attribute("t")?.Value;
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));

        var value = cell.Element(ns + "v")?.Value;
        if (type == "s" && int.TryParse(value, out var index))
            return sharedStrings[index];

        return value ?? string.Empty;
    }

    private static decimal GetWorkbookCellNumber(XDocument worksheet, IReadOnlyList<string> sharedStrings, string address)
    {
        var text = GetWorkbookCellText(worksheet, sharedStrings, address);
        return decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static string GetSmitheryBlueprintCategory(RecipeCategoryType category)
    {
        return category switch
        {
            RecipeCategoryType.Belt or
                RecipeCategoryType.Cloak or
                RecipeCategoryType.Necklace or
                RecipeCategoryType.Ring => "Accessory Blueprints",
            _ => "Armor Blueprints"
        };
    }

    private static string GetRecipeCategoryBibleName(NamedRareEliteRecipeSpec entry)
    {
        return entry.Skill == SkillType.Fabrication && entry.CraftedResref.EndsWith("_locker", StringComparison.Ordinal)
            ? "MiscellaneousFurniture"
            : entry.Category.ToString();
    }

    private static decimal GetRecipeBibleTier(int level)
    {
        return level switch
        {
            <= 10 => 1m,
            <= 20 => 2m,
            <= 30 => 3m,
            <= 40 => 4m,
            _ => 5m
        };
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record NamedRareEliteSpec(
        string Resref,
        string SpawnTableId,
        string RareLootTableId,
        string ComponentLootTableId,
        string ComponentResref,
        string ModuleFile);

    private sealed record NamedRareEliteRecipeSpec(
        RecipeType Recipe,
        SkillType Skill,
        RecipeEnhancementType EnhancementType,
        RecipeCategoryType Category,
        string CraftedResref,
        int Level,
        string PrimaryComponent,
        int PrimaryQuantity,
        string SecondaryComponent,
        int SecondaryQuantity,
        string RareComponent);
}
