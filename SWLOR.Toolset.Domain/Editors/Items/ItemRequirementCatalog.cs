using System.Reflection;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Every equip requirement an item editor can offer: one row per non-language
    /// <see cref="SkillType"/> (RequiresSkill, property 131), the six iprp_reqstat abilities
    /// (RequiresStat, property 132), the perk gate (UseLimitationPerk, property 100), and the race
    /// gate (UseLimitationRacial, property 64).
    /// </summary>
    /// <remarks>
    /// Skill names and categories are read off <see cref="SkillType"/>'s own
    /// <see cref="SkillAttribute"/> via reflection - the same lookup
    /// <c>ReflectionGameplayEnumReader.ReadSkillTypes</c> uses - so this catalog cannot drift from
    /// the game's own skill list or category grouping.
    /// </remarks>
    public static class ItemRequirementCatalog
    {
        public static IReadOnlyList<ItemRequirementDefinition> All { get; } = Build();

        public static IReadOnlyList<ItemRequirementDefinition> ByCategory(ItemRequirementCategory category) =>
            All.Where(requirement => requirement.Category == category)
                .OrderBy(requirement => requirement.DisplayOrder)
                .ToList();

        private static List<ItemRequirementDefinition> Build()
        {
            var order = 0;
            var requirements = new List<ItemRequirementDefinition>();

            void Add(
                ItemRequirementCategory category,
                string label,
                int propertyId,
                int subtypeId,
                int costTableId,
                SkillCategoryType? skillCategory = null) =>
                requirements.Add(new ItemRequirementDefinition(
                    category, label, propertyId, subtypeId, costTableId, order++, skillCategory));

            foreach (SkillType skill in Enum.GetValues<SkillType>())
            {
                if (skill == SkillType.Invalid)
                    continue;

                var attribute = typeof(SkillType).GetField(skill.ToString())?.GetCustomAttribute<SkillAttribute>();
                if (attribute == null || attribute.Category == SkillCategoryType.Languages)
                    continue; // Requirements list combat/crafting/utility skills; languages do not gate equipment.

                Add(ItemRequirementCategory.Skill, attribute.Name, 131, (int)skill, 48, attribute.Category);
            }

            var reqStats = new (int Id, string Label)[]
            {
                (0, "Might"), (1, "Perception"), (2, "Vitality"), (3, "Agility"), (4, "Willpower"), (5, "Social")
            };
            foreach (var (id, label) in reqStats)
                Add(ItemRequirementCategory.Stat, label, 132, id, 53);

            Add(ItemRequirementCategory.Perk, "Required Perk", 100, -1, 33);
            Add(ItemRequirementCategory.Race, "Required Race", 64, -1, -1);

            return requirements;
        }
    }
}
