using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class CulinarianPerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            CookingRecipes(builder);
            AutoCraftCooking(builder);

            return builder.Build();
        }

        private static void CookingRecipes(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Culinarian, PerkType.CookingRecipes)
                .Name("Cooking Recipes")
                .Description("Unlocks cooking recipes.")

                .AddPerkLevel()
                .Description("Unlocks tier 1 cooking recipes.")
                .Price(2)

                .AddPerkLevel()
                .Description("Unlocks tier 2 cooking recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Cooking, 10)

                .AddPerkLevel()
                .Description("Unlocks tier 3 cooking recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Cooking, 20)

                .AddPerkLevel()
                .Description("Unlocks tier 4 cooking recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Cooking, 30)

                .AddPerkLevel()
                .Description("Unlocks tier 5 cooking recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Cooking, 40);
        }

        private static void AutoCraftCooking(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Culinarian, PerkType.AutoCraftCooking)
                .Name("Auto-Craft Cooking")
                .Description("Unlocks and speeds up Cooking auto-crafting.")

                .AddPerkLevel()
                .Description("Unlocks auto-craft command for Cooking.")
                .Price(4)

                .AddPerkLevel()
                .Description("Reduces Cooking auto-craft delay by 4 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Cooking, 10)

                .AddPerkLevel()
                .Description("Reduces Cooking auto-craft delay by 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Cooking, 20)

                .AddPerkLevel()
                .Description("Reduces Cooking auto-craft delay by 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Cooking, 30)

                .AddPerkLevel()
                .Description("Reduces Cooking auto-craft delay by 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Cooking, 40);
        }
    }
}
