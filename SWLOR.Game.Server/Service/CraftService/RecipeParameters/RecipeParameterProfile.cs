using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// Base class for a minigame's profile. A profile has a public parameterless constructor so the
    /// generator tool and the coverage tests can find it.
    /// </summary>
    public abstract class RecipeParameterProfile<TParameters> : IRecipeParameterProfile
        where TParameters : RecipeMinigameParameters
    {
        public abstract string Key { get; }
        public abstract SkillType Skill { get; }
        public abstract IReadOnlyList<RecipeParameterDefinition> Definitions { get; }
        public abstract DifficultyBudget BudgetFor(int recipeLevel);

        public Type ParametersType => typeof(TParameters);

        public virtual RecipeParameterOverrides Overrides => RecipeParameterOverrides.None;

        /// <summary>Every active recipe of the profile's skill, unless the profile narrows it.</summary>
        public virtual bool AppliesTo(RecipeParameterContext recipe)
        {
            return recipe.IsActive && recipe.Skill == Skill;
        }

        public abstract IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe);

        protected abstract TParameters Create(RecipeParameterValues values);

        protected abstract double MeasureDifficulty(TParameters parameters);

        RecipeMinigameParameters IRecipeParameterProfile.Create(RecipeParameterValues values)
        {
            return Create(values);
        }

        double IRecipeParameterProfile.MeasureDifficulty(RecipeMinigameParameters parameters)
        {
            return MeasureDifficulty((TParameters)parameters);
        }
    }
}
