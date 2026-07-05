using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class BitterFenTeaRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Bitter Fen Tea
            _builder.Create(RecipeType.BitterFenTea, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mv_tea")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("herb_m", 3)
                .Component("distilled_water", 1)
                .Component("mv_core", 1);
        }
    }
}
