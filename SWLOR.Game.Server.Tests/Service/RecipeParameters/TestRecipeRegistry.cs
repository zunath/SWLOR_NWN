using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>
/// Builds a small recipe registry whose parameters come from a profile's generator.
/// </summary>
public static class TestRecipeRegistry
{
    public const RecipeType InactiveRecipe = RecipeType.AmateurTunic;
    public const RecipeType OtherSkillRecipe = RecipeType.CombatTunic;

    private static readonly RecipeCategoryType[] _categories =
    {
        RecipeCategoryType.Breastplate,
        RecipeCategoryType.Tunic,
        RecipeCategoryType.Glove,
        RecipeCategoryType.Helmet,
    };

    public static Dictionary<RecipeType, RecipeDetail> Create(IRecipeParameterProfile profile)
    {
        var types = new[]
        {
            RecipeType.BattlemasterBreastplate,
            RecipeType.SpiritmasterTunic,
            OtherSkillRecipe,
            RecipeType.AdventTunic,
            InactiveRecipe,
        }.Concat(Enum.GetValues<RecipeType>().Where(type => type != RecipeType.Invalid).Take(60)).Distinct().ToList();

        var registry = new Dictionary<RecipeType, RecipeDetail>();
        for (var index = 0; index < types.Count; index++)
        {
            var type = types[index];
            var detail = new RecipeDetail
            {
                Skill = type == OtherSkillRecipe ? SkillType.Engineering : profile.Skill,
                Category = _categories[index % _categories.Length],
                Level = 1 + index * 3 % 50,
                IsActive = type != InactiveRecipe,
            };

            for (var component = 0; component <= index % 6; component++)
            {
                detail.Components["comp_" + component] = 1 + component;
            }

            if (detail.Skill == profile.Skill && detail.IsActive && !profile.DerivedRecipes.ContainsKey(type))
            {
                detail.MinigameParameters =
                    RecipeParameterGenerator.Generate(profile, RecipeParameterContext.From(type, detail)).Parameters;
            }

            registry[type] = detail;
        }

        foreach (var (derived, source) in profile.DerivedRecipes)
        {
            registry[derived].MinigameParameters = registry[source].MinigameParameters;
        }

        return registry;
    }
}
