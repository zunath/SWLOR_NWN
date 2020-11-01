using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class AlchemistPerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            AlchemistRecipes(builder);
            AutoCraftAlchemy(builder);

            return builder.Build();
        }

        private static void AlchemistRecipes(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Alchemist, PerkType.AlchemyRecipes)
                .Name("Alchemy Recipes")
                .Description("Unlocks alchemy recipes.")

                .AddPerkLevel()
                .Description("Unlocks tier 1 alchemy recipes.")
                .Price(2)

                .AddPerkLevel()
                .Description("Unlocks tier 2 alchemy recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Alchemy, 10)

                .AddPerkLevel()
                .Description("Unlocks tier 3 alchemy recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Alchemy, 20)

                .AddPerkLevel()
                .Description("Unlocks tier 4 alchemy recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Alchemy, 30)

                .AddPerkLevel()
                .Description("Unlocks tier 5 alchemy recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Alchemy, 40);
        }

        private static void AutoCraftAlchemy(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Alchemist, PerkType.AutoCraftAlchemy)
                .Name("Auto-Craft Alchemy")
                .Description("Unlocks and speeds up Alchemy auto-crafting.")

                .AddPerkLevel()
                .Description("Unlocks auto-craft command for Alchemy.")
                .Price(4)

                .AddPerkLevel()
                .Description("Reduces Alchemy auto-craft delay by 4 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Alchemy, 10)

                .AddPerkLevel()
                .Description("Reduces Alchemy auto-craft delay by 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Alchemy, 20)

                .AddPerkLevel()
                .Description("Reduces Alchemy auto-craft delay by 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Alchemy, 30)

                .AddPerkLevel()
                .Description("Reduces Alchemy auto-craft delay by 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Alchemy, 40);
        }
    }
}
