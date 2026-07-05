using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class SavoryShellBraiseRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Savory Shell Braise
            _builder.Create(RecipeType.SavoryShellBraise, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mb_braise")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("gimp_meat", 3)
                .Component("herb_m", 2)
                .Component("mb_shell", 1);
        }
    }
}
