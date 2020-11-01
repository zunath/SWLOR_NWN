using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class LeatherworkerPerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            LeathercraftRecipes(builder);
            AutoCraftLeathercrafting(builder);

            return builder.Build();
        }

        private static void LeathercraftRecipes(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Leatherworker, PerkType.LeathercraftRecipes)
                .Name("Leathercraft Recipes")
                .Description("Unlocks leathercraft recipes.")

                .AddPerkLevel()
                .Description("Unlocks tier 1 leathercraft recipes.")
                .Price(2)

                .AddPerkLevel()
                .Description("Unlocks tier 2 leathercraft recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Leathercraft, 10)

                .AddPerkLevel()
                .Description("Unlocks tier 3 leathercraft recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Leathercraft, 20)

                .AddPerkLevel()
                .Description("Unlocks tier 4 leathercraft recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Leathercraft, 30)

                .AddPerkLevel()
                .Description("Unlocks tier 5 leathercraft recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Leathercraft, 40);
        }

        private static void AutoCraftLeathercrafting(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Leatherworker, PerkType.AutoCraftLeathercrafting)
                .Name("Auto-Craft Leathercraft")
                .Description("Unlocks and speeds up Leathercraft auto-crafting.")

                .AddPerkLevel()
                .Description("Unlocks auto-craft command for Leathercraft.")
                .Price(4)

                .AddPerkLevel()
                .Description("Reduces Leathercraft auto-craft delay by 4 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Leathercraft, 10)

                .AddPerkLevel()
                .Description("Reduces Leathercraft auto-craft delay by 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Leathercraft, 20)

                .AddPerkLevel()
                .Description("Reduces Leathercraft auto-craft delay by 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Leathercraft, 30)

                .AddPerkLevel()
                .Description("Reduces Leathercraft auto-craft delay by 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Leathercraft, 40);
        }
    }
}
