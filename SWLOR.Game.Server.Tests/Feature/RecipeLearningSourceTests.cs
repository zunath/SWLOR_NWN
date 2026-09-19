using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class RecipeLearningSourceTests
{
    [TestCase("recipe_trnsabers", "TrainingSaber")]
    [TestCase("recipe_trnsabstf", "TrainingSaberstaff")]
    public void TrainingBooks_TeachOriginalAndIntermediateTiers(string resref, string weapon)
    {
        using var document = ReadTemplate("uti", resref);
        var item = document.RootElement;
        var locals = ReadLocals(item);
        var expected = Enumerable.Range(1, 5).Select(rank => Enum.Parse<RecipeType>($"{weapon}{rank}"))
            .Concat(new[] { "Field", "Veteran", "Prime", "Ascendant" }
                .Select(tier => Enum.Parse<RecipeType>($"{tier}{weapon}")));

        ReadTaughtRecipes(item).Should().BeEquivalentTo(expected);
        GetString(item, "Tag").Should().Be("RECIPE");
        locals["CHARACTER_TYPE"].GetInt32().Should().Be(2, "training books remain Jedi-only");
        locals["NO_ECONOMY"].GetInt32().Should().Be(1, "these books remain DM-issued");
    }

    [Test]
    public void EveryLockedRecipe_HasAUsableTeachingBook()
    {
        var taught = ReadRecipeBooks().SelectMany(book => book.Recipes).ToHashSet();
        var missing = Definitions<IRecipeListDefinition>()
            .SelectMany(definition => definition.BuildRecipes())
            .Where(recipe => recipe.Value.IsActive &&
                recipe.Value.Requirements.OfType<RecipeUnlockRequirement>().Any() &&
                !taught.Contains(recipe.Key))
            .Select(recipe => recipe.Key).Distinct().ToList();

        missing.Should().BeEmpty("locked recipes need a teaching book, including DM-issued recipes");
    }

    [TestCase("recipe_trnsabers", "181,182,183,184,185")]
    [TestCase("recipe_trnsabstf", "202,203,204,205,206")]
    public void StoredTrainingBooks_GainCurrentTiersAndRemainUnchangedOnRetry(string resref, string previousRecipes)
    {
        using var document = ReadTemplate("uti", resref);
        var expected = ReadLocals(document.RootElement)["RECIPES"].GetString();

        var migrated = ExpandStoredTrainingBook(resref, previousRecipes);

        migrated.Should().Be(expected, "persisted books must teach the same tiers as newly issued books");
        ExpandStoredTrainingBook(resref, migrated).Should().Be(migrated);
    }

    [TestCase("recipe_trnsabers", "181,182,183,184,185,422,1")]
    [TestCase("recipe_trnsabstf", "202,203,204,205,206,423,1")]
    public void StoredTrainingBooks_PreserveCustomRecipesAndDoNotDuplicateExistingTiers(string resref, string previousRecipes)
    {
        using var document = ReadTemplate("uti", resref);
        var expected = ReadTaughtRecipes(document.RootElement).Append(RecipeType.BasicGreatSword);

        var migrated = ExpandStoredTrainingBook(resref, previousRecipes);
        var recipes = migrated.Split(',').Select(id => (RecipeType)int.Parse(id)).ToList();

        recipes.Should().BeEquivalentTo(expected);
        recipes.Should().OnlyHaveUniqueItems();
        ExpandStoredTrainingBook(resref, migrated).Should().Be(migrated);
    }

    [Test]
    public void StoredTrainingBookMigration_LeavesOtherBooksUnchanged()
    {
        ExpandStoredTrainingBook("recipe_sturd1", "3471").Should().Be("3471");
        ExpandStoredTrainingBook("custom_book", "181,182,183,184,185").Should().Be("181,182,183,184,185");
    }

    [Test]
    public void DroidResistanceBooks_UseDroidEnhancementTierValues()
    {
        var recipes = new DroidEnhancementRecipes().BuildRecipes().Keys
            .Where(recipe => recipe.ToString().StartsWith("DroidResistance", StringComparison.Ordinal)).ToHashSet();
        var books = ReadRecipeBooks().Where(book => book.Recipes.Overlaps(recipes)).ToList();
        books.Should().HaveCount(16);

        foreach (var book in books)
        {
            var expectedCost = book.Recipes.Single().ToString().EndsWith("2", StringComparison.Ordinal) ? 30000 : 10000;
            using var document = ReadTemplate("uti", book.Resref);
            foreach (var field in new[] { "AddCost", "Cost" })
                document.RootElement.GetProperty(field).GetProperty("value").GetInt32().Should()
                    .Be(expectedCost, $"{book.Resref}/{field} follows the existing droid enhancement blueprint tier values");
        }
    }

    private static string ExpandStoredTrainingBook(string resref, string recipes)
    {
        var method = typeof(IRecipeListDefinition).Assembly
            .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration.StoredItemDataMigration")!
            .GetMethod("ExpandTrainingRecipeBook", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, new object[] { resref, recipes })!;
    }

    [Test]
    public void DroidResistanceBooks_DropFromPlacedRareSpawnsOutsideCapstoneQuestEnemies()
    {
        var recipes = new DroidEnhancementRecipes().BuildRecipes().Keys
            .Where(recipe => recipe.ToString().StartsWith("DroidResistance", StringComparison.Ordinal))
            .ToHashSet();
        recipes.Should().HaveCount(16);
        var books = ReadRecipeBooks().Where(book => book.Recipes.Overlaps(recipes)).ToList();
        books.SelectMany(book => book.Recipes).Should().BeEquivalentTo(recipes);

        var loot = Definitions<ILootTableDefinition>()
            .SelectMany(definition => definition.BuildLootTables())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var spawnTables = Definitions<ISpawnListDefinition>()
            .SelectMany(definition => definition.BuildSpawnTables()).ToList();
        var placedTables = ReadPlacedSpawnTables();
        var rareSpawns = spawnTables.Where(table => placedTables.Contains(table.Key))
            .SelectMany(table => table.Value.Spawns)
            .Where(spawn => spawn.Type == ObjectType.Creature && spawn.IsRare && spawn.Weight > 0)
            .Select(spawn => spawn.Resref).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var questEnemies = CapstoneQuestDefinitionTestData.Lines
            .SelectMany(line => line.EnemyResrefs).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var creatureTables = Directory.EnumerateFiles(Path.Combine(FindRoot(), "Module", "utc"), "*.utc.json")
            .Select(path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return (Resref: GetString(document.RootElement, "TemplateResRef"),
                    Tables: ReadLootTables(document.RootElement));
            }).ToList();

        foreach (var book in books)
        {
            using var document = ReadTemplate("uti", book.Resref);
            var item = document.RootElement;
            GetString(item, "TemplateResRef").Should().Be(book.Resref);
            book.Resref.Length.Should().BeLessThanOrEqualTo(16);
            var locals = ReadLocals(item);
            locals.ContainsKey("CHARACTER_TYPE").Should().BeFalse("engineers of any character type can learn these recipes");
            locals.ContainsKey("NO_ECONOMY").Should().BeFalse("these blueprints are player loot");

            var tables = loot.Where(table => table.Value.Any(entry => entry.Resref == book.Resref)).ToList();
            tables.Should().NotBeEmpty($"{book.Resref} must drop from loot");
            foreach (var table in tables)
            {
                table.Key.Should().NotStartWith("CAPSTONE_", "capstone quest rewards must not supply these books");
                table.Value.IsRare.Should().BeTrue();
                var entry = table.Value.Single(entry => entry.Resref == book.Resref);
                entry.Weight.Should().BeGreaterThan(0);
                entry.MaxQuantity.Should().Be(1);
                entry.IsRare.Should().BeTrue();

                var consumers = creatureTables.Where(creature => creature.Tables.Contains(table.Key))
                    .Select(creature => creature.Resref).ToList();
                consumers.Should().NotBeEmpty($"{book.Resref}/{table.Key} must be used by a creature");
                consumers.Should().OnlyContain(resref => rareSpawns.Contains(resref),
                    $"{book.Resref} must come from rare NPCs in placed spawn tables");
                consumers.Should().NotIntersectWith(questEnemies,
                    $"{book.Resref} must not come from capstone quest bosses or wardens");
            }
        }
    }

    private static List<(string Resref, HashSet<RecipeType> Recipes)> ReadRecipeBooks()
    {
        var result = new List<(string, HashSet<RecipeType>)>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(FindRoot(), "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var item = document.RootElement;
            if (GetString(item, "Tag") != "RECIPE") continue;
            // Recipe books use the standard Unique Power (Self Only) item property.
            if (!GetList(item, "PropertiesList").Any(property =>
                    property.GetProperty("PropertyName").GetProperty("value").GetInt32() == 15 &&
                    property.GetProperty("Subtype").GetProperty("value").GetInt32() == 335)) continue;
            result.Add((GetString(item, "TemplateResRef"), ReadTaughtRecipes(item)));
        }
        return result;
    }

    private static HashSet<RecipeType> ReadTaughtRecipes(JsonElement item)
    {
        var recipeIds = ReadLocals(item)["RECIPES"].GetString()!.Split(',');
        return recipeIds.Select(id => (RecipeType)int.Parse(id)).ToHashSet();
    }

    private static HashSet<string> ReadLootTables(JsonElement creature)
    {
        return ReadLocals(creature)
            .Where(local => local.Key.StartsWith("LOOT_TABLE_", StringComparison.Ordinal) &&
                local.Value.ValueKind == JsonValueKind.String)
            .Select(local => local.Value.GetString()!.Split(','))
            .Where(parts => parts.Length == 1 || int.TryParse(parts[1], out var chance) && chance > 0)
            .Select(parts => parts[0].Trim()).ToHashSet();
    }

    private static HashSet<string> ReadPlacedSpawnTables()
    {
        var tables = new HashSet<string>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(FindRoot(), "Module", "git"), "*.git.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var waypoint in GetList(document.RootElement, "WaypointList"))
                tables.Add(GetString(waypoint, "Tag"));
            if (ReadLocals(document.RootElement).TryGetValue("CREATURE_SPAWN_TABLE_ID", out var table))
                tables.Add(table.GetString()!);
        }
        return tables;
    }

    private static IEnumerable<T> Definitions<T>() => typeof(T).Assembly.GetTypes()
        .Where(type => typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .Select(type => (T)Activator.CreateInstance(type)!);

    private static Dictionary<string, JsonElement> ReadLocals(JsonElement item) =>
        GetList(item, "VarTable").ToDictionary(local => GetString(local, "Name"),
            local => local.GetProperty("Value").GetProperty("value"));

    private static IEnumerable<JsonElement> GetList(JsonElement element, string name) =>
        element.TryGetProperty(name, out var wrapper)
            ? wrapper.GetProperty("value").EnumerateArray() : Enumerable.Empty<JsonElement>();

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var wrapper) ? wrapper.GetProperty("value").GetString()! : string.Empty;

    private static JsonDocument ReadTemplate(string type, string resref) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRoot(), "Module", type, $"{resref}.{type}.json")));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
