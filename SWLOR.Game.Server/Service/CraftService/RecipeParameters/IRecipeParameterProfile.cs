using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// One minigame's rules for turning a recipe into parameters. Derive from
    /// <see cref="RecipeParameterProfile{TParameters}"/> rather than implementing this directly.
    /// </summary>
    public interface IRecipeParameterProfile
    {
        /// <summary>A short unique name for the minigame, such as "Forge". It prefixes the Bible headers.</summary>
        string Key { get; }

        /// <summary>The skill whose recipes the profile covers.</summary>
        SkillType Skill { get; }

        /// <summary>The parameter type, whose constructor arguments are named like <see cref="Definitions"/>.</summary>
        Type ParametersType { get; }

        IReadOnlyList<RecipeParameterDefinition> Definitions { get; }

        RecipeParameterOverrides Overrides { get; }

        bool AppliesTo(RecipeParameterContext recipe);

        /// <summary>The value of every definition for a recipe, before the per-recipe variation.</summary>
        IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe);

        RecipeMinigameParameters Create(RecipeParameterValues values);

        /// <summary>One number for how hard the parameters make the minigame; higher is harder.</summary>
        double MeasureDifficulty(RecipeMinigameParameters parameters);

        /// <summary>The difficulty range allowed for recipes of this level.</summary>
        DifficultyBudget BudgetFor(int recipeLevel);
    }
}
