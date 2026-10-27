using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the built recipe definitions: for every recipe its skill, category, level, practice limit, output item, enhancement
/// slots, research cost modifier, components, requirements and effect text. The recipes are split over several snapshot
/// files by skill and category group so that each file stays reviewable. A recipe holds no delegates; its requirements
/// are plain data and are written out. Whether the crafted item is intended for crafting is decided from the item with the
/// engine when the module loads, so it is not part of the data.
/// </summary>
public class CraftRecipeDefinitionCharacterizationTests
{
    private static readonly string[] RecipeProperties =
    {
        nameof(RecipeDetail.Category),
        nameof(RecipeDetail.Components),
        nameof(RecipeDetail.EffectLines),
        nameof(RecipeDetail.EnhancementSlots),
        nameof(RecipeDetail.EnhancementType),
        nameof(RecipeDetail.IsActive),
        nameof(RecipeDetail.IsItemIntendedForCrafting),
        nameof(RecipeDetail.Level),
        nameof(RecipeDetail.PracticeRankLimit),
        nameof(RecipeDetail.Quantity),
        nameof(RecipeDetail.Requirements),
        nameof(RecipeDetail.Resref),
        nameof(RecipeDetail.ResearchCostModifier),
        nameof(RecipeDetail.Skill),
    };

    private static readonly RecipeCategoryType[] SmitheryArmorCategories =
    {
        RecipeCategoryType.Cloak, RecipeCategoryType.Belt, RecipeCategoryType.Ring, RecipeCategoryType.Necklace,
        RecipeCategoryType.Breastplate, RecipeCategoryType.Helmet, RecipeCategoryType.Bracer, RecipeCategoryType.Legging,
        RecipeCategoryType.Shield, RecipeCategoryType.Tunic, RecipeCategoryType.Cap, RecipeCategoryType.Glove,
        RecipeCategoryType.Boots
    };

    private static readonly RecipeCategoryType[] EngineeringEnhancementCategories =
    {
        RecipeCategoryType.WeaponEnhancement, RecipeCategoryType.ArmorEnhancement, RecipeCategoryType.StarshipEnhancement,
        RecipeCategoryType.ModuleEnhancement, RecipeCategoryType.DroidEnhancement, RecipeCategoryType.StructureEnhancement
    };

    private static readonly RecipeCategoryType[] EngineeringStarshipCategories =
    {
        RecipeCategoryType.Starship, RecipeCategoryType.ShipModule, RecipeCategoryType.StarshipAmmo,
        RecipeCategoryType.CapitalConstruction, RecipeCategoryType.CapitalShipModule
    };

    private static readonly RecipeCategoryType[] EngineeringDroidPartCategories =
    {
        RecipeCategoryType.DroidComponent, RecipeCategoryType.DroidHead, RecipeCategoryType.DroidArms,
        RecipeCategoryType.DroidLegs, RecipeCategoryType.DroidBody
    };

    private static readonly RecipeCategoryType[] EngineeringDroidSoftwareCategories =
    {
        RecipeCategoryType.DroidCPU, RecipeCategoryType.DroidInstruction
    };

    private static readonly RecipeCategoryType[] EngineeringMusicCategories =
    {
        RecipeCategoryType.MusicPlayers, RecipeCategoryType.CantinaMusicDiscs, RecipeCategoryType.BattleMusicDiscs,
        RecipeCategoryType.ThemeMusicDiscs, RecipeCategoryType.LocationMusicDiscs, RecipeCategoryType.AmbientMusicDiscs
    };

    private static readonly string[] GroupNames =
    {
        "Agriculture", "Engineering.DroidParts", "Engineering.DroidSoftware", "Engineering.Enhancements", "Engineering.Gear",
        "Engineering.Music", "Engineering.Starship", "Espionage", "Fabrication", "Smithery.Armor", "Smithery.Weapons"
    };

    [TestCaseSource(nameof(GroupNames))]
    public void RecipeDefinitions(string groupName)
    {
        var recipes = RecipeDefinitionSource.Load();
        var group = recipes.Where(entry => GroupOf(entry.Recipe) == groupName).ToArray();

        var table = new SnapshotTable(
            $"Recipe definitions: {groupName} (every IRecipeListDefinition recipe in this group)",
            "The built data of every recipe in the group: skill, category, level, practice rank limit, quantity, crafted item, active flag, enhancement type and slots, " +
            "research cost modifier, components in the order the definition adds them (resref x quantity), requirements (class and field values in declaration order) " +
            "and the effect lines (count and fingerprint).",
            $"{group.Length} of the {recipes.Count} recipes, sorted by recipe id. Groups: Smithery by armor/accessory or weapon categories, Fabrication, Agriculture, Espionage and " +
            "Engineering by gear, enhancements, starship, droid parts, droid software and music. effectFingerprint is the first 12 hex digits of the SHA-256 of the lines joined with a line feed.",
            new[] { "recipe", "recipeName" },
            new[]
            {
                "skill", "category", "level", "practiceRankLimit", "quantity", "resref", "isActive", "enhancementType", "enhancementSlots",
                "researchCostModifier", "components", "requirements", "effectLineCount", "effectFingerprint"
            })
        {
            AllowConstantOutputs = true
        };

        foreach (var entry in group)
        {
            var recipe = entry.Recipe;
            table.Add(
                DefinitionCell.Id(entry.Type),
                entry.Type,
                recipe.Skill,
                recipe.Category,
                recipe.Level,
                recipe.PracticeRankLimit,
                recipe.Quantity,
                recipe.Resref,
                recipe.IsActive,
                recipe.EnhancementType,
                recipe.EnhancementSlots,
                recipe.ResearchCostModifier,
                recipe.Components.Select(component => $"{component.Key} x{component.Value}").ToArray(),
                recipe.Requirements.Select(RequirementText).ToArray(),
                recipe.EffectLines.Count,
                recipe.EffectLines.Count == 0 ? null : DefinitionCell.Fingerprint(string.Join("\n", recipe.EffectLines)));
        }

        Snapshot.Verify("Craft", "Recipes." + groupName, table);
    }

    [Test]
    public void RecipeGroups_AreTheOnesThatHaveSnapshots()
    {
        // A recipe that falls in a group without a snapshot would otherwise go unlocked.
        var actual = RecipeDefinitionSource.Load()
            .Select(entry => GroupOf(entry.Recipe))
            .Distinct()
            .OrderBy(group => group, StringComparer.Ordinal);

        Assert.That(actual, Is.EqualTo(GroupNames.OrderBy(group => group, StringComparer.Ordinal)));
    }

    [Test]
    public void RecipeDefinitionSource_FindsEveryDefinition()
    {
        // The recipe cache is built from every IRecipeListDefinition; a definition that builds nothing would be a silent gap.
        var built = RecipeDefinitionSource.Load().Select(entry => entry.Definition).Distinct().Count();
        Assert.That(built, Is.EqualTo(RecipeDefinitionSource.DefinitionTypes().Count));
    }

    [Test]
    public void EveryRecipeDetailProperty_IsLockedOrDeliberatelyLeftOut()
    {
        var properties = typeof(RecipeDetail)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal);

        Assert.That(properties, Is.EquivalentTo(RecipeProperties),
            "RecipeDetail gained or lost a property. Lock new plain data in RecipeDefinitions and add it to RecipeProperties.");
    }

    private static string GroupOf(RecipeDetail recipe)
    {
        return recipe.Skill switch
        {
            SkillType.Smithery => SmitheryArmorCategories.Contains(recipe.Category) ? "Smithery.Armor" : "Smithery.Weapons",
            SkillType.Fabrication => "Fabrication",
            SkillType.Agriculture => "Agriculture",
            SkillType.Espionage => "Espionage",
            SkillType.Engineering when EngineeringEnhancementCategories.Contains(recipe.Category) => "Engineering.Enhancements",
            SkillType.Engineering when EngineeringStarshipCategories.Contains(recipe.Category) => "Engineering.Starship",
            SkillType.Engineering when EngineeringDroidPartCategories.Contains(recipe.Category) => "Engineering.DroidParts",
            SkillType.Engineering when EngineeringDroidSoftwareCategories.Contains(recipe.Category) => "Engineering.DroidSoftware",
            SkillType.Engineering when EngineeringMusicCategories.Contains(recipe.Category) => "Engineering.Music",
            SkillType.Engineering => "Engineering.Gear",
            _ => "Other." + recipe.Skill
        };
    }

    // The requirement classes keep their data in private fields, so every instance field is read in declaration order:
    // RecipeUnlockRequirement(Recipe), RecipePerkRequirement(PerkType, level, perk name).
    private static string RequirementText(IRecipeRequirement requirement)
    {
        var fields = requirement.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderBy(field => field.MetadataToken)
            .Select(field => DefinitionCell.Text(field.GetValue(requirement)));
        return $"{requirement.GetType().Name}({string.Join(", ", fields)})";
    }
}
