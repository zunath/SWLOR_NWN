using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Data
{
    /// <summary>One active recipe as the old minigame sees it: its skill, level and component stacks.</summary>
    public sealed class RecipeEntry
    {
        public RecipeEntry(RecipeType type, RecipeDetail detail)
            : this(type, detail.Skill, detail.Level, detail.Resref, detail.Category, detail.Components, detail.Quantity, detail.EnhancementSlots)
        {
        }

        public RecipeEntry(
            RecipeType type,
            SkillType skill,
            int level,
            string resref,
            RecipeCategoryType category,
            IReadOnlyDictionary<string, int> components,
            int quantity = 1,
            int enhancementSlots = 0)
        {
            Type = type;
            Skill = skill;
            Level = level;
            Resref = resref;
            Category = category;
            Components = components;
            Quantity = quantity;
            EnhancementSlots = enhancementSlots;
        }

        public RecipeType Type { get; }
        public SkillType Skill { get; }
        public int Level { get; }
        public string Resref { get; }
        public RecipeCategoryType Category { get; }
        public IReadOnlyDictionary<string, int> Components { get; }
        public int Quantity { get; }
        public int EnhancementSlots { get; }

        /// <summary>Distinct component lines: one committed stack each when a player holds each in one stack.</summary>
        public int ComponentStacks => Components.Count;

        /// <summary>Total component units the recipe consumes.</summary>
        public int ComponentUnits => Components.Values.Sum();
    }
}
