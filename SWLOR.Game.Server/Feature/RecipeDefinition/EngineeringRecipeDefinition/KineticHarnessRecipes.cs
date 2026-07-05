using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class KineticHarnessRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Kinetic Harness
            _builder.Create(RecipeType.KineticHarness, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("hv_servo")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("obsidian", 3)
                .Component("elec_good", 3)
                .Component("hv_plate", 1);
        }
    }
}
