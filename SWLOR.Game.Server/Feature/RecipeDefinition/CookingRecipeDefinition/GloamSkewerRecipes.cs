using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class GloamSkewerRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Gloam Skewer
            _builder.Create(RecipeType.GloamSkewer, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ss_skewer")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("kinrath_meat", 3)
                .Component("herb_v", 2)
                .Component("ss_silk", 1);
        }
    }
}
