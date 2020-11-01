using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.BlacksmithingRecipeDefinition
{
    public class CookingRecipes1: IRecipeListDefinition
    {
        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            var builder = new RecipeBuilder();
            LongswordRecipes(builder);

            return builder.Build();
        }

        private static void LongswordRecipes(RecipeBuilder builder)
        {
            builder.Create(RecipeType.BasicLongsword, SkillType.Blacksmithing)
                .Category(RecipeCategoryType.Longsword)
                .Name("Basic Longsword")
                .Resref("nw_wswls001")
                .RequirementPerk(PerkType.BlacksmithingRecipes, 1)
                .Component("quest_item", 2);
        }

    }
}
