using System.Collections.Generic;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// The facts about a recipe that a profile turns into minigame parameters.
    /// </summary>
    public sealed class RecipeParameterContext
    {
        public RecipeType Type { get; }
        public SkillType Skill { get; }
        public RecipeCategoryType Category { get; }
        public int Level { get; }
        public bool IsActive { get; }
        public IReadOnlyDictionary<string, int> Components { get; }

        public RecipeParameterContext(
            RecipeType type,
            SkillType skill,
            RecipeCategoryType category,
            int level,
            bool isActive,
            IReadOnlyDictionary<string, int> components)
        {
            Type = type;
            Skill = skill;
            Category = category;
            Level = level;
            IsActive = isActive;
            Components = components;
        }

        public static RecipeParameterContext From(RecipeType type, RecipeDetail detail)
        {
            return new RecipeParameterContext(
                type,
                detail.Skill,
                detail.Category,
                detail.Level,
                detail.IsActive,
                detail.Components);
        }
    }
}
