using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class StonebarbPotPieRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Stonebarb Pot Pie
            _builder.Create(RecipeType.StonebarbPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("gs_potpie")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("cairnmog_meat", 3)
                .Component("v_flour", 2)
                .Component("gs_spine", 1);
        }
    }
}
