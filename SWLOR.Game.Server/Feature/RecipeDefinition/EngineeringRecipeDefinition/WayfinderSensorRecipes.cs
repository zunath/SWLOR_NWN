using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class WayfinderSensorRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Wayfinder Sensor
            _builder.Create(RecipeType.WayfinderSensor, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("tk_sensor")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("obsidian", 3)
                .Component("elec_good", 3)
                .Component("tk_badge", 1);
        }
    }
}
