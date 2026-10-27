using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the crafting calculations that run without the engine: the recipe level chart, the skill rank a recipe needs,
/// the XP a craft grants, the blueprint research cost, which enhancements add weapon damage, recipe search and the
/// recipe category and enhancement metadata. Item property creation, blueprint storage and the crafting window need the
/// engine (Tier B).
/// </summary>
[NonParallelizable]
public class CraftCalculationCharacterizationTests
{
    [OneTimeSetUp]
    public void BuildChart()
    {
        // GetBaseRecipeXP reads the rank-difference table, which the module load builds with this call.
        Skill.CacheXPChartData();
    }

    [Test]
    public void PublicConstants()
    {
        var table = new SnapshotTable(
            "Craft (public const fields)",
            "The highest blueprint research level and the offset between a recipe's level and the skill rank needed to craft it.",
            "All public const fields of Craft, sorted by name.",
            new[] { "constant" },
            new[] { "type", "value" })
        {
            AllowConstantOutputs = true
        };

        var constants = typeof(Craft)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .OrderBy(field => field.Name, StringComparer.Ordinal);
        foreach (var field in constants)
            table.Add(field.Name, field.FieldType.Name, field.GetRawConstantValue());

        Snapshot.Verify("Craft", "PublicConstants", table);
    }

    [Test]
    public void GetRecipeLevelDetail()
    {
        var table = new SnapshotTable(
            "Craft.GetRecipeLevelDetail(int level)",
            "The crafting minigame numbers of a recipe level: progress and quality to reach, durability, and the difficulty adjustment. " +
            "A level outside the chart throws.",
            "Every level 1..80 and the neighbours outside the chart (-1, 0, 81, 82).",
            new[] { "level" },
            new[] { "progress", "quality", "durability", "difficultyAdjustment" });

        foreach (var level in Enumerable.Range(-1, 84))
        {
            RecipeLevelDetail? detail = null;
            string? failure = null;
            try
            {
                detail = Craft.GetRecipeLevelDetail(level);
            }
            catch (Exception exception)
            {
                failure = Outcome.Describe(exception);
            }

            if (detail == null)
                table.Add(level, failure, failure, failure, failure);
            else
                table.Add(level, detail.Progress, detail.Quality, detail.Durability, detail.DifficultyAdjustment);
        }

        Snapshot.Verify("Craft", "GetRecipeLevelDetail", table);
    }

    [Test]
    public void GetRequiredSkillRankForRecipe()
    {
        var table = new SnapshotTable(
            "Craft.GetRequiredSkillRankForRecipe(RecipeDetail)",
            "The skill rank needed to craft a recipe: its level minus 3, never below 0.",
            "Recipe levels {-5,0,1,2,3,4,5,10,25,50,53,80,1000}.",
            new[] { "recipeLevel" },
            new[] { "requiredSkillRank" });

        foreach (var level in new[] { -5, 0, 1, 2, 3, 4, 5, 10, 25, 50, 53, 80, 1000 })
            table.Add(level, Craft.GetRequiredSkillRankForRecipe(new RecipeDetail { Level = level }));

        Snapshot.Verify("Craft", "GetRequiredSkillRankForRecipe", table);
    }

    [Test]
    public void GetBaseRecipeXP()
    {
        var table = new SnapshotTable(
            "Craft.GetBaseRecipeXP(RecipeDetail, int skillRank)",
            "The XP a craft grants before bonuses: the rank-difference XP of the recipe level against the skill rank, or, for a recipe with a " +
            "practice rank limit, the practice XP (at least 150 until the skill rank reaches the limit, then nothing).",
            "recipe level {1,5,10,20,30,50,53} x skill rank {0,1,4,5,9,10,19,20,29,30,49,50,60} x practice rank limit {null,10,30,50}.",
            new[] { "recipeLevel", "skillRank", "practiceRankLimit" },
            new[] { "xp" });

        foreach (var level in new[] { 1, 5, 10, 20, 30, 50, 53 })
        {
            foreach (var rank in new[] { 0, 1, 4, 5, 9, 10, 19, 20, 29, 30, 49, 50, 60 })
            {
                foreach (var limit in new int?[] { null, 10, 30, 50 })
                {
                    var recipe = new RecipeDetail { Level = level, PracticeRankLimit = limit };
                    table.Add(level, rank, limit, Craft.GetBaseRecipeXP(recipe, rank));
                }
            }
        }

        Snapshot.Verify("Craft", "GetBaseRecipeXP", table);
    }

    [Test]
    public void BlueprintResearchCosts()
    {
        var table = new SnapshotTable(
            "Craft.CalculateBlueprintResearchCreditCost(RecipeType, int blueprintLevel, int reductionBonus) / Craft.CalculateBlueprintResearchSeconds",
            "The credits and the seconds to research a blueprint to a level: 200 x (recipe tier ^ 2.2) x (blueprint level ^ 2), where the recipe tier is its level / 10 + 1 " +
            "(at most 5), raised by the recipe's research cost modifier and lowered by the percentage reduction; truncated to a whole number. " +
            "Both functions use the same formula.",
            "A recipe registered in the recipe cache under RecipeType.Invalid: level {0,1,9,10,20,40,50,53,100} x research cost modifier {-0.5,0,0.25} x " +
            "(blueprint level, reduction %) {(1,0),(3,25),(10,0),(10,100)}.",
            new[] { "recipeLevel", "researchCostModifier", "blueprintLevel", "reductionBonus" },
            new[] { "creditCost", "researchSeconds" });

        var recipe = new RecipeDetail();
        using var cache = new CraftRecipeCacheScope(RecipeType.Invalid, recipe);

        foreach (var level in new[] { 0, 1, 9, 10, 20, 40, 50, 53, 100 })
        {
            foreach (var modifier in new[] { -0.5f, 0f, 0.25f })
            {
                foreach (var (blueprintLevel, reduction) in new[] { (1, 0), (3, 25), (10, 0), (10, 100) })
                {
                    recipe.Level = level;
                    recipe.ResearchCostModifier = modifier;
                    table.Add(
                        level,
                        modifier,
                        blueprintLevel,
                        reduction,
                        Craft.CalculateBlueprintResearchCreditCost(RecipeType.Invalid, blueprintLevel, reduction),
                        Craft.CalculateBlueprintResearchSeconds(RecipeType.Invalid, blueprintLevel, reduction));
                }
            }
        }

        Snapshot.Verify("Craft", "BlueprintResearchCosts", table);
    }

    [Test]
    public void WeaponDamageEnhancement()
    {
        var table = new SnapshotTable(
            "Craft.IsWeaponDamageEnhancement / Craft.TryGetWeaponDamageTypeForEnhancement",
            "Whether an enhancement adds weapon damage (only DMG does) and the damage type it uses: the chosen type when it is one a character can deal, " +
            "otherwise physical; a Force choice becomes physical because Force conversion belongs to Imbuement Stance. Other enhancements give no damage type.",
            "enhancements {Invalid, DefensePhysical, Attack, Accuracy, DMG} x every CombatDamageType member.",
            new[] { "enhancement", "explicitDamageType" },
            new[] { "isWeaponDamageEnhancement", "result", "damageType" });

        foreach (var subType in new[]
                 {
                     EnhancementSubType.Invalid, EnhancementSubType.DefensePhysical, EnhancementSubType.Attack, EnhancementSubType.Accuracy, EnhancementSubType.DMG
                 })
        {
            foreach (var damageType in Enum.GetValues<CombatDamageType>().OrderBy(type => DefinitionCell.Id(type)))
            {
                var result = Craft.TryGetWeaponDamageTypeForEnhancement(subType, damageType, out var resolved);
                table.Add(subType, damageType, Craft.IsWeaponDamageEnhancement(subType), result, resolved);
            }
        }

        Snapshot.Verify("Craft", "WeaponDamageEnhancement", table);
    }

    [Test]
    public void IsResearchableRecipe()
    {
        var method = typeof(Craft).GetMethod("IsResearchableRecipe", BindingFlags.Static | BindingFlags.NonPublic)!;

        var table = new SnapshotTable(
            "Craft.IsResearchableRecipe(RecipeDetail) (private, read by reflection)",
            "Which kinds of recipe can be researched into blueprints: weapon, armor and food enhancement recipes.",
            "Every RecipeEnhancementType member.",
            new[] { "enhancementType" },
            new[] { "isResearchable" });

        foreach (var type in Enum.GetValues<RecipeEnhancementType>().OrderBy(type => DefinitionCell.Id(type)))
            table.Add(type, method.Invoke(null, new object[] { new RecipeDetail { EnhancementType = type } }));

        Snapshot.Verify("Craft", "IsResearchableRecipe", table);
    }

    [Test]
    public void RecipeSearch_Matches()
    {
        var names = new Dictionary<string, string>
        {
            ["r_sword"] = "Long Sword",
            ["c_steel"] = "Steel Plate",
            ["c_wire"] = "Copper Wire",
            ["c_unnamed"] = null!
        };
        string? NameOf(string resref) => names.GetValueOrDefault(resref);

        var recipes = new (string Name, RecipeDetail Recipe)[]
        {
            ("sword with components", new RecipeDetail
            {
                Resref = "r_sword",
                Components = new Dictionary<string, int> { ["c_steel"] = 2, ["c_wire"] = 1 }
            }),
            ("sword without components", new RecipeDetail { Resref = "r_sword", Components = null! }),
            ("unnamed item with components", new RecipeDetail
            {
                Resref = "r_unknown",
                Components = new Dictionary<string, int> { ["c_steel"] = 1, ["c_unnamed"] = 1 }
            }),
        };

        var table = new SnapshotTable(
            "RecipeSearch.Matches(RecipeDetail, string searchText, bool includeComponents, Func<string, string> getItemName)",
            "Whether a recipe matches the text a player searched for: a blank search matches everything, otherwise the trimmed text must be part of the crafted " +
            "item's name or, when components are included, of any component's name; the comparison ignores case and an unnamed item never matches.",
            "3 recipes x search texts {null, \"\", \"  \", \"sword\", \"SWORD\", \" sword \", \"steel\", \"Wire\", \"plate\", \"xyz\", \"Long Sword\", \"w\"} x include components {false, true}. " +
            "Names: r_sword = Long Sword, c_steel = Steel Plate, c_wire = Copper Wire; r_unknown and c_unnamed have no name.",
            new[] { "recipe", "searchText", "includeComponents" },
            new[] { "matches" });

        foreach (var (name, recipe) in recipes)
        {
            foreach (var text in new[] { null, "", "  ", "sword", "SWORD", " sword ", "steel", "Wire", "plate", "xyz", "Long Sword", "w" })
            {
                foreach (var includeComponents in new[] { false, true })
                    table.Add(name, text, includeComponents, RecipeSearch.Matches(recipe, text!, includeComponents, NameOf!));
            }
        }

        Snapshot.Verify("Craft", "RecipeSearch.Matches", table);
    }

    [Test]
    public void RecipeCategories()
    {
        var table = new SnapshotTable(
            "RecipeCategoryType metadata",
            "The display name and active flag every recipe category declares through its RecipeCategory attribute.",
            "Every RecipeCategoryType member, sorted by id.",
            new[] { "category", "categoryName" },
            new[] { "displayName", "isActive" })
        {
            AllowConstantOutputs = true
        };

        foreach (var category in Enum.GetValues<RecipeCategoryType>().OrderBy(category => DefinitionCell.Id(category)))
        {
            var attribute = category.GetAttribute<RecipeCategoryType, RecipeCategoryAttribute>();
            table.Add(DefinitionCell.Id(category), category, attribute.Name, attribute.IsActive);
        }

        Snapshot.Verify("Craft", "RecipeCategories", table);
    }

    [Test]
    public void EnhancementSubTypes()
    {
        var table = new SnapshotTable(
            "EnhancementSubType metadata",
            "The display name every enhancement sub type declares through its EnhancementSubType attribute.",
            "Every EnhancementSubType member, sorted by id.",
            new[] { "subType", "subTypeName" },
            new[] { "displayName" });

        foreach (var subType in Enum.GetValues<EnhancementSubType>().OrderBy(subType => DefinitionCell.Id(subType)))
        {
            var attribute = subType.GetAttribute<EnhancementSubType, EnhancementSubTypeAttribute>();
            table.Add(DefinitionCell.Id(subType), subType, attribute.Name);
        }

        Snapshot.Verify("Craft", "EnhancementSubTypes", table);
    }
}
