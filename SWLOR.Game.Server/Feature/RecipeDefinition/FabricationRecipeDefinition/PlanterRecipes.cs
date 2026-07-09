using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class PlanterRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Planter Box
            _builder.Create(RecipeType.PlanterBox, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_0431")
                .Level(5)
                .Component("const_parts", 2);

            // Hydroponic Rack
            _builder.Create(RecipeType.HydroponicRack, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_0432")
                .Level(25)
                .Component("const_parts", 3)
                .Component("pow_supp_unit", 2);
        }
    }
}
