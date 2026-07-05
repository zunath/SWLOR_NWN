using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class EmberclawRoastRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Emberclaw Roast
            _builder.Create(RecipeType.EmberclawRoast, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("rk_roast")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("raivor_meat", 3)
                .Component("herb_m", 2)
                .Component("rk_claw", 1);
        }
    }
}
