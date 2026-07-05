using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class ResonantBrothRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Resonant Broth
            _builder.Create(RecipeType.ResonantBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ae_broth")
                .Level(4)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("mynock_wing", 3)
                .Component("distilled_water", 1)
                .Component("ae_echo", 1);
        }
    }
}
