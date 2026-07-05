using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class FaultlineCapacitorRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Faultline Capacitor
            _builder.Create(RecipeType.FaultlineCapacitor, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("sr_jrcell")
                .Level(12)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("elec_ruined", 3)
                .Component("ref_veldite", 2)
                .Component("sr_token", 1);
        }
    }
}
