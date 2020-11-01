using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BlacksmithPerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            BlacksmithingRecipes(builder);
            AutoCraftBlacksmithing(builder);

            return builder.Build();
        }

        private static void BlacksmithingRecipes(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Blacksmith, PerkType.BlacksmithingRecipes)
                .Name("Blacksmithing Recipes")
                .Description("Unlocks blacksmithing recipes.")

                .AddPerkLevel()
                .Description("Unlocks tier 1 blacksmithing recipes.")
                .Price(2)

                .AddPerkLevel()
                .Description("Unlocks tier 2 blacksmithing recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Blacksmithing, 10)

                .AddPerkLevel()
                .Description("Unlocks tier 3 blacksmithing recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Blacksmithing, 20)

                .AddPerkLevel()
                .Description("Unlocks tier 4 blacksmithing recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Blacksmithing, 30)

                .AddPerkLevel()
                .Description("Unlocks tier 5 blacksmithing recipes.")
                .Price(2)
                .RequirementSkill(SkillType.Blacksmithing, 40);
        }

        private static void AutoCraftBlacksmithing(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Blacksmith, PerkType.AutoCraftBlacksmithing)
                .Name("Auto-Craft Blacksmithing")
                .Description("Unlocks and speeds up Blacksmithing auto-crafting.")

                .AddPerkLevel()
                .Description("Unlocks auto-craft command for Blacksmithing.")
                .Price(4)

                .AddPerkLevel()
                .Description("Reduces Blacksmithing auto-craft delay by 4 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Blacksmithing, 10)

                .AddPerkLevel()
                .Description("Reduces Blacksmithing auto-craft delay by 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Blacksmithing, 20)

                .AddPerkLevel()
                .Description("Reduces Blacksmithing auto-craft delay by 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Blacksmithing, 30)

                .AddPerkLevel()
                .Description("Reduces Blacksmithing auto-craft delay by 16 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Blacksmithing, 40);
        }
    }
}
