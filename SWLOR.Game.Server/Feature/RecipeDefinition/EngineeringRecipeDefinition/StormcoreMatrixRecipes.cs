using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class StormcoreMatrixRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Stormcore Matrix
            _builder.Create(RecipeType.StormcoreMatrix, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("vx_matrix")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("vx_core", 1);
        }
    }
}
