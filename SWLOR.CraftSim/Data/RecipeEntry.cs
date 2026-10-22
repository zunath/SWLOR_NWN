using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Data
{
    /// <summary>One active recipe as the old minigame sees it: its skill, level and component stacks.</summary>
    public sealed class RecipeEntry
    {
        public RecipeEntry(RecipeType type, RecipeDetail detail)
            : this(
                type, detail.Skill, detail.Level, detail.Resref, detail.Category, detail.Components, detail.Quantity, detail.EnhancementSlots,
                detail.PracticeRankLimit, detail)
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
            int enhancementSlots = 0,
            int? practiceRankLimit = null,
            RecipeDetail detail = null,
            RecipeEnhancementType enhancementType = RecipeEnhancementType.None)
        {
            Type = type;
            Skill = skill;
            Level = level;
            Resref = resref;
            Category = category;
            Components = components;
            Quantity = quantity;
            EnhancementSlots = enhancementSlots;
            PracticeRankLimit = practiceRankLimit;
            Detail = detail;
            EnhancementType = detail?.EnhancementType ?? enhancementType;
        }

        public RecipeType Type { get; }
        public SkillType Skill { get; }
        public int Level { get; }
        public string Resref { get; }
        public RecipeCategoryType Category { get; }
        public IReadOnlyDictionary<string, int> Components { get; }
        public int Quantity { get; }
        public int EnhancementSlots { get; }

        /// <summary>The kind of enhancement item the recipe accepts (armor, weapon, structure, food, starship, module or droid), or none.</summary>
        public RecipeEnhancementType EnhancementType { get; }

        /// <summary>Set for a tiered activity such as Espionage's, whose base XP comes from <c>Skill.GetPracticeXP</c> instead of the XP chart.</summary>
        public int? PracticeRankLimit { get; }

        /// <summary>The server's definition, so a minigame scenario can read the recipe's own minigame parameters; null for a hand-written entry.</summary>
        public RecipeDetail Detail { get; }

        /// <summary>Distinct component lines: one committed stack each when a player holds each in one stack.</summary>
        public int ComponentStacks => Components.Count;

        /// <summary>Total component units the recipe consumes.</summary>
        public int ComponentUnits => Components.Values.Sum();
    }
}
