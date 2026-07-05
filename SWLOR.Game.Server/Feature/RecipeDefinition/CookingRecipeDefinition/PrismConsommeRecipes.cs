using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class PrismConsommeRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Prism Consomme
            _builder.Create(RecipeType.PrismConsomme, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("se_consomme")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("visc_urchin", 2)
                .Component("p_crystal_blue", 1)
                .Component("se_eye", 1);
        }
    }
}
